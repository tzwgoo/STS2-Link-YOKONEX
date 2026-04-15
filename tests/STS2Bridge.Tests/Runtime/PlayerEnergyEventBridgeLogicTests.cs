using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class PlayerEnergyEventBridgeLogicTests
{
    [Fact]
    public void PublishEnergyChanged_should_emit_energy_changed_and_refresh_state()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-energy",
            Floor = 14,
            RoomType = "Combat",
            Player = new PlayerStateDto(50, 70, 3, 2, 99)
        });

        var published = PlayerEnergyEventBridgeLogic.PublishEnergyChanged(
            eventBus,
            stateStore,
            new FakePlayerCombatState
            {
                _player = new FakePlayer
                {
                    PlayerId = "watcher"
                },
                Energy = 1,
                MaxEnergy = 3
            },
            previousEnergy: 3);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerEnergyChanged, gameEvent.Type);
        Assert.Equal("watcher", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal(-2, GetInt(gameEvent.Payload, "delta"));
        Assert.Equal(1, GetInt(gameEvent.Payload, "energy"));
        Assert.Equal(3, GetInt(gameEvent.Payload, "maxEnergy"));
        Assert.Equal(1, stateStore.GetSnapshot().Player.Energy);
    }

    [Fact]
    public void PublishEnergyChanged_should_ignore_no_op_changes()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(40, 60, 2, 0, 10)
        });

        var published = PlayerEnergyEventBridgeLogic.PublishEnergyChanged(
            eventBus,
            stateStore,
            new FakePlayerCombatState
            {
                _player = new FakePlayer
                {
                    PlayerId = "silent"
                },
                Energy = 2,
                MaxEnergy = 3
            },
            previousEnergy: 2);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
        Assert.Equal(2, stateStore.GetSnapshot().Player.Energy);
    }

    [Fact]
    public void PublishEnergyChanged_should_ignore_unknown_shape()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = PlayerEnergyEventBridgeLogic.PublishEnergyChanged(eventBus, stateStore, new object(), previousEnergy: 1);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    private static int GetInt(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<int>(value);
    }

    private static string GetString(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<string>(value);
    }

    private sealed class FakePlayerCombatState
    {
        public FakePlayer? _player { get; init; }

        public int Energy { get; init; }

        public int MaxEnergy { get; init; }
    }

    private sealed class FakePlayer
    {
        public string? PlayerId { get; init; }
    }
}
