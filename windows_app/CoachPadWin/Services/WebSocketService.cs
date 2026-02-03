using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CoachPadWin.Models;

namespace CoachPadWin.Services;

public sealed class WebSocketService
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;

    public event Action<LiveMetrics>? MetricsReceived;
    public event Action<string>? Error;

    public async Task ConnectAsync(string host)
    {
        await DisconnectAsync();
        _socket = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        var uri = new Uri($"ws://{host}/ws/ui");
        await _socket.ConnectAsync(uri, _cts.Token);
        _ = Task.Run(ReceiveLoopAsync, _cts.Token);
        _ = Task.Run(PingLoopAsync, _cts.Token);
    }

    public async Task DisconnectAsync()
    {
        if (_cts is null || _socket is null)
        {
            return;
        }
        _cts.Cancel();
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

    private async Task ReceiveLoopAsync()
    {
        if (_socket is null || _cts is null)
        {
            return;
        }
        var buffer = new byte[8192];
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var result = await _socket.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await DisconnectAsync();
                    return;
                }
                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var metrics = JsonSerializer.Deserialize<LiveMetrics>(text);
                if (metrics is not null)
                {
                    MetricsReceived?.Invoke(metrics);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
                return;
            }
        }
    }

    private async Task PingLoopAsync()
    {
        if (_socket is null || _cts is null)
        {
            return;
        }
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _socket.SendAsync(Encoding.UTF8.GetBytes("ping"), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch
            {
            }
            await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
        }
    }
}
