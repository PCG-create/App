using System.Net.WebSockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace CoachPadWin.Services;

public sealed class CameraCaptureService
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private MediaCapture? _mediaCapture;
    private Task? _captureTask;

    public event Action<string>? Error;

    public async Task StartAsync(string host)
    {
        await StopAsync();
        _socket = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        var uri = new Uri($"ws://{host}/ws/vision");
        await _socket.ConnectAsync(uri, _cts.Token);

        _mediaCapture = new MediaCapture();
        await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            StreamingCaptureMode = StreamingCaptureMode.Video
        });

        _captureTask = Task.Run(CaptureLoopAsync, _cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }
        _cts.Cancel();
        if (_mediaCapture is not null)
        {
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }
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

    private async Task CaptureLoopAsync()
    {
        if (_socket is null || _cts is null || _mediaCapture is null)
        {
            return;
        }
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                var imageProperties = ImageEncodingProperties.CreateJpeg();
                await _mediaCapture.CapturePhotoToStreamAsync(imageProperties, stream);
                stream.Seek(0);
                var length = (int)stream.Size;
                var bytes = new byte[length];
                await stream.ReadAsync(bytes.AsBuffer(), (uint)length, InputStreamOptions.None);
                await _socket.SendAsync(bytes, WebSocketMessageType.Binary, true, _cts.Token);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(500), _cts.Token);
        }
    }
}
