using STS2Bridge.Events;

namespace STS2Bridge.Config;

public static class EventCommandCatalog
{
    public static IReadOnlyDictionary<string, string> DefaultMap { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EventTypes.PlayerHpChanged] = "player_hp_changed",
            [EventTypes.PlayerDamaged] = "player_hurt",
            [EventTypes.PlayerHealed] = "player_heal",
            [EventTypes.PlayerEnergyChanged] = "player_energy_changed",
            [EventTypes.PlayerBlockChanged] = "player_block_changed",
            [EventTypes.PlayerBlockBroken] = "player_block_break",
            [EventTypes.PlayerBlockCleared] = "player_block_clear",
            [EventTypes.PlayerDied] = "player_dead",
            [EventTypes.OrbPassiveTriggered] = "orb_passive_triggered",
            [EventTypes.OrbEvoked] = "orb_evoked",
            [EventTypes.CombatStarted] = "combat_start",
            [EventTypes.CombatEnded] = "combat_end",
            [EventTypes.TurnStarted] = "turn_start",
            [EventTypes.CardUpgraded] = "card_upgraded",
            [EventTypes.ItemPurchased] = "item_purchased",
            [EventTypes.RewardSelected] = "reward_selected",
            [EventTypes.RoomEntered] = "room_entered"
        };
}
