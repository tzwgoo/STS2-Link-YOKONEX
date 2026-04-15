using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class ShopEventBridgeLogicTests
{
    [Fact]
    public void PublishItemPurchased_should_emit_item_purchased_for_card_purchase()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-shop-card",
            Floor = 14,
            RoomType = "Shop",
            Player = new PlayerStateDto(60, 70, 3, 0, 125)
        });

        var published = ShopEventBridgeLogic.PublishItemPurchased(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "ironclad",
                Gold = 50
            },
            new FakeMerchantCard
            {
                Id = "inflame",
                Name = "Inflame"
            },
            goldSpent: 75);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.ItemPurchased, gameEvent.Type);
        Assert.Equal("ironclad", GetString(gameEvent.Payload, "playerId"));
        Assert.Equal("card", GetString(gameEvent.Payload, "itemType"));
        Assert.Equal("inflame", GetString(gameEvent.Payload, "itemId"));
        Assert.Equal("Inflame", GetString(gameEvent.Payload, "itemName"));
        Assert.Equal(75, GetInt(gameEvent.Payload, "goldSpent"));
        Assert.Equal("merchant", GetString(gameEvent.Payload, "shopType"));
        Assert.Equal(50, GetInt(gameEvent.Payload, "playerGoldAfter"));
        Assert.Equal(50, stateStore.GetSnapshot().Player.Gold);
    }

    [Fact]
    public void PublishItemPurchased_should_emit_item_purchased_for_potion_purchase()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-shop-potion",
            Floor = 8,
            RoomType = "Shop",
            Player = new PlayerStateDto(40, 60, 3, 0, 90)
        });

        var published = ShopEventBridgeLogic.PublishItemPurchased(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "silent",
                Gold = 55
            },
            new FakeMerchantPotion
            {
                Model = new FakePotionModel
                {
                    Id = "dexterity_potion",
                    Name = "Dexterity Potion"
                }
            },
            goldSpent: 35);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal("potion", GetString(gameEvent.Payload, "itemType"));
        Assert.Equal("dexterity_potion", GetString(gameEvent.Payload, "itemId"));
        Assert.Equal("Dexterity Potion", GetString(gameEvent.Payload, "itemName"));
        Assert.Equal(55, GetInt(gameEvent.Payload, "playerGoldAfter"));
    }

    [Fact]
    public void PublishItemPurchased_should_ignore_unknown_item_shapes()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            Player = new PlayerStateDto(40, 60, 3, 0, 90)
        });

        var published = ShopEventBridgeLogic.PublishItemPurchased(
            eventBus,
            stateStore,
            new FakePlayer
            {
                PlayerId = "silent",
                Gold = 55
            },
            new object(),
            goldSpent: 35);

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
        Assert.Equal(90, stateStore.GetSnapshot().Player.Gold);
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

    private sealed class FakePlayer
    {
        public string? PlayerId { get; init; }

        public int Gold { get; init; }
    }

    private sealed class FakeMerchantCard
    {
        public string? Id { get; init; }

        public string? Name { get; init; }
    }

    private sealed class FakeMerchantPotion
    {
        public FakePotionModel? Model { get; init; }
    }

    private sealed class FakePotionModel
    {
        public string? Id { get; init; }

        public string? Name { get; init; }
    }
}
