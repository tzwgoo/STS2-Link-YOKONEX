using System.Net;
using System.Text;
using System.Text.Json;
using STS2Bridge.Actions;
using STS2Bridge.Api.Http;
using STS2Bridge.Api.WebSocket;
using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.State;

namespace STS2Bridge.Api;

public static class LocalApiServer
{
    public static async Task<RuntimeApiHost> StartRuntimeAsync(
        BridgeConfig config,
        GameEventBus eventBus,
        GameStateStore stateStore,
        ActionRouter actionRouter,
        CancellationToken cancellationToken)
    {
        var host = new RuntimeApiHost(config, eventBus, stateStore, actionRouter);
        await host.StartAsync(cancellationToken);
        return host;
    }

    internal static async Task RunAsync(
        HttpListener listener,
        WsHub wsHub,
        BridgeConfig config,
        GameEventBus eventBus,
        GameStateStore stateStore,
        ActionRouter actionRouter,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || !listener.IsListening)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || !listener.IsListening)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(context, wsHub, config, eventBus, stateStore, actionRouter, cancellationToken), CancellationToken.None);
        }
    }

    private static async Task HandleRequestAsync(
        HttpListenerContext context,
        WsHub wsHub,
        BridgeConfig config,
        GameEventBus eventBus,
        GameStateStore stateStore,
        ActionRouter actionRouter,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            var method = context.Request.HttpMethod;

            if (string.Equals(path, "/ws", StringComparison.OrdinalIgnoreCase))
            {
                await HandleWebSocketAsync(context, wsHub, config, cancellationToken);
                return;
            }

            if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                await WriteJsonAsync(context.Response, ApiEnvelope<object>.Ok(new
                {
                    ok = true,
                    serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    modLoaded = true,
                    apiReady = true
                }));
                return;
            }

            if (string.Equals(path, "/version", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                await WriteJsonAsync(context.Response, ApiEnvelope<object>.Ok(new
                {
                    modVersion = "0.1.0",
                    protocolVersion = "1.0",
                    gameVersion = "unknown",
                    features = new[] { "http", "state_snapshot", "actions" }
                }));
                return;
            }

            if (ValidateToken(context.Request, config, out var authStatus, out var authEnvelope))
            {
                context.Response.StatusCode = authStatus;
                await WriteJsonAsync(context.Response, authEnvelope);
                return;
            }

            if (string.Equals(path, "/state", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                await WriteJsonAsync(context.Response, ApiEnvelope<State.Dtos.StateSnapshotDto>.Ok(stateStore.GetSnapshot()));
                return;
            }

            if (string.Equals(path, "/events/recent", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                var limitRaw = context.Request.QueryString["limit"];
                var limit = int.TryParse(limitRaw, out var parsedLimit) ? parsedLimit : 50;
                await WriteJsonAsync(context.Response, ApiEnvelope<object>.Ok(new
                {
                    items = eventBus.GetRecentEvents(limit)
                }));
                return;
            }

            if (string.Equals(path, "/action", StringComparison.OrdinalIgnoreCase) && method == "POST")
            {
                var request = await JsonSerializer.DeserializeAsync<ActionRequest>(context.Request.InputStream, ApiJson.SerializerOptions, cancellationToken);
                if (request is null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context.Response, ApiEnvelope<object>.Fail("BAD_REQUEST", "Request body is required."));
                    return;
                }

                var response = await actionRouter.RouteAsync(request, cancellationToken);
                await WriteJsonAsync(context.Response, ApiEnvelope<ActionResponse>.Ok(response));
                return;
            }

            if (string.Equals(path, "/actions/schema", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                await WriteJsonAsync(context.Response, ApiEnvelope<object>.Ok(new
                {
                    items = config.AllowedActions
                }));
                return;
            }

            if (string.Equals(path, "/events/schema", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                await WriteJsonAsync(context.Response, ApiEnvelope<object>.Ok(new
                {
                    items = config.EventWhitelist
                }));
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteJsonAsync(context.Response, ApiEnvelope<object>.Fail("NOT_FOUND", $"No route matched '{method} {path}'."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (JsonException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteJsonAsync(context.Response, ApiEnvelope<object>.Fail("BAD_JSON", ex.Message));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteJsonAsync(context.Response, ApiEnvelope<object>.Fail("INTERNAL_ERROR", ex.Message));
        }
        finally
        {
            if (context.Response.OutputStream.CanWrite)
            {
                context.Response.OutputStream.Close();
            }
        }
    }

    private static async Task HandleWebSocketAsync(
        HttpListenerContext context,
        WsHub wsHub,
        BridgeConfig config,
        CancellationToken cancellationToken)
    {
        if (ValidateToken(context.Request, config, out var authStatus, out var authEnvelope))
        {
            context.Response.StatusCode = authStatus;
            await WriteJsonAsync(context.Response, authEnvelope);
            return;
        }

        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteJsonAsync(context.Response, ApiEnvelope<object>.Fail("BAD_REQUEST", "WebSocket request expected."));
            return;
        }

        await wsHub.AcceptAsync(context, cancellationToken);
    }

    private static bool ValidateToken(
        HttpListenerRequest request,
        BridgeConfig config,
        out int statusCode,
        out ApiEnvelope<object> envelope)
    {
        if (string.IsNullOrWhiteSpace(config.AuthToken))
        {
            statusCode = (int)HttpStatusCode.OK;
            envelope = ApiEnvelope<object>.Ok(new { });
            return false;
        }

        var tokenValue = request.Headers["X-STS2-Token"];
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            statusCode = (int)HttpStatusCode.Unauthorized;
            envelope = ApiEnvelope<object>.Fail("UNAUTHORIZED", "Missing X-STS2-Token header.");
            return true;
        }

        if (!string.Equals(tokenValue, config.AuthToken, StringComparison.Ordinal))
        {
            statusCode = (int)HttpStatusCode.Forbidden;
            envelope = ApiEnvelope<object>.Fail("FORBIDDEN", "Token is invalid.");
            return true;
        }

        statusCode = (int)HttpStatusCode.OK;
        envelope = ApiEnvelope<object>.Ok(new { });
        return false;
    }

    private static async Task WriteJsonAsync<T>(HttpListenerResponse response, ApiEnvelope<T> envelope)
    {
        response.ContentType = "application/json; charset=utf-8";
        response.ContentEncoding = Encoding.UTF8;

        var buffer = JsonSerializer.SerializeToUtf8Bytes(envelope, ApiJson.SerializerOptions);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }
}
