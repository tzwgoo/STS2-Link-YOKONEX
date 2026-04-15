namespace STS2Bridge.Tests.Actions;

public sealed class ActionRouterTests
{
    [Fact]
    public async Task RouteAsync_should_reject_unknown_action()
    {
        var router = ActionRouter.CreateDefault(new BridgeConfig(), new MainThreadDispatcher(), new GameStateStore(), new GameEventBus());
        var response = await router.RouteAsync(new ActionRequest("req-1", "unknown_action", null), CancellationToken.None);

        Assert.False(response.Success);
        Assert.Equal("ACTION_NOT_ALLOWED", response.ErrorCode);
    }
}
