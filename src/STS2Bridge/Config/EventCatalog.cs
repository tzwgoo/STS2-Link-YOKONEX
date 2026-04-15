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
        new(EventTypes.CardPlayed, "打牌事件", "Card Played"),
        new(EventTypes.PlayerHpChanged, "玩家生命变化", "Player HP Changed"),
        new(EventTypes.PlayerDamaged, "玩家受伤", "Player Damaged"),
        new(EventTypes.PlayerHealed, "玩家回血", "Player Healed"),
        new(EventTypes.PlayerEnergyChanged, "玩家能量变化", "Player Energy Changed"),
        new(EventTypes.PlayerBlockChanged, "玩家格挡变化", "Player Block Changed"),
        new(EventTypes.PlayerBlockBroken, "玩家破甲", "Player Block Broken"),
        new(EventTypes.PlayerBlockCleared, "玩家清空格挡", "Player Block Cleared"),
        new(EventTypes.PlayerDied, "玩家死亡", "Player Died"),
        new(EventTypes.EnemyHpChanged, "敌人生命变化", "Enemy HP Changed"),
        new(EventTypes.EnemyDamaged, "敌人受伤", "Enemy Damaged"),
        new(EventTypes.CardUpgraded, "卡牌升级", "Card Upgraded"),
        new(EventTypes.ItemPurchased, "购买道具", "Item Purchased"),
        new(EventTypes.RewardSelected, "选择奖励", "Reward Selected")
    ];

    public static IReadOnlyCollection<string> SupportedIds { get; } =
        Supported.Select(item => item.Id).ToArray();
}

public sealed record EventDefinition(string Id, string ZhHansName, string EnName);
