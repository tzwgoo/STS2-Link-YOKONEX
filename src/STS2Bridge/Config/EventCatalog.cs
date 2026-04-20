using STS2Bridge.Events;

namespace STS2Bridge.Config;

public static class EventCatalog
{
    public static IReadOnlyList<EventDefinition> Supported { get; } =
    [
        new(EventTypes.RoomEntered, "进入房间", "Room Entered"),
        new(EventTypes.CombatStarted, "战斗开始", "Combat Started"),
        new(EventTypes.TurnStarted, "回合开始", "Turn Started"),
        new(EventTypes.CombatEnded, "战斗结束", "Combat Ended"),
        new(EventTypes.PlayerHpChanged, "玩家生命变化", "Player HP Changed"),
        new(EventTypes.PlayerDamaged, "玩家受伤", "Player Damaged"),
        new(EventTypes.PlayerHealed, "玩家回血", "Player Healed"),
        new(EventTypes.PlayerEnergyChanged, "玩家能量变化", "Player Energy Changed"),
        new(EventTypes.PlayerBlockChanged, "玩家格挡变化", "Player Block Changed"),
        new(EventTypes.PlayerBlockBroken, "玩家破甲", "Player Block Broken"),
        new(EventTypes.PlayerBlockCleared, "玩家格挡清空", "Player Block Cleared"),
        new(EventTypes.PlayerDied, "玩家死亡", "Player Died"),
        new(EventTypes.LightningOrbPassiveTriggered, "闪电球被动触发", "Lightning Orb Passive Triggered"),
        new(EventTypes.LightningOrbEvoked, "闪电球激发", "Lightning Orb Evoked"),
        new(EventTypes.FrostOrbPassiveTriggered, "冰霜球被动触发", "Frost Orb Passive Triggered"),
        new(EventTypes.FrostOrbEvoked, "冰霜球激发", "Frost Orb Evoked"),
        new(EventTypes.DarkOrbPassiveTriggered, "黑暗球被动触发", "Dark Orb Passive Triggered"),
        new(EventTypes.DarkOrbEvoked, "黑暗球激发", "Dark Orb Evoked"),
        new(EventTypes.PlasmaOrbPassiveTriggered, "等离子球被动触发", "Plasma Orb Passive Triggered"),
        new(EventTypes.PlasmaOrbEvoked, "等离子球激发", "Plasma Orb Evoked"),
        new(EventTypes.CardUpgraded, "卡牌升级", "Card Upgraded"),
        new(EventTypes.ItemPurchased, "购买道具", "Item Purchased"),
        new(EventTypes.RewardSelected, "选择奖励", "Reward Selected")
    ];

    public static IReadOnlyCollection<string> SupportedIds { get; } =
        Supported.Select(item => item.Id).ToArray();
}

public sealed record EventDefinition(string Id, string ZhHansName, string EnName);
