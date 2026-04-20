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
    public void PublishDamageReceived_should_not_duplicate_player_damaged_after_hp_changed_for_same_hit()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-player-damage-dedupe",
            Floor = 12,
            RoomType = "Combat",
            Player = new PlayerStateDto(70, 80, 3, 10, 99)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 68,
            MaxHp = 80,
            Block = 0
        };

        var hpPublished = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -2);
        var damagePublished = PlayerEventBridgeLogic.PublishDamageReceived(
            eventBus,
            stateStore,
            creature,
            new FakeDamageResult
            {
                BlockedDamage = 10,
                UnblockedDamage = 2,
                WasBlockBroken = true,
                WasFullyBlocked = false
            });

        Assert.True(hpPublished);
        Assert.False(damagePublished);

        var events = eventBus.GetRecentEvents(10);
        Assert.Equal(2, events.Count);
        Assert.Equal(EventTypes.PlayerHpChanged, events[0].Type);
        Assert.Equal(EventTypes.PlayerDamaged, events[1].Type);
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
    public void PublishHpChanged_should_support_player_id_from_nested_state()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-nested",
            Floor = 8,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 4, 20)
        });

        var creature = new FakePlayerCreatureWithNestedState
        {
            CurrentHp = 30,
            MaxHp = 80,
            Block = 4,
            State = new FakePlayerState
            {
                playerId = "ironclad"
            }
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -5);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item => Assert.Equal("ironclad", GetString(item.Payload, "playerId")),
            item => Assert.Equal("ironclad", GetString(item.Payload, "playerId")));
    }

    [Fact]
    public void PublishHpChanged_should_support_numeric_player_id_from_nested_state()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-numeric-player-id",
            Floor = 8,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 4, 20)
        });

        var creature = new FakePlayerCreatureWithNumericNestedState
        {
            State = new FakeNumericPlayerState
            {
                playerId = 1UL,
                currentHp = 30,
                maxHp = 80,
                block = 4
            }
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -5);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item => Assert.Equal("1", GetString(item.Payload, "playerId")),
            item => Assert.Equal("1", GetString(item.Payload, "playerId")));
    }

    [Fact]
    public void PublishHpChanged_should_support_all_player_stats_from_nested_state()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-nested-stats",
            Floor = 9,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 20)
        });

        var creature = new FakePlayerCreatureWithStateBackedStats
        {
            State = new FakePlayerStateWithStats
            {
                playerId = "watcher",
                currentHp = 27,
                maxHp = 80,
                block = 2
            }
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -8);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item =>
            {
                Assert.Equal(EventTypes.PlayerHpChanged, item.Type);
                Assert.Equal("watcher", GetString(item.Payload, "playerId"));
                Assert.Equal(27, GetInt(item.Payload, "currentHp"));
                Assert.Equal(2, GetInt(item.Payload, "block"));
            },
            item =>
            {
                Assert.Equal(EventTypes.PlayerDamaged, item.Type);
                Assert.Equal(8, GetInt(item.Payload, "amount"));
            });
    }

    [Fact]
    public void IsPlayerCreature_should_support_player_id_from_nested_state()
    {
        var creature = new FakePlayerCreatureWithNestedState
        {
            CurrentHp = 30,
            MaxHp = 80,
            Block = 4,
            State = new FakePlayerState
            {
                playerId = "ironclad"
            }
        };

        var result = PlayerEventBridgeLogic.IsPlayerCreature(creature);

        Assert.True(result);
    }

    [Fact]
    public void PublishHpChanged_should_fall_back_to_combat_state_player_mapping()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-combat-state-fallback",
            Floor = 10,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 20)
        });

        var creature = new FakeCombatLinkedCreature
        {
            State = new FakeCombatLinkedCreatureState
            {
                currentHp = 29,
                maxHp = 80,
                block = 3
            }
        };

        var combatState = new FakeCombatState();
        combatState.PlayerCreatures = [creature];
        combatState.Players = [new FakeCombatPlayer { NetId = 7UL, Creature = creature }];
        creature.CombatState = combatState;

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -6);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item =>
            {
                Assert.Equal(EventTypes.PlayerHpChanged, item.Type);
                Assert.Equal("7", GetString(item.Payload, "playerId"));
                Assert.Equal(29, GetInt(item.Payload, "currentHp"));
            },
            item => Assert.Equal(EventTypes.PlayerDamaged, item.Type));
    }

    [Fact]
    public void PublishHpChanged_should_use_player_creature_match_when_player_creatures_collection_is_missing()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-single-player-fallback",
            Floor = 11,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 20)
        });

        var creature = new FakeCombatLinkedCreature
        {
            State = new FakeCombatLinkedCreatureState
            {
                currentHp = 31,
                maxHp = 80,
                block = 2
            },
            CombatId = 42
        };

        creature.CombatState = new FakeCombatState
        {
            Players = [new FakeCombatPlayer { NetId = 9UL, Creature = creature }]
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -4);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        Assert.Collection(
            events,
            item => Assert.Equal("9", GetString(item.Payload, "playerId")),
            item => Assert.Equal("9", GetString(item.Payload, "playerId")));
    }

    [Fact]
    public void PublishHpChanged_should_not_treat_unmapped_combat_creature_as_player()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-unmapped-creature",
            Floor = 11,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 20)
        });

        var creature = new FakeCombatLinkedCreature
        {
            State = new FakeCombatLinkedCreatureState
            {
                currentHp = 19,
                maxHp = 40,
                block = 0
            },
            CombatId = 77
        };

        creature.CombatState = new FakeCombatState
        {
            Players = [new FakeCombatPlayer { NetId = 9UL }]
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, creature, -4);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
        Assert.Equal(35, stateStore.GetSnapshot().Player.Hp);
    }

    [Fact]
    public void PublishHpChanged_should_not_match_different_creatures_only_by_same_combat_id()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-same-combat-id",
            Floor = 11,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 20)
        });

        var playerCreature = new FakeCombatLinkedCreature
        {
            State = new FakeCombatLinkedCreatureState
            {
                currentHp = 35,
                maxHp = 80,
                block = 6
            },
            CombatId = 42
        };

        var enemyCreature = new FakeCombatLinkedCreature
        {
            State = new FakeCombatLinkedCreatureState
            {
                currentHp = 18,
                maxHp = 40,
                block = 0
            },
            CombatId = 42
        };

        enemyCreature.CombatState = new FakeCombatState
        {
            Players = [new FakeCombatPlayer { NetId = 9UL, Creature = playerCreature }],
            PlayerCreatures = [playerCreature]
        };

        var published = PlayerEventBridgeLogic.PublishHpChanged(eventBus, stateStore, enemyCreature, -2);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
        Assert.Equal(35, stateStore.GetSnapshot().Player.Hp);
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
    public void PublishBlockLossFromTransition_should_support_damage_reason()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(41, 70, 3, 12, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 41,
            MaxHp = 70,
            Block = 7
        };

        var published = PlayerEventBridgeLogic.PublishBlockLossFromTransition(eventBus, stateStore, creature, 12, "damaged");

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerBlockChanged, gameEvent.Type);
        Assert.Equal(-5, GetInt(gameEvent.Payload, "delta"));
        Assert.Equal("damaged", GetString(gameEvent.Payload, "reason"));
    }

    [Fact]
    public void PublishBlockLossFromTransition_should_support_nested_state_block_values()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(41, 70, 3, 12, 0)
        });

        var creature = new FakePlayerCreatureWithStateBackedStats
        {
            State = new FakePlayerStateWithStats
            {
                playerId = "ironclad",
                currentHp = 41,
                maxHp = 70,
                block = 7
            }
        };

        var published = PlayerEventBridgeLogic.PublishBlockLossFromTransition(eventBus, stateStore, creature, 12, "damaged");

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerBlockChanged, gameEvent.Type);
        Assert.Equal(-5, GetInt(gameEvent.Payload, "delta"));
        Assert.Equal(7, GetInt(gameEvent.Payload, "block"));
        Assert.Equal("damaged", GetString(gameEvent.Payload, "reason"));
    }

    [Fact]
    public void PublishBlockBrokenFromTransition_should_emit_only_block_broken()
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

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerBlockBroken, gameEvent.Type);
        Assert.Equal("watcher", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal(8, GetInt(gameEvent.Payload, "previousBlock"));
        Assert.Equal(0, GetInt(gameEvent.Payload, "block"));
    }

    [Fact]
    public void PublishDamageReceived_should_emit_only_player_damaged()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-damage-result",
            Floor = 13,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 0)
        });

        var creature = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 30,
            MaxHp = 80,
            Block = 1
        };

        var result = new FakeDamageResult
        {
            BlockedDamage = 5,
            UnblockedDamage = 5,
            WasBlockBroken = false,
            WasFullyBlocked = false
        };

        var published = PlayerEventBridgeLogic.PublishDamageReceived(eventBus, stateStore, creature, result);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.PlayerDamaged, gameEvent.Type);
        Assert.Equal(5, GetInt(gameEvent.Payload, "amount"));
        Assert.Equal(30, GetInt(gameEvent.Payload, "currentHp"));
    }

    [Fact]
    public void PublishDamageReceived_should_support_nested_state_stats()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-damage-result-nested",
            Floor = 13,
            RoomType = "Combat",
            Player = new PlayerStateDto(35, 80, 3, 6, 0)
        });

        var creature = new FakePlayerCreatureWithStateBackedStats
        {
            State = new FakePlayerStateWithStats
            {
                playerId = "watcher",
                currentHp = 28,
                maxHp = 80,
                block = 0
            }
        };

        var result = new FakeDamageResult
        {
            BlockedDamage = 2,
            UnblockedDamage = 7,
            WasBlockBroken = true,
            WasFullyBlocked = false
        };

        var published = PlayerEventBridgeLogic.PublishDamageReceived(eventBus, stateStore, creature, result);

        Assert.True(published);

        var events = eventBus.GetRecentEvents(10);
        var gameEvent = Assert.Single(events);
        Assert.Equal(EventTypes.PlayerDamaged, gameEvent.Type);
        Assert.Equal("watcher", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal(7, GetInt(gameEvent.Payload, "amount"));
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

    private sealed class FakePlayerCreatureWithNestedState
    {
        public FakePlayerState? State { get; init; }

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }

    private sealed class FakePlayerState
    {
        public string? playerId { get; init; }
    }

    private sealed class FakePlayerCreatureWithStateBackedStats
    {
        public FakePlayerStateWithStats? State { get; init; }
    }

    private sealed class FakePlayerStateWithStats
    {
        public string? playerId { get; init; }

        public int currentHp { get; init; }

        public int maxHp { get; init; }

        public int block { get; init; }
    }

    private sealed class FakePlayerCreatureWithNumericNestedState
    {
        public FakeNumericPlayerState? State { get; init; }
    }

    private sealed class FakeNumericPlayerState
    {
        public ulong playerId { get; init; }

        public int currentHp { get; init; }

        public int maxHp { get; init; }

        public int block { get; init; }
    }

    private sealed class FakeCombatLinkedCreature
    {
        public FakeCombatState? CombatState { get; set; }

        public FakeCombatLinkedCreatureState? State { get; init; }

        public uint? CombatId { get; init; }
    }

    private sealed class FakeCombatLinkedCreatureState
    {
        public int currentHp { get; init; }

        public int maxHp { get; init; }

        public int block { get; init; }
    }

    private sealed class FakeCombatState
    {
        public IReadOnlyList<object> PlayerCreatures { get; set; } = [];

        public IReadOnlyList<object> Players { get; set; } = [];
    }

    private sealed class FakeCombatPlayer
    {
        public ulong NetId { get; init; }

        public object? Creature { get; init; }
    }

    private sealed class FakeDamageResult
    {
        public int BlockedDamage { get; init; }

        public int UnblockedDamage { get; init; }

        public bool WasBlockBroken { get; init; }

        public bool WasFullyBlocked { get; init; }
    }
}
