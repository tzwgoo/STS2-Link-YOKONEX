using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class RestSiteEventBridgeLogicTests
{
    [Fact]
    public void PublishCardUpgraded_should_emit_card_upgraded_for_rest_site_smith()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-rest-site",
            Floor = 21,
            RoomType = "RestSite"
        });

        var runState = new FakeRunState
        {
            CurrentMapPointHistoryEntry = new FakeMapPointHistoryEntry
            {
                PlayerStats =
                [
                    new FakePlayerMapPointHistoryEntry
                    {
                        PlayerId = "ironclad",
                        UpgradedCards =
                        [
                            new FakeCard
                            {
                                Id = "bash_red",
                                Name = "Bash+",
                                CurrentUpgradeLevel = 1
                            }
                        ]
                    }
                ]
            }
        };

        var published = RestSiteEventBridgeLogic.PublishCardUpgraded(
            eventBus,
            stateStore,
            runState,
            new FakePlayer
            {
                PlayerId = "ironclad"
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.CardUpgraded, gameEvent.Type);
        Assert.Equal("ironclad", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal("bash_red", GetString(gameEvent.Payload, "cardId"));
        Assert.Equal("Bash+", GetString(gameEvent.Payload, "cardName"));
        Assert.Equal(0, GetInt(gameEvent.Payload, "upgradeLevelBefore"));
        Assert.Equal(1, GetInt(gameEvent.Payload, "upgradeLevelAfter"));
        Assert.Equal("rest_site_smith", GetString(gameEvent.Payload, "source"));
    }

    [Fact]
    public void PublishCardUpgraded_should_use_last_upgraded_card_when_multiple_exist()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-rest-site-2",
            Floor = 6,
            RoomType = "RestSite"
        });

        var runState = new FakeRunState
        {
            CurrentMapPointHistoryEntry = new FakeMapPointHistoryEntry
            {
                PlayerStats =
                [
                    new FakePlayerMapPointHistoryEntry
                    {
                        PlayerId = "silent",
                        UpgradedCards =
                        [
                            new FakeCard
                            {
                                Id = "neutralize",
                                Name = "Neutralize+",
                                CurrentUpgradeLevel = 1
                            },
                            new FakeCard
                            {
                                Id = "survivor",
                                Name = "Survivor++",
                                CurrentUpgradeLevel = 2
                            }
                        ]
                    }
                ]
            }
        };

        var published = RestSiteEventBridgeLogic.PublishCardUpgraded(
            eventBus,
            stateStore,
            runState,
            new FakePlayer
            {
                PlayerId = "silent"
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal("survivor", GetString(gameEvent.Payload, "cardId"));
        Assert.Equal(1, GetInt(gameEvent.Payload, "upgradeLevelBefore"));
        Assert.Equal(2, GetInt(gameEvent.Payload, "upgradeLevelAfter"));
    }

    [Fact]
    public void PublishCardUpgraded_should_ignore_missing_history_shape()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = RestSiteEventBridgeLogic.PublishCardUpgraded(
            eventBus,
            stateStore,
            runState: new object(),
            player: new FakePlayer
            {
                PlayerId = "watcher"
            });

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    [Fact]
    public void PublishCardUpgradedFromSource_should_emit_card_upgraded_for_generic_forge_source()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-forge",
            Floor = 9,
            RoomType = "Event"
        });

        var published = RestSiteEventBridgeLogic.PublishCardUpgradedFromSource(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "defect"
            },
            new FakeCard
            {
                Id = "zap_blue",
                Name = "Zap+",
                CurrentUpgradeLevel = 1
            },
            source: "forge",
            upgradeAmount: 1);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.CardUpgraded, gameEvent.Type);
        Assert.Equal("defect", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal("zap_blue", GetString(gameEvent.Payload, "cardId"));
        Assert.Equal("forge", GetString(gameEvent.Payload, "source"));
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

    private sealed class FakeRunState
    {
        public FakeMapPointHistoryEntry? CurrentMapPointHistoryEntry { get; init; }
    }

    private sealed class FakeMapPointHistoryEntry
    {
        public IReadOnlyList<FakePlayerMapPointHistoryEntry>? PlayerStats { get; init; }
    }

    private sealed class FakePlayerMapPointHistoryEntry
    {
        public string? PlayerId { get; init; }

        public IReadOnlyList<FakeCard>? UpgradedCards { get; init; }
    }

    private sealed class FakeCard
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public int CurrentUpgradeLevel { get; init; }
    }

    private sealed class FakePlayer
    {
        public string? PlayerId { get; init; }
    }
}
