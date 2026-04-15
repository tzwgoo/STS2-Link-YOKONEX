using System.Net;
using System.Net.WebSockets;
using STS2Bridge.Actions;
using STS2Bridge.Api.WebSocket;
using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.State;

namespace STS2Bridge.Api;

public sealed class RuntimeApiHost : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stopCts = new();
    private readonly WsHub _wsHub;
    private readonly BridgeConfig _config;
    private readonly GameEventBus _eventBus;
    private readonly GameStateStore _stateStore;
    private readonly ActionRouter _actionRouter;
    private Task? _serveLoop;

    public RuntimeApiHost(
        BridgeConfig config,
        GameEventBus eventBus,
        GameStateStore stateStore,
        ActionRouter actionRouter)
    {
        _config = config;
        _eventBus = eventBus;
        _stateStore = stateStore;
        _actionRouter = actionRouter;
        _wsHub = new WsHub(eventBus);
        BaseUri = new Uri($"http://{config.BindHost}:{config.Port}/");
        _listener.Prefixes.Add(BaseUri.AbsoluteUri);
    }

    public Uri BaseUri { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _listener.Start();
        _serveLoop = Task.Run(() => LocalApiServer.RunAsync(_listener, _wsHub, _config, _eventBus, _stateStore, _actionRouter, _stopCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _stopCts.Cancel();

        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _wsHub.Dispose();

        if (_serveLoop is not null)
        {
            try
            {
                await _serveLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpListenerException)
            {
            }
            catch (WebSocketException)
            {
            }
        }

        _stopCts.Dispose();
    }
}
