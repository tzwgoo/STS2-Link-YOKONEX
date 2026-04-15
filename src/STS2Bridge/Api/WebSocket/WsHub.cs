using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using STS2Bridge.Events;

namespace STS2Bridge.Api.WebSocket;

public sealed class WsHub : IDisposable
{
    private readonly ConcurrentDictionary<Guid, WsClientSession> _clients = new();
    private readonly IDisposable _subscription;

    public WsHub(GameEventBus eventBus)
    {
        _subscription = eventBus.Subscribe(evt => _ = BroadcastEventAsync(evt));
    }

    public async Task AcceptAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
        var socket = wsContext.WebSocket;
        var session = new WsClientSession(Guid.NewGuid(), socket);
        _clients[session.Id] = session;

        await session.SendAsync(new
        {
            kind = "hello",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            data = new { message = "sts2 bridge websocket ready" }
        }, cancellationToken);

        var buffer = new byte[1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        finally
        {
            _clients.TryRemove(session.Id, out _);
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            }
            socket.Dispose();
        }
    }

    private async Task BroadcastEventAsync(GameEvent evt)
    {
        if (_clients.IsEmpty)
        {
            return;
        }

        var payload = new
        {
            kind = "event",
            type = evt.Type,
            timestamp = evt.Timestamp,
            data = new
            {
                evt.EventId,
                evt.RunId,
                evt.Floor,
                evt.RoomType,
                evt.Payload
            }
        };

        foreach (var session in _clients.Values)
        {
            if (session.Socket.State != WebSocketState.Open)
            {
                _clients.TryRemove(session.Id, out _);
                continue;
            }

            try
            {
                await session.SendAsync(payload, CancellationToken.None);
            }
            catch
            {
                _clients.TryRemove(session.Id, out _);
            }
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
