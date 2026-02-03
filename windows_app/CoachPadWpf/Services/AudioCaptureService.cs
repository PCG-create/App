using System.Net.WebSockets;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CoachPadWpf;

public sealed class AudioCaptureService
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private IWaveIn? _loopback;
    private IWaveIn? _mic;
    private BufferedWaveProvider? _loopbackBuffer;
    private BufferedWaveProvider? _micBuffer;

    public event Action<string>? Error;

    public async Task<bool> StartAsync(string host, bool includeSystemAudio, bool includeMic)
    {
        await StopAsync();
        _socket = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        await _socket.ConnectAsync(new Uri($"ws://{host}/ws/audio"), _cts.Token);

        var systemAudioOk = true;
        if (includeSystemAudio)
        {
            try
            {
                _loopback = new WasapiLoopbackCapture();
                _loopbackBuffer = new BufferedWaveProvider(_loopback.WaveFormat) { DiscardOnBufferOverflow = true };
                _loopback.DataAvailable += (_, args) => _loopbackBuffer?.AddSamples(args.Buffer, 0, args.BytesRecorded);
                _loopback.StartRecording();
            }
            catch (Exception ex)
            {
                systemAudioOk = false;
                Error?.Invoke(ex.Message);
            }
        }

        if (includeMic)
        {
            _mic = new WasapiCapture();
            _micBuffer = new BufferedWaveProvider(_mic.WaveFormat) { DiscardOnBufferOverflow = true };
            _mic.DataAvailable += (_, args) => _micBuffer?.AddSamples(args.Buffer, 0, args.BytesRecorded);
            _mic.StartRecording();
        }

        _ = Task.Run(SendLoopAsync, _cts.Token);
        return systemAudioOk;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }
        _cts.Cancel();
        _loopback?.StopRecording();
        _mic?.StopRecording();
        _loopback?.Dispose();
        _mic?.Dispose();
        _loopback = null;
        _mic = null;
        _loopbackBuffer = null;
        _micBuffer = null;
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
        if (_socket is null || _cts is null)
        {
            return;
        }
        var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1))
        {
            ReadFully = true
        };

        if (_loopbackBuffer is not null)
        {
            var loopProvider = BuildProvider(_loopbackBuffer);
            mixer.AddMixerInput(loopProvider);
        }
        if (_micBuffer is not null)
        {
            var micProvider = BuildProvider(_micBuffer);
            mixer.AddMixerInput(micProvider);
        }

        var pcm16 = new SampleToWaveProvider16(mixer);
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

    private static ISampleProvider BuildProvider(BufferedWaveProvider buffer)
    {
        ISampleProvider provider = buffer.ToSampleProvider();
        if (provider.WaveFormat.Channels > 1)
        {
            provider = new StereoToMonoSampleProvider(provider)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f
            };
        }
        if (provider.WaveFormat.SampleRate != 16000)
        {
            provider = new WdlResamplingSampleProvider(provider, 16000);
        }
        return provider;
    }
}
