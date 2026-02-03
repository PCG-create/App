using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CoachPadWpf;

public sealed class TranscriptSender
{
    public async Task SendAsync(string host, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri($"ws://{host}/ws/ingest"), CancellationToken.None);
        var payload = new
        {
            speaker = "rep",
            text,
            timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        var json = JsonSerializer.Serialize(payload);
        await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
