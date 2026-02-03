using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoachPadWin.Models;
using CoachPadWin.Services;
using Microsoft.UI.Dispatching;

namespace CoachPadWin;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly WebSocketService _webSocketService = new();
    private readonly AudioCaptureService _audioService = new();
    private readonly CameraCaptureService _cameraService = new();
    private readonly OutcomeClient _outcomeClient = new();
    private readonly SummaryStore _summaryStore = new();
    private readonly DispatcherQueue _dispatcher;

    private string _host = "127.0.0.1:8000";
    private string _connectionState = "disconnected";
    private string _statusMessage = "";
    private string _metricsText = "Waiting for metrics";
    private string _summaryText = "No summary yet";
    private int _audioSourceIndex;
    private bool _isCameraEnabled;
    private int _outcomeIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _webSocketService.MetricsReceived += OnMetrics;
        _webSocketService.Error += message => StatusMessage = message;
        _audioService.Error += message => StatusMessage = message;
        _cameraService.Error += message => StatusMessage = message;
        var cached = _summaryStore.Load();
        if (!string.IsNullOrWhiteSpace(cached))
        {
            SummaryText = cached;
        }
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

    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

    public ObservableCollection<string> SayNext { get; } = new();

    public int AudioSourceIndex
    {
        get => _audioSourceIndex;
        set => SetProperty(ref _audioSourceIndex, value);
    }

    public bool IsCameraEnabled
    {
        get => _isCameraEnabled;
        set => SetProperty(ref _isCameraEnabled, value);
    }

    public int OutcomeIndex
    {
        get => _outcomeIndex;
        set => SetProperty(ref _outcomeIndex, value);
    }

    public async Task ConnectAsync()
    {
        try
        {
            ConnectionState = "connecting";
            await _webSocketService.ConnectAsync(Host);
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
        _ = _webSocketService.DisconnectAsync();
    }

    public async Task StartCoachingAsync()
    {
        StatusMessage = "Starting coaching";
        await ConnectAsync();
        var source = AudioSourceIndex == 0 ? AudioSourceMode.Microphone : AudioSourceMode.SystemAudio;
        await _audioService.StartAsync(Host, source);
        if (IsCameraEnabled)
        {
            await _cameraService.StartAsync(Host);
        }
        StatusMessage = "Coaching active";
    }

    public void StopCoaching()
    {
        StatusMessage = "Stopping coaching";
        _ = _audioService.StopAsync();
        _ = _cameraService.StopAsync();
        Disconnect();
    }

    public async Task EndCallAsync()
    {
        try
        {
            var outcome = OutcomeIndex switch
            {
                0 => "meeting_booked",
                2 => "lost",
                _ => "follow_up"
            };
            await _outcomeClient.SendOutcomeAsync(Host, outcome);
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

    private void OnMetrics(LiveMetrics metrics)
    {
        _dispatcher.TryEnqueue(() =>
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
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
