using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace STS2Bridge.Api.WebSocket;

public sealed class WsClientSession(Guid id, System.Net.WebSockets.WebSocket socket)
{
    public Guid Id { get; } = id;

    public System.Net.WebSockets.WebSocket Socket { get; } = socket;

    public async Task SendAsync<T>(T payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, ApiJson.SerializerOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        await Socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }
}
