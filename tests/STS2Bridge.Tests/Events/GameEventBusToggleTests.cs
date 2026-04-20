using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Tests.Events;

public sealed class GameEventBusToggleTests
{
    [Fact]
    public void Publish_should_skip_events_disabled_at_runtime()
    {
        var toggles = new EventToggleService(BridgeSettings.CreateDefault().SetEventEnabled(EventTypes.PlayerEnergyChanged, false));
        var bus = new GameEventBus(10, new[] { EventTypes.PlayerEnergyChanged }, toggles.IsEventEnabled);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerEnergyChanged, "run-1", 1, "Combat", new { delta = -1 }));

        Assert.Empty(bus.GetRecentEvents(10));
    }

    [Fact]
    public void Publish_should_pick_up_toggle_changes_without_recreating_bus()
    {
        var toggles = new EventToggleService(BridgeSettings.CreateDefault().SetEventEnabled(EventTypes.PlayerEnergyChanged, false));
        var bus = new GameEventBus(10, new[] { EventTypes.PlayerEnergyChanged }, toggles.IsEventEnabled);

        toggles.SetEventEnabled(EventTypes.PlayerEnergyChanged, true);
        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerEnergyChanged, "run-1", 1, "Combat", new { delta = -1 }));

        Assert.Single(bus.GetRecentEvents(10));
    }
}
