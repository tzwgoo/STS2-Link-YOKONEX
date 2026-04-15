namespace STS2Bridge.Tests.Api;

public sealed class LocalApiServerTests
{
    [Fact]
    public async Task Health_endpoint_should_return_success_envelope()
    {
        var config = BridgeConfig.CreateDefault() with
        {
            Port = GetFreePort()
        };
        var bus = new GameEventBus();
        var store = new GameStateStore();
        var router = ActionRouter.CreateDefault(config, new MainThreadDispatcher(), store, bus);
        await using var host = await LocalApiServer.StartRuntimeAsync(config, bus, store, router, CancellationToken.None);

        using var client = new HttpClient
        {
            BaseAddress = host.BaseUri
        };
        var response = await client.GetFromJsonAsync<ApiEnvelope<JsonElement>>("health");

        Assert.NotNull(response);
        Assert.True(response!.Success);
        Assert.True(response.Data.GetProperty("ok").GetBoolean());
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
