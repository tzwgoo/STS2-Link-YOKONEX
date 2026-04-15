using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class RewardEventBridgeLogicTests
{
    [Fact]
    public void PublishRewardSelected_should_emit_reward_selected_for_special_card_reward()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-reward-card",
            Floor = 11,
            RoomType = "CombatReward"
        });

        var published = RewardEventBridgeLogic.PublishRewardSelected(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "ironclad"
            },
            new FakeReward
            {
                RewardType = FakeRewardType.SpecialCard,
                SpecialCard = new FakeCard
                {
                    Id = "uppercut",
                    Name = "Uppercut"
                }
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.RewardSelected, gameEvent.Type);
        Assert.Equal("ironclad", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal("specialcard", GetString(gameEvent.Payload, "rewardType"));
        Assert.Equal("card", GetString(gameEvent.Payload, "rewardItemType"));
        Assert.Equal("uppercut", GetString(gameEvent.Payload, "rewardId"));
        Assert.Equal("Uppercut", GetString(gameEvent.Payload, "rewardName"));
    }

    [Fact]
    public void PublishRewardSelected_should_emit_reward_selected_for_gold_reward()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-reward-gold",
            Floor = 5,
            RoomType = "CombatReward"
        });

        var published = RewardEventBridgeLogic.PublishRewardSelected(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "silent"
            },
            new FakeReward
            {
                RewardType = FakeRewardType.Gold,
                GoldAmount = 97,
                Source = "combat",
                OptionCount = 2
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal("gold", GetString(gameEvent.Payload, "rewardType"));
        Assert.Equal("gold", GetString(gameEvent.Payload, "rewardItemType"));
        Assert.Equal(97, GetInt(gameEvent.Payload, "goldAmount"));
        Assert.Equal("combat", GetString(gameEvent.Payload, "rewardSource"));
        Assert.Equal(2, GetInt(gameEvent.Payload, "optionCount"));
        Assert.False(GetBool(gameEvent.Payload, "wasGoldStolenBack"));
    }

    [Fact]
    public void PublishRewardSelected_should_support_player_fallback_and_card_count()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = RewardEventBridgeLogic.PublishRewardSelected(
            eventBus,
            stateStore,
            player: null,
            reward: new FakeCardReward
            {
                Player = new FakePlayer
                {
                    PlayerId = "silent"
                },
                RewardType = FakeRewardType.Card,
                Cards =
                [
                    new FakeCard
                    {
                        Id = "slice",
                        Name = "Slice"
                    },
                    new FakeCard
                    {
                        Id = "prepared",
                        Name = "Prepared"
                    }
                ],
                Source = "event_choice",
                OptionCount = 3
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal("silent", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal("card", GetString(gameEvent.Payload, "rewardItemType"));
        Assert.Equal("event_choice", GetString(gameEvent.Payload, "rewardSource"));
        Assert.Equal(3, GetInt(gameEvent.Payload, "optionCount"));
        Assert.Equal(2, GetInt(gameEvent.Payload, "cardCount"));
    }

    [Fact]
    public void PublishRewardSelected_should_ignore_unknown_reward_shape()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = RewardEventBridgeLogic.PublishRewardSelected(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "watcher"
            },
            new object());

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

    private sealed class FakePlayer
    {
        public string? PlayerId { get; init; }
    }

    private sealed class FakeReward
    {
        public FakeRewardType RewardType { get; init; }

        public FakeCard? SpecialCard { get; init; }

        public int GoldAmount { get; init; }

        public string? Source { get; init; }

        public int OptionCount { get; init; }

        public bool WasGoldStolenBack { get; init; }
    }

    private sealed class FakeCardReward
    {
        public FakeRewardType RewardType { get; init; }

        public FakePlayer? Player { get; init; }

        public IReadOnlyList<FakeCard>? Cards { get; init; }

        public string? Source { get; init; }

        public int OptionCount { get; init; }
    }

    private sealed class FakeCard
    {
        public string? Id { get; init; }

        public string? Name { get; init; }
    }

    private enum FakeRewardType
    {
        Gold,
        SpecialCard,
        Card
    }
}
