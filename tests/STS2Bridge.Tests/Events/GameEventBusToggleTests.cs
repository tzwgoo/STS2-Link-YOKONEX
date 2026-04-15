using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Tests.Events;

public sealed class GameEventBusToggleTests
{
    [Fact]
    public void Publish_should_skip_events_disabled_at_runtime()
    {
        var toggles = new EventToggleService(BridgeSettings.CreateDefault().SetEventEnabled(EventTypes.CardPlayed, false));
        var bus = new GameEventBus(10, new[] { EventTypes.CardPlayed }, toggles.IsEventEnabled);

        bus.Publish(new GameEvent("evt-1", EventTypes.CardPlayed, "run-1", 1, "Combat", new { }));

        Assert.Empty(bus.GetRecentEvents(10));
    }

    [Fact]
    public void Publish_should_pick_up_toggle_changes_without_recreating_bus()
    {
        var toggles = new EventToggleService(BridgeSettings.CreateDefault().SetEventEnabled(EventTypes.CardPlayed, false));
        var bus = new GameEventBus(10, new[] { EventTypes.CardPlayed }, toggles.IsEventEnabled);

        toggles.SetEventEnabled(EventTypes.CardPlayed, true);
        bus.Publish(new GameEvent("evt-1", EventTypes.CardPlayed, "run-1", 1, "Combat", new { }));

        Assert.Single(bus.GetRecentEvents(10));
    }
}
