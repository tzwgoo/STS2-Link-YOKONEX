using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class CardEventBridgeLogicTests
{
    [Fact]
    public void PublishCardPlayed_should_emit_card_played_with_card_and_target_details()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-card",
            Floor = 9,
            RoomType = "Combat",
            Player = new PlayerStateDto(50, 70, 2, 0, 20)
        });

        var cardPlay = new FakeCardPlay
        {
            Card = new FakeCard
            {
                Id = "strike_red",
                Name = "Strike",
                Cost = 1
            },
            Target = new FakeTarget
            {
                Id = "cultist-01"
            },
            PlayIndex = 3,
            PlayCount = 1,
            IsAutoPlay = false
        };

        var published = CardEventBridgeLogic.PublishCardPlayed(eventBus, stateStore, cardPlay);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.CardPlayed, gameEvent.Type);
        Assert.Equal("strike_red", GetString(gameEvent.Payload, "cardId"));
        Assert.Equal("Strike", GetString(gameEvent.Payload, "cardName"));
        Assert.Equal(1, GetInt(gameEvent.Payload, "cost"));
        Assert.Equal("cultist-01", GetString(gameEvent.Payload, "targetId"));
        Assert.Equal(3, GetInt(gameEvent.Payload, "playIndex"));
        Assert.Equal(1, GetInt(gameEvent.Payload, "playCount"));
        Assert.False(GetBool(gameEvent.Payload, "isAutoPlay"));
    }

    [Fact]
    public void PublishCardPlayed_should_support_action_fallback_shape()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-card-2",
            Floor = 2,
            RoomType = "Combat"
        });

        var cardPlay = new FakeActionCardPlay
        {
            CardModelId = "bash_red",
            TargetId = "jawworm-01"
        };

        var published = CardEventBridgeLogic.PublishCardPlayed(eventBus, stateStore, cardPlay);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal("bash_red", GetString(gameEvent.Payload, "cardId"));
        Assert.Equal("jawworm-01", GetString(gameEvent.Payload, "targetId"));
    }

    [Fact]
    public void PublishCardPlayed_should_ignore_unknown_shapes()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = CardEventBridgeLogic.PublishCardPlayed(eventBus, stateStore, new object());

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    private static int GetInt(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<int>(value);
    }

    private static bool GetBool(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<bool>(value);
    }

    private static string? GetString(object payload, string propertyName)
    {
        return payload.GetType().GetProperty(propertyName)?.GetValue(payload) as string;
    }

    private sealed class FakeCardPlay
    {
        public FakeCard? Card { get; init; }

        public FakeTarget? Target { get; init; }

        public int PlayIndex { get; init; }

        public int PlayCount { get; init; }

        public bool IsAutoPlay { get; init; }
    }

    private sealed class FakeActionCardPlay
    {
        public string? CardModelId { get; init; }

        public string? TargetId { get; init; }
    }

    private sealed class FakeCard
    {
        public string? Id { get; init; }

        public string? Name { get; init; }

        public int Cost { get; init; }
    }

    private sealed class FakeTarget
    {
        public string? Id { get; init; }
    }
}
