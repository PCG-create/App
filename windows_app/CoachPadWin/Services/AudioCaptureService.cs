using System.Net.WebSockets;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CoachPadWin.Services;

public enum AudioSourceMode
{
    Microphone = 0,
    SystemAudio = 1
}

public sealed class AudioCaptureService
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private IWaveIn? _capture;
    private BufferedWaveProvider? _buffered;
    private Task? _sendTask;

    public event Action<string>? Error;

    public async Task StartAsync(string host, AudioSourceMode source)
    {
        await StopAsync();
        _socket = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        var uri = new Uri($"ws://{host}/ws/audio");
        await _socket.ConnectAsync(uri, _cts.Token);

        _capture = source == AudioSourceMode.SystemAudio
            ? new WasapiLoopbackCapture()
            : new WasapiCapture();

        _buffered = new BufferedWaveProvider(_capture.WaveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        _capture.DataAvailable += (_, args) =>
        {
            _buffered?.AddSamples(args.Buffer, 0, args.BytesRecorded);
        };
        _capture.StartRecording();
        _sendTask = Task.Run(SendLoopAsync, _cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }
        _cts.Cancel();
        _capture?.StopRecording();
        _capture?.Dispose();
        _capture = null;
        _buffered = null;
        if (_socket is not null)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch
            {
            }
            _socket.Dispose();
            _socket = null;
        }
    }

    private async Task SendLoopAsync()
    {
        if (_socket is null || _cts is null || _buffered is null)
        {
            return;
        }
        var waveProvider = _buffered.ToSampleProvider();
        if (waveProvider.WaveFormat.Channels > 1)
        {
            waveProvider = new StereoToMonoSampleProvider(waveProvider)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f
            };
        }
        if (waveProvider.WaveFormat.SampleRate != 16000)
        {
            waveProvider = new WdlResamplingSampleProvider(waveProvider, 16000);
        }
        var pcm16 = new SampleToWaveProvider16(waveProvider);
        var buffer = new byte[3200];
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var read = pcm16.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    await _socket.SendAsync(buffer.AsMemory(0, read), WebSocketMessageType.Binary, true, _cts.Token);
                }
                else
                {
                    await Task.Delay(10, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return;
            }
        }
    }
}
