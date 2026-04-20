namespace STS2Bridge.Tests.Events;

public sealed class GameEventBusTests
{
    [Fact]
    public void Publish_should_store_recent_events_and_notify_subscribers()
    {
        var bus = new GameEventBus(2, new[] { EventTypes.CombatStarted, EventTypes.PlayerEnergyChanged });
        GameEvent? received = null;
        bus.Subscribe(evt => received = evt);

        var first = new GameEvent("evt-1", EventTypes.CombatStarted, "run-1", 1, "MonsterRoom", new { enemyCount = 2 });
        var second = new GameEvent("evt-2", EventTypes.PlayerEnergyChanged, "run-1", 1, "MonsterRoom", new { delta = -1, energy = 2, maxEnergy = 3 });
        var ignored = new GameEvent("evt-3", "ignored.type", "run-1", 1, "MonsterRoom", new { });

        bus.Publish(first);
        bus.Publish(second);
        bus.Publish(ignored);

        Assert.NotNull(received);
        Assert.Equal(second.EventId, received!.EventId);
        Assert.Equal(2, bus.GetRecentEvents(10).Count);
        Assert.DoesNotContain(bus.GetRecentEvents(10), item => item.EventId == ignored.EventId);
    }
}
