using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CoachPadWpf;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly WebSocketService _ws = new();
    private readonly TranscriptSender _transcriptSender = new();
    private readonly AudioCaptureService _audioService = new();
    private readonly CameraCaptureService _cameraService = new();
    private readonly DetectorService _detector = new();
    private readonly OutcomeClient _outcomeClient = new();
    private readonly SummaryStore _summaryStore = new();
    private readonly AutoStartService _autoStart = new();
    private readonly DataRetentionService _retention = new();

    private string _host = "127.0.0.1:8000";
    private string _connectionState = "disconnected";
    private string _statusMessage = "Waiting";
    private string _metricsText = "Waiting for metrics";
    private string _debugTranscript = "";
    private bool _isSystemAudioEnabled = true;
    private bool _isMicEnabled = true;
    private Visibility _isRecordingVisible = Visibility.Collapsed;
    private string _summaryText = "No summary yet";
    private bool _isCoaching;
    private bool _hasAudioConsent;
    private bool _hasCameraConsent;
    private bool _startWithWindows;
    private bool _followActiveWindow;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        _ws.MetricsReceived += metrics =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MetricsText = $"Talk to listen: {metrics.talk_listen_ratio:F2}\n" +
                              $"Questions per minute: {metrics.questions_per_minute:F2}\n" +
                              $"Sentiment: {metrics.sentiment:F2}\n" +
                              $"Engagement: {metrics.engagement:F2}\n" +
                              $"Stage: {metrics.methodology_stage}";
                SayNext.Clear();
                foreach (var line in metrics.say_next)
                {
                    SayNext.Add(line);
                }
            });
        };
        _ws.Error += message => StatusMessage = message;
        _audioService.Error += message => StatusMessage = message;
        _cameraService.Error += message => StatusMessage = message;
        _detector.Detected += async () => await StartCoachingAsync();
        var cached = _summaryStore.Load();
        if (!string.IsNullOrWhiteSpace(cached))
        {
            _summaryText = cached;
        }
        _startWithWindows = _autoStart.IsEnabled();
    }

    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    public string ConnectionState
    {
        get => _connectionState;
        set => SetProperty(ref _connectionState, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string MetricsText
    {
        get => _metricsText;
        set => SetProperty(ref _metricsText, value);
    }

    public string DebugTranscript
    {
        get => _debugTranscript;
        set => SetProperty(ref _debugTranscript, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

    public bool HasAudioConsent
    {
        get => _hasAudioConsent;
        set => SetProperty(ref _hasAudioConsent, value);
    }

    public bool HasCameraConsent
    {
        get => _hasCameraConsent;
        set => SetProperty(ref _hasCameraConsent, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetProperty(ref _startWithWindows, value))
            {
                if (value)
                {
                    _autoStart.Enable(Environment.ProcessPath ?? string.Empty);
                }
                else
                {
                    _autoStart.Disable();
                }
            }
        }
    }

    public bool FollowActiveWindow
    {
        get => _followActiveWindow;
        set => SetProperty(ref _followActiveWindow, value);
    }

    public bool IsSystemAudioEnabled
    {
        get => _isSystemAudioEnabled;
        set => SetProperty(ref _isSystemAudioEnabled, value);
    }

    public bool IsMicEnabled
    {
        get => _isMicEnabled;
        set => SetProperty(ref _isMicEnabled, value);
    }

    public Visibility IsRecordingVisible
    {
        get => _isRecordingVisible;
        set => SetProperty(ref _isRecordingVisible, value);
    }

    public ObservableCollection<string> SayNext { get; } = new();

    public void InitializeDetector(Action onDetected)
    {
        _detector.UiSignal = onDetected;
        _detector.Start();
    }

    public async Task ConnectAsync()
    {
        try
        {
            ConnectionState = "connecting";
            await _ws.ConnectAsync(Host);
            ConnectionState = "connected";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            ConnectionState = "error";
        }
    }

    public void Disconnect()
    {
        ConnectionState = "disconnected";
        _ = _ws.DisconnectAsync();
    }

    public async Task StartCoachingAsync()
    {
        if (_isCoaching)
        {
            return;
        }
        if (!HasAudioConsent)
        {
            StatusMessage = "Audio consent required";
            return;
        }
        _isCoaching = true;
        StatusMessage = "Starting coaching";
        await ConnectAsync();
        var result = await _audioService.StartAsync(Host, IsSystemAudioEnabled, IsMicEnabled);
        if (!result)
        {
            StatusMessage = "System audio failed. Using microphone only.";
        }
        if (HasCameraConsent)
        {
            await _cameraService.StartAsync(Host);
        }
        IsRecordingVisible = Visibility.Visible;
        StatusMessage = "Coaching active";
    }

    public void StopCoaching()
    {
        StatusMessage = "Stopping coaching";
        _ = _audioService.StopAsync();
        _ = _cameraService.StopAsync();
        Disconnect();
        IsRecordingVisible = Visibility.Collapsed;
        _isCoaching = false;
        _detector.Reset();
    }

    public async Task EndCallAsync()
    {
        try
        {
            await _outcomeClient.SendOutcomeAsync(Host, "follow_up");
            var summary = await _outcomeClient.FetchSummaryAsync(Host);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                SummaryText = summary;
                _summaryStore.Save(summary);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void SendDebugTranscript()
    {
        _ = _transcriptSender.SendAsync(Host, DebugTranscript);
    }

    public void DeleteAllData()
    {
        _retention.DeleteAll();
        SummaryText = "No summary yet";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
        {
            return false;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
