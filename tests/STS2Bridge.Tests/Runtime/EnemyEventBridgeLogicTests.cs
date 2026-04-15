using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class EnemyEventBridgeLogicTests
{
    [Fact]
    public void PublishHpChanged_should_emit_enemy_hp_changed_and_refresh_enemy_snapshot()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-enemy",
            Floor = 9,
            RoomType = "Combat",
            Enemies =
            [
                new EnemyStateDto("jawworm-01", "Jaw Worm", 42, 46, 7, true)
            ]
        });

        var published = EnemyEventBridgeLogic.PublishHpChanged(
            eventBus,
            stateStore,
            new FakeEnemyCreature
            {
                MonsterId = "jawworm-01",
                Name = "Jaw Worm",
                CurrentHp = 31,
                MaxHp = 46,
                Block = 0
            },
            delta: -11);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        var gameEvent = Assert.Single(events);
        Assert.Equal(EventTypes.EnemyHpChanged, gameEvent.Type);
        Assert.Equal("jawworm-01", GetString(gameEvent.Payload, "enemyId"));
        Assert.Equal("Jaw Worm", GetString(gameEvent.Payload, "enemyName"));
        Assert.Equal(-11, GetInt(gameEvent.Payload, "delta"));
        Assert.Equal(31, GetInt(gameEvent.Payload, "currentHp"));
        Assert.Equal(46, GetInt(gameEvent.Payload, "maxHp"));
        Assert.Equal(0, GetInt(gameEvent.Payload, "block"));

        var enemy = Assert.Single(stateStore.GetSnapshot().Enemies);
        Assert.Equal(31, enemy.Hp);
        Assert.Equal(0, enemy.Block);
    }

    [Fact]
    public void PublishDamaged_should_emit_enemy_damaged_with_damage_result_details()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-enemy-dmg",
            Floor = 10,
            RoomType = "Combat"
        });

        var published = EnemyEventBridgeLogic.PublishDamaged(
            eventBus,
            stateStore,
            new FakeEnemyCreature
            {
                MonsterId = "cultist-01",
                Name = "Cultist",
                CurrentHp = 35,
                MaxHp = 50,
                Block = 0
            },
            new FakeDamageResult
            {
                BlockedDamage = 3,
                UnblockedDamage = 12,
                WasBlockBroken = true,
                WasFullyBlocked = false
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.EnemyDamaged, gameEvent.Type);
        Assert.Equal("cultist-01", GetString(gameEvent.Payload, "enemyId"));
        Assert.Equal("Cultist", GetString(gameEvent.Payload, "enemyName"));
        Assert.Equal(15, GetInt(gameEvent.Payload, "amount"));
        Assert.Equal(3, GetInt(gameEvent.Payload, "blockedDamage"));
        Assert.Equal(12, GetInt(gameEvent.Payload, "unblockedDamage"));
        Assert.True(GetBool(gameEvent.Payload, "wasBlockBroken"));
        Assert.False(GetBool(gameEvent.Payload, "wasFullyBlocked"));
    }

    [Fact]
    public void PublishHpChanged_should_ignore_player_shapes()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = EnemyEventBridgeLogic.PublishHpChanged(
            eventBus,
            stateStore,
            new FakePlayerCreature
            {
                PlayerId = "ironclad",
                CurrentHp = 50,
                MaxHp = 80,
                Block = 0
            },
            delta: -5);

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

    private static bool GetBool(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<bool>(value);
    }

    private sealed class FakeEnemyCreature
    {
        public string? MonsterId { get; init; }

        public string? Name { get; init; }

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }

    private sealed class FakePlayerCreature
    {
        public string? PlayerId { get; init; }

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }

    private sealed class FakeDamageResult
    {
        public int BlockedDamage { get; init; }

        public int UnblockedDamage { get; init; }

        public bool WasBlockBroken { get; init; }

        public bool WasFullyBlocked { get; init; }
    }
}
