using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using NAudio.CoreAudioApi;

namespace CoachPadWpf;

public sealed class DetectorService
{
    private readonly DispatcherTimer _timer;
    private readonly HashSet<string> _processNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "zoom",
        "teams",
        "ms-teams",
        "ms-teams.exe",
        "chrome",
        "msedge"
    };

    public event Func<Task>? Detected;
    public Action? UiSignal { get; set; }
    private bool _isTriggered;

    public DetectorService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await CheckAsync();
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Reset()
    {
        _isTriggered = false;
    }

    private async Task CheckAsync()
    {
        var windows = WindowEnumerator.GetTopLevelWindows();
        var hasTargetWindow = windows.Any(w => IsTargetWindow(w.Title));
        var hasTargetProcess = Process.GetProcesses().Any(p => IsTargetProcess(p.ProcessName));
        var hasTargetAudio = HasTargetAudioSession();
        if ((hasTargetWindow || hasTargetProcess || hasTargetAudio) && !_isTriggered)
        {
            _isTriggered = true;
            UiSignal?.Invoke();
            if (Detected is not null)
            {
                await Detected.Invoke();
            }
        }
    }

    private bool HasTargetAudioSession()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessions = device.AudioSessionManager.Sessions;
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                using var session2 = session.QueryInterface<AudioSessionControl2>();
                var processId = session2.ProcessID;
                if (processId == 0)
                {
                    continue;
                }
                var process = Process.GetProcessById((int)processId);
                if (!IsTargetProcess(process.ProcessName))
                {
                    continue;
                }
                var peak = session.AudioMeterInformation.MasterPeakValue;
                if (peak > 0.02f)
                {
                    return true;
                }
            }
        }
        catch
        {
        }
        return false;
    }

    private bool IsTargetProcess(string name)
    {
        return _processNames.Contains(name);
    }

    private bool IsTargetWindow(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }
        var lower = title.ToLowerInvariant();
        return lower.Contains("teams") ||
               lower.Contains("zoom") ||
               lower.Contains("google meet") ||
               lower.Contains("meet.google.com") ||
               lower.Contains("snapmobile") ||
               lower.Contains("webphone");
    }
}

public sealed record WindowInfo(nint Handle, string Title);

public static class WindowEnumerator
{
    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT rect);

    public static bool TryGetForegroundRect(out RECT rect)
    {
        var handle = GetForegroundWindow();
        if (handle == nint.Zero)
        {
            rect = default;
            return false;
        }
        return GetWindowRect(handle, out rect);
    }

    public static List<WindowInfo> GetTopLevelWindows()
    {
        var result = new List<WindowInfo>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
            {
                return true;
            }
            var length = GetWindowTextLength(hWnd);
            if (length == 0)
            {
                return true;
            }
            var builder = new StringBuilder(length + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            result.Add(new WindowInfo(hWnd, builder.ToString()));
            return true;
        }, 0);
        return result;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
