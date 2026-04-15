namespace STS2Bridge.Tests.Runtime;

public sealed class CombatManagerEventBridgeTests
{
    [Fact]
    public void Install_should_bridge_combat_manager_events_to_game_events()
    {
        var combatManager = new FakeCombatManager();
        var eventBus = new GameEventBus(20);
        var bridge = new STS2Bridge.Runtime.CombatManagerEventBridge(
            typeof(FakeCombatManager),
            () => combatManager,
            eventBus,
            () => "run-test",
            () => 7,
            () => "MonsterRoom");

        using var subscription = bridge.Install();

        combatManager.RaiseCombatSetUp();
        combatManager.RaiseTurnStarted();
        combatManager.RaiseCombatEnded();

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item => Assert.Equal(EventTypes.CombatStarted, item.Type),
            item => Assert.Equal(EventTypes.TurnStarted, item.Type),
            item => Assert.Equal(EventTypes.CombatEnded, item.Type));
        Assert.All(events, item => Assert.Equal("run-test", item.RunId));
        Assert.All(events, item => Assert.Equal(7, item.Floor));
    }

    [Fact]
    public void Install_should_be_noop_when_instance_is_missing()
    {
        var eventBus = new GameEventBus(20);
        var bridge = new STS2Bridge.Runtime.CombatManagerEventBridge(
            typeof(FakeCombatManager),
            () => null,
            eventBus,
            () => "run-test",
            () => 1,
            () => "Unknown");

        using var subscription = bridge.Install();

        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    private sealed class FakeCombatManager
    {
        public event Action? CombatSetUp;

        public event Action? CombatEnded;

        public event Action? TurnStarted;

        public void RaiseCombatSetUp() => CombatSetUp?.Invoke();

        public void RaiseCombatEnded() => CombatEnded?.Invoke();

        public void RaiseTurnStarted() => TurnStarted?.Invoke();
    }
}
