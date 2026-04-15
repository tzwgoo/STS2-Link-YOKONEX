using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class RoomEventBridgeLogicTests
{
    [Fact]
    public void PublishRoomEntered_should_emit_room_entered_and_refresh_state_room_type()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-room",
            Floor = 7,
            RoomType = "Unknown"
        });

        var published = RoomEventBridgeLogic.PublishRoomEntered(
            eventBus,
            stateStore,
            new FakeRoom
            {
                RoomType = FakeRoomType.Shop,
                ModelId = "merchant_room_01"
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.RoomEntered, gameEvent.Type);
        Assert.Equal("shop", GetString(gameEvent.Payload, "roomType"));
        Assert.Equal("merchant_room_01", GetString(gameEvent.Payload, "modelId"));
        Assert.Equal("hook.after_room_entered", GetString(gameEvent.Payload, "source"));
        Assert.Equal("shop", stateStore.GetSnapshot().RoomType);
    }

    [Fact]
    public void PublishRoomEntered_should_support_string_room_type()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = RoomEventBridgeLogic.PublishRoomEntered(
            eventBus,
            stateStore,
            new LowercaseRoom
            {
                roomType = "RestSite",
                modelId = "rest_site_02"
            });

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal("restsite", GetString(gameEvent.Payload, "roomType"));
        Assert.Equal("rest_site_02", GetString(gameEvent.Payload, "modelId"));
    }

    [Fact]
    public void PublishRoomEntered_should_ignore_unknown_room_shape()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();

        var published = RoomEventBridgeLogic.PublishRoomEntered(eventBus, stateStore, new object());

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    private static string GetString(object payload, string propertyName)
    {
        var value = payload.GetType().GetProperty(propertyName)?.GetValue(payload);
        return Assert.IsType<string>(value);
    }

    private sealed class FakeRoom
    {
        public FakeRoomType RoomType { get; init; }

        public string? ModelId { get; init; }
    }

    private sealed class LowercaseRoom
    {
        public string? roomType { get; init; }

        public string? modelId { get; init; }
    }

    private enum FakeRoomType
    {
        Shop,
        RestSite
    }
}
