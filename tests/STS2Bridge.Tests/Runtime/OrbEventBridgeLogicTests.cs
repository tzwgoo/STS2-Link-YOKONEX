using STS2Bridge.Events;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Tests.Runtime;

public sealed class OrbEventBridgeLogicTests
{
    [Fact]
    public void PublishPassiveTriggered_should_emit_lightning_passive_event()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = CreateStateStore();
        var orb = new FakeOrb
        {
            Owner = new FakeOrbOwner
            {
                NetId = 7UL
            },
            PassiveVal = 3,
            EvokeVal = 8
        };

        var published = OrbEventBridgeLogic.PublishPassiveTriggered(eventBus, stateStore, orb, "lightning", "damage");

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.OrbPassiveTriggered, gameEvent.Type);
        Assert.Equal("lightning", GetString(gameEvent.Payload, "orbType"));
        Assert.Equal("damage", GetString(gameEvent.Payload, "amountKind"));
        Assert.Equal(3, GetInt(gameEvent.Payload, "amount"));
        Assert.Equal("7", GetString(gameEvent.Payload, "ownerId"));
        Assert.Equal("lightning.passive", GetString(gameEvent.Payload, "displayName"));
    }

    [Theory]
    [InlineData("frost", "block", 2)]
    [InlineData("dark", "damage", 6)]
    [InlineData("plasma", "energy", 1)]
    public void PublishPassiveTriggered_should_support_other_orb_types(string orbType, string amountKind, int passiveAmount)
    {
        var eventBus = new GameEventBus(20);
        var stateStore = CreateStateStore();
        var orb = new FakeOrb
        {
            PassiveVal = passiveAmount,
            Owner = new FakeOrbOwner
            {
                NetId = 11UL
            }
        };

        var published = OrbEventBridgeLogic.PublishPassiveTriggered(eventBus, stateStore, orb, orbType, amountKind);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.OrbPassiveTriggered, gameEvent.Type);
        Assert.Equal(orbType, GetString(gameEvent.Payload, "orbType"));
        Assert.Equal(amountKind, GetString(gameEvent.Payload, "amountKind"));
        Assert.Equal(passiveAmount, GetInt(gameEvent.Payload, "amount"));
    }

    [Theory]
    [InlineData("lightning", "damage", 8)]
    [InlineData("frost", "block", 5)]
    [InlineData("dark", "damage", 22)]
    [InlineData("plasma", "energy", 2)]
    public void PublishEvoked_should_emit_evoked_event_for_supported_orbs(string orbType, string amountKind, int evokeAmount)
    {
        var eventBus = new GameEventBus(20);
        var stateStore = CreateStateStore();
        var orb = new FakeOrb
        {
            PassiveVal = 1,
            EvokeVal = evokeAmount,
            Owner = new FakeOrbOwner
            {
                NetId = 99UL
            }
        };

        var published = OrbEventBridgeLogic.PublishEvoked(eventBus, stateStore, orb, orbType, amountKind);

        Assert.True(published);

        var gameEvent = Assert.Single(eventBus.GetRecentEvents(10));
        Assert.Equal(EventTypes.OrbEvoked, gameEvent.Type);
        Assert.Equal(orbType, GetString(gameEvent.Payload, "orbType"));
        Assert.Equal(amountKind, GetString(gameEvent.Payload, "amountKind"));
        Assert.Equal(evokeAmount, GetInt(gameEvent.Payload, "amount"));
        Assert.Equal("99", GetString(gameEvent.Payload, "ownerId"));
        Assert.Equal($"{orbType}.evoked", GetString(gameEvent.Payload, "displayName"));
    }

    [Fact]
    public void PublishEvoked_should_skip_when_amount_is_unavailable()
    {
        var eventBus = new GameEventBus(20);
        var stateStore = CreateStateStore();

        var published = OrbEventBridgeLogic.PublishEvoked(
            eventBus,
            stateStore,
            new FakeOrbWithoutValues(),
            "plasma",
            "energy");

        Assert.False(published);
        Assert.Empty(eventBus.GetRecentEvents(10));
    }

    private static GameStateStore CreateStateStore()
    {
        var stateStore = new GameStateStore();
        stateStore.Update(StateSnapshotDto.Empty with
        {
            RunId = "run-orbs",
            Floor = 18,
            RoomType = "Combat",
            Player = new PlayerStateDto(55, 70, 3, 0, 99)
        });

        return stateStore;
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

    private sealed class FakeOrb
    {
        public FakeOrbOwner? Owner { get; init; }

        public int PassiveVal { get; init; }

        public int EvokeVal { get; init; }
    }

    private sealed class FakeOrbOwner
    {
        public ulong NetId { get; init; }
    }

    private sealed class FakeOrbWithoutValues
    {
        public FakeOrbOwner? Owner { get; init; } = new()
        {
            NetId = 1UL
        };
    }
}
