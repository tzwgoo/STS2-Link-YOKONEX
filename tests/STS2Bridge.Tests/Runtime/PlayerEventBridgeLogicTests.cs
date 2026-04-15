using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class PlayerEventBridgeLogicTests
{
    [Fact]
    public void PublishHpChanged_should_emit_hp_changed_and_damaged_for_negative_delta()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-1",
            Floor = 12,
            RoomType = "Combat",
            Player = new PlayerStateDto(70, 80, 3, 9, 99)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 61,
            MaxHp = 80,
            Block = 9
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -9);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item =>
            {
                Assert.Equal(EventTypes.PlayerHpChanged, item.Type);
                Assert.Equal(-9, GetInt(item.Payload, "delta"));
                Assert.Equal(61, GetInt(item.Payload, "currentHp"));
                Assert.Equal(80, GetInt(item.Payload, "maxHp"));
                Assert.Equal(9, GetInt(item.Payload, "block"));
            },
            item =>
            {
                Assert.Equal(EventTypes.PlayerDamaged, item.Type);
                Assert.Equal(9, GetInt(item.Payload, "amount"));
                Assert.Equal(61, GetInt(item.Payload, "currentHp"));
            });

        var snapshot = stateStore.GetSnapshot();
        Assert.Equal(61, snapshot.Player.Hp);
        Assert.Equal(80, snapshot.Player.MaxHp);
        Assert.Equal(9, snapshot.Player.Block);
    }

    [Fact]
    public void PublishHpChanged_should_emit_hp_changed_and_healed_for_positive_delta()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-2",
            Floor = 3,
            RoomType = "Rest",
            Player = new PlayerStateDto(40, 80, 3, 0, 50)
        });

        var creature = new FakeLowercasePlayerCreature
        {
            playerId = "silent",
            currentHp = 52,
            maxHp = 80,
            block = 0
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, 12);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item =>
            {
                Assert.Equal(EventTypes.PlayerHpChanged, item.Type);
                Assert.Equal(12, GetInt(item.Payload, "delta"));
                Assert.Equal("silent", GetString(item.Payload, "playerId"));
            },
            item =>
            {
                Assert.Equal(EventTypes.PlayerHealed, item.Type);
                Assert.Equal(12, GetInt(item.Payload, "amount"));
            });

        Assert.Equal(52, stateStore.GetSnapshot().Player.Hp);
    }

    [Fact]
    public void PublishBlockChanged_should_emit_block_changed_and_refresh_snapshot()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-3",
            Floor = 20,
            RoomType = "Combat",
            Player = new PlayerStateDto(55, 70, 3, 4, 10)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "watcher",
            CurrentHp = 55,
            MaxHp = 70,
            Block = 11
        };

        var published = PlayerEventBridgeLogic.PublishBlockChanged(eventBus, stateStore, creature, 7, "gained");

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        var gameEvent = Assert.Single(events);
        Assert.Equal(EventTypes.PlayerBlockChanged, gameEvent.Type);
        Assert.Equal(7, GetInt(gameEvent.Payload, "delta"));
        Assert.Equal(11, GetInt(gameEvent.Payload, "block"));
        Assert.Equal("gained", GetString(gameEvent.Payload, "reason"));

        var snapshot = stateStore.GetSnapshot();
        Assert.Equal(55, snapshot.Player.Hp);
        Assert.Equal(11, snapshot.Player.Block);
    }

    [Fact]
    public void PublishBlockChanged_should_support_block_cleared_when_delta_is_unknown()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(22, 60, 3, 5, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "defect",
            CurrentHp = 22,
            MaxHp = 60,
            Block = 0
        };

        var published = PlayerEventBridgeLogic.PublishBlockChanged(eventBus, stateStore, creature, null, "cleared");

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerBlockChanged, gameEvent.Type);
        Assert.Null(GetNullableInt(gameEvent.Payload, "delta"));
        Assert.Equal(0, GetInt(gameEvent.Payload, "block"));
        Assert.Equal("cleared", GetString(gameEvent.Payload, "reason"));
        Assert.Equal(0, stateStore.GetSnapshot().Player.Block);
    }

    [Fact]
    public void PublishBlockCleared_should_emit_dedicated_event()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(22, 60, 3, 5, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "defect",
            CurrentHp = 22,
            MaxHp = 60,
            Block = 0
        };

        var published = PlayerEventBridgeLogic.PublishBlockCleared(eventBus, stateStore, creature);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item => Assert.Equal(EventTypes.PlayerBlockChanged, item.Type),
            item =>
            {
                Assert.Equal(EventTypes.PlayerBlockCleared, item.Type);
                Assert.Equal("defect", GetString(item.Payload, "playerId"));
                Assert.Equal(0, GetInt(item.Payload, "block"));
            });
    }

    [Fact]
    public void PublishBlockLossFromTransition_should_emit_negative_delta_when_block_decreases()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(33, 60, 3, 9, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 33,
            MaxHp = 60,
            Block = 4
        };

        var published = PlayerEventBridgeLogic.PublishBlockLossFromTransition(eventBus, stateStore, creature, 9, "lost");

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerBlockChanged, gameEvent.Type);
        Assert.Equal(-5, GetInt(gameEvent.Payload, "delta"));
        Assert.Equal(4, GetInt(gameEvent.Payload, "block"));
        Assert.Equal("lost", GetString(gameEvent.Payload, "reason"));
        Assert.Equal(4, stateStore.GetSnapshot().Player.Block);
    }

    [Fact]
    public void PublishBlockBrokenFromTransition_should_emit_block_changed_and_block_broken()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(30, 50, 3, 8, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "watcher",
            CurrentHp = 30,
            MaxHp = 50,
            Block = 0
        };

        var published = PlayerEventBridgeLogic.PublishBlockBrokenFromTransition(eventBus, stateStore, creature, 8);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item =>
            {
                Assert.Equal(EventTypes.PlayerBlockChanged, item.Type);
                Assert.Equal(-8, GetInt(item.Payload, "delta"));
                Assert.Equal("broken", GetString(item.Payload, "reason"));
            },
            item =>
            {
                Assert.Equal(EventTypes.PlayerBlockBroken, item.Type);
                Assert.Equal("watcher", GetString(item.Payload, "playerId"));
                Assert.Equal(8, GetInt(item.Payload, "previousBlock"));
                Assert.Equal(0, GetInt(item.Payload, "block"));
            });
    }

    [Fact]
    public void PublishPlayerDied_should_emit_player_died()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-death",
            Floor = 17,
            RoomType = "Combat",
            Player = new PlayerStateDto(10, 70, 3, 2, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 0,
            MaxHp = 70,
            Block = 0
        };

        var published = PlayerEventBridgeLogic.PublishPlayerDied(eventBus, stateStore, creature, wasRemovalPrevented: false);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerDied, gameEvent.Type);
        Assert.Equal("ironclad", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal(0, GetInt(gameEvent.Payload, "currentHp"));
        Assert.Equal(70, GetInt(gameEvent.Payload, "maxHp"));
        Assert.Equal(0, GetInt(gameEvent.Payload, "block"));
        Assert.False(GetBool(gameEvent.Payload, "wasRemovalPrevented"));

        var snapshot = stateStore.GetSnapshot();
        Assert.Equal(0, snapshot.Player.Hp);
        Assert.Equal(0, snapshot.Player.Block);
    }

    [Fact]
    public void PublishPlayerDied_should_ignore_non_player_creatures()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(30, 40, 3, 2, 0)
        });

        var published = PlayerEventBridgeLogic.PublishPlayerDied(
            eventBus,
            stateStore,
            new FakeEnemyCreature
            {
                MonsterId = "slime",
                CurrentHp = 0,
                MaxHp = 10,
                Block = 0
            },
            wasRemovalPrevented: false);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    [Fact]
    public void PublishBlockLossFromTransition_should_skip_when_block_does_not_decrease()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(20, 50, 3, 2, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "defect",
            CurrentHp = 20,
            MaxHp = 50,
            Block = 5
        };

        var published = PlayerEventBridgeLogic.PublishBlockLossFromTransition(eventBus, stateStore, creature, 2, "lost");

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
        Assert.Equal(2, stateStore.GetSnapshot().Player.Block);
    }

    [Fact]
    public void PublishHpChanged_should_ignore_non_player_creatures()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(30, 40, 3, 2, 0)
        });

        var published = PlayerEventBridgeLogic.PublishHpChanged(
            eventBus,
            stateStore,
            new FakeEnemyCreature
            {
                MonsterId = "slime",
                CurrentHp = 10,
                MaxHp = 10,
                Block = 0
            },
            -3);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
        Assert.Equal(30, stateStore.GetSnapshot().Player.Hp);
    }

    private static int GetInt(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<int>(value);
    }

    private static int? GetNullableInt(object payload, string propertyName)
    {
        return payload.GetType().GetProperty(propertyName)?.GetValue(payload) as int?;
    }

    private static string GetString(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<string>(value);
    }

    private static bool GetBool(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<bool>(value);
    }

    private sealed class FakePlayerCreature
    {
        public string? PlayerId { get; init; }

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }

    private sealed class FakeLowercasePlayerCreature
    {
        public string? playerId { get; init; }

        public int currentHp { get; init; }

        public int maxHp { get; init; }

        public int block { get; init; }
    }

    private sealed class FakeEnemyCreature
    {
        public string MonsterId { get; init; } = string.Empty;

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }
}
