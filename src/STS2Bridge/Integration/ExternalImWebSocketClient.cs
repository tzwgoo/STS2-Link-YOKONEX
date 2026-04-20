using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace STS2Bridge.Integration;

public sealed class ExternalImWebSocketClient : IExternalImClient, IDisposable
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveLoopCts;
    private Task? _receiveLoopTask;
    private ExternalImStatus _status = new();

    public ExternalImStatus Status => _status;

    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (_socket is { State: WebSocketState.Open } && string.Equals(_status.ConnectedUrl, url, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        DisposeSocket();

        _status = _status with
        {
            ConnectionState = ExternalImConnectionState.Connecting,
            ConnectedUrl = url,
            LastError = null
        };

        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(url), cancellationToken);
        _socket = socket;
        _receiveLoopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(socket, _receiveLoopCts.Token), _receiveLoopCts.Token);
        _status = _status with
        {
            ConnectionState = ExternalImConnectionState.Connected,
            LastError = null
        };
    }

    public async Task LoginAsync(string uid, string token, CancellationToken cancellationToken = default)
    {
        _status = _status with
        {
            ConnectionState = ExternalImConnectionState.LoggingIn,
            CurrentUid = uid,
            LastError = null
        };

        await SendAsync(new
        {
            type = "login",
            uid,
            token
        }, cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync(new
        {
            type = "logout",
            userId = _status.CurrentUserId
        }, cancellationToken);
    }

    public async Task SendCommandAsync(string userId, string commandId, CancellationToken cancellationToken = default)
    {
        await SendAsync(new
        {
            type = "sendCommand",
            userId,
            commandId
        }, cancellationToken);
    }

    public void HandleIncomingMessage(string message)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        var success = root.TryGetProperty("success", out var successElement) && successElement.ValueKind == JsonValueKind.True;
        var serverMessage = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

        switch (type)
        {
            case "connected":
                _status = _status with
                {
                    ConnectionState = ExternalImConnectionState.Connected,
                    LastServerMessage = serverMessage,
                    LastError = null
                };
                break;
            case "loginResult":
                if (success)
                {
                    var data = root.TryGetProperty("data", out var dataElement) ? dataElement : default;
                    _status = _status with
                    {
                        ConnectionState = ExternalImConnectionState.LoggedIn,
                        CurrentUserId = TryGetString(data, "userId"),
                        CurrentUid = TryGetString(data, "uid") ?? _status.CurrentUid,
                        LastServerMessage = serverMessage,
                        LastError = null
                    };
                }
                else
                {
                    _status = _status with
                    {
                        ConnectionState = ExternalImConnectionState.LoginFailed,
                        LastServerMessage = serverMessage,
                        LastError = serverMessage ?? "IM 登录失败"
                    };
                }
                break;
            case "logoutResult":
                _status = _status with
                {
                    ConnectionState = ExternalImConnectionState.Connected,
                    CurrentUserId = null,
                    LastServerMessage = serverMessage,
                    LastError = success ? null : serverMessage
                };
                break;
            case "error":
                _status = _status with
                {
                    ConnectionState = ExternalImConnectionState.Error,
                    LastServerMessage = serverMessage,
                    LastError = serverMessage ?? "外部 IM 服务返回错误"
                };
                break;
            default:
                _status = _status with
                {
                    LastServerMessage = serverMessage ?? message
                };
                break;
        }
    }

    public void Dispose()
    {
        _receiveLoopCts?.Cancel();
        DisposeSocket();
        GC.SuppressFinalize(this);
    }

    private async Task SendAsync<T>(T payload, CancellationToken cancellationToken)
    {
        if (_socket is not { State: WebSocketState.Open })
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private void DisposeSocket()
    {
        _receiveLoopCts?.Dispose();
        _receiveLoopCts = null;
        _receiveLoopTask = null;
        _socket?.Dispose();
        _socket = null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(buffer);
                using var stream = new MemoryStream();

                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(segment, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _status = _status with
                        {
                            ConnectionState = ExternalImConnectionState.Disconnected
                        };
                        return;
                    }

                    stream.Write(segment.Array!, segment.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(stream.ToArray());
                    HandleIncomingMessage(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _status = _status with
            {
                ConnectionState = ExternalImConnectionState.Error,
                LastError = ex.Message
            };
        }
    }
}
