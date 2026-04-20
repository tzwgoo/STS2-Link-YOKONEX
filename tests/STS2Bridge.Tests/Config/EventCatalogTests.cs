using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Tests.Config;

public sealed class EventCatalogTests
{
    [Fact]
    public void Supported_should_include_currently_wired_events()
    {
        var ids = EventCatalog.Supported.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(EventTypes.CombatStarted, ids);
        Assert.Contains(EventTypes.TurnStarted, ids);
        Assert.Contains(EventTypes.CombatEnded, ids);
        Assert.Contains(EventTypes.PlayerHpChanged, ids);
        Assert.Contains(EventTypes.PlayerDamaged, ids);
        Assert.Contains(EventTypes.PlayerHealed, ids);
        Assert.Contains(EventTypes.PlayerEnergyChanged, ids);
        Assert.Contains(EventTypes.PlayerBlockChanged, ids);
        Assert.Contains(EventTypes.PlayerBlockBroken, ids);
        Assert.Contains(EventTypes.PlayerBlockCleared, ids);
        Assert.Contains(EventTypes.PlayerDied, ids);
        Assert.Contains(EventTypes.LightningOrbPassiveTriggered, ids);
        Assert.Contains(EventTypes.LightningOrbEvoked, ids);
        Assert.Contains(EventTypes.FrostOrbPassiveTriggered, ids);
        Assert.Contains(EventTypes.FrostOrbEvoked, ids);
        Assert.Contains(EventTypes.DarkOrbPassiveTriggered, ids);
        Assert.Contains(EventTypes.DarkOrbEvoked, ids);
        Assert.Contains(EventTypes.PlasmaOrbPassiveTriggered, ids);
        Assert.Contains(EventTypes.PlasmaOrbEvoked, ids);
    }

    [Fact]
    public void Default_command_map_should_include_documented_im_bridge_events()
    {
        Assert.Equal("combat_start", EventCommandCatalog.DefaultMap[EventTypes.CombatStarted]);
        Assert.Equal("combat_end", EventCommandCatalog.DefaultMap[EventTypes.CombatEnded]);
        Assert.Equal("turn_start", EventCommandCatalog.DefaultMap[EventTypes.TurnStarted]);
        Assert.Equal("player_hurt", EventCommandCatalog.DefaultMap[EventTypes.PlayerDamaged]);
        Assert.Equal("player_energy_changed", EventCommandCatalog.DefaultMap[EventTypes.PlayerEnergyChanged]);
        Assert.Equal("player_dead", EventCommandCatalog.DefaultMap[EventTypes.PlayerDied]);
        Assert.Equal("orb_lightning_passive_triggered", EventCommandCatalog.DefaultMap[EventTypes.LightningOrbPassiveTriggered]);
        Assert.Equal("orb_frost_evoked", EventCommandCatalog.DefaultMap[EventTypes.FrostOrbEvoked]);
        Assert.Equal("orb_dark_passive_triggered", EventCommandCatalog.DefaultMap[EventTypes.DarkOrbPassiveTriggered]);
        Assert.Equal("orb_plasma_evoked", EventCommandCatalog.DefaultMap[EventTypes.PlasmaOrbEvoked]);
        Assert.Equal("reward_selected", EventCommandCatalog.DefaultMap[EventTypes.RewardSelected]);
        Assert.Equal("room_entered", EventCommandCatalog.DefaultMap[EventTypes.RoomEntered]);
    }
}
