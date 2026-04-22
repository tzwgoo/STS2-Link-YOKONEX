namespace STS2Bridge.Tests.Events;

public sealed class GameEventBusLogTests
{
    [Fact]
    public void RecentVersion_should_increase_when_recent_events_change()
    {
        var bus = new GameEventBus(10, new[] { EventTypes.PlayerDamaged });

        var initialVersion = bus.RecentVersion;
        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "Combat", new { amount = 2 }));
        var afterPublishVersion = bus.RecentVersion;
        bus.ClearRecentEvents();
        var afterClearVersion = bus.RecentVersion;

        Assert.True(afterPublishVersion > initialVersion);
        Assert.True(afterClearVersion > afterPublishVersion);
    }

    [Fact]
    public void ClearRecentEvents_should_remove_all_recent_items()
    {
        var bus = new GameEventBus(10, new[] { EventTypes.PlayerDamaged, EventTypes.PlayerEnergyChanged });

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "Combat", new { amount = 2 }));
        bus.Publish(new GameEvent("evt-2", EventTypes.PlayerEnergyChanged, "run-1", 1, "Combat", new { delta = -1, energy = 2, maxEnergy = 3 }));

        bus.ClearRecentEvents();

        Assert.Empty(bus.GetRecentEvents(10));
    }
}
