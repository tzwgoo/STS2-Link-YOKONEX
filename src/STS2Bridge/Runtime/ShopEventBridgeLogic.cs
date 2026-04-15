using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;

namespace STS2Bridge.Runtime;

internal static class ShopEventBridgeLogic
{
    private static readonly string[] PlayerIdMemberNames = ["PlayerId", "playerId"];
    private static readonly string[] PlayerGoldMemberNames = ["Gold", "gold", "CurrentGold", "currentGold"];
    private static readonly string[] ItemIdMemberNames = ["Id", "id", "CardId", "cardId", "ModelId", "modelId"];
    private static readonly string[] ItemNameMemberNames = ["Name", "name", "Title", "title"];
    private static readonly string[] NestedItemMemberNames = ["Model", "model", "CreationResult", "creationResult", "Card", "card"];

    public static bool PublishItemPurchased(GameEventBus eventBus, GameStateStore stateStore, object? player, object? itemPurchased, int goldSpent)
    {
        if (!TryCreatePlayerSnapshot(player, out var playerSnapshot))
        {
            return false;
        }

        if (!TryCreateItemSnapshot(itemPurchased, out var itemSnapshot))
        {
            return false;
        }

        UpdateState(stateStore, playerSnapshot);

        var state = stateStore.GetSnapshot();
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: EventTypes.ItemPurchased,
            RunId: state.RunId,
            Floor: state.Floor,
            RoomType: state.RoomType,
            Payload: new
            {
                playerId = playerSnapshot.PlayerId,
                itemType = itemSnapshot.ItemType,
                itemId = itemSnapshot.ItemId,
                itemName = itemSnapshot.ItemName,
                goldSpent,
                shopType = "merchant",
                playerGoldAfter = playerSnapshot.Gold
            }));

        return true;
    }

    public static object? FindPlayerArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (TryCreatePlayerSnapshot(arg, out _))
            {
                return arg;
            }
        }

        return null;
    }

    public static object? FindPurchasedItemArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (TryCreatePlayerSnapshot(arg, out _))
            {
                continue;
            }

            if (TryCreateItemSnapshot(arg, out _))
            {
                return arg;
            }
        }

        return null;
    }

    public static int FindGoldSpentArgument(object?[]? args)
    {
        if (args is null)
        {
            return 0;
        }

        for (var index = args.Length - 1; index >= 0; index--)
        {
            if (args[index] is int goldSpent)
            {
                return goldSpent;
            }
        }

        return 0;
    }

    private static void UpdateState(GameStateStore stateStore, PlayerSnapshot player)
    {
        var snapshot = stateStore.GetSnapshot();
        stateStore.Update(snapshot with
        {
            Player = snapshot.Player with
            {
                Gold = player.Gold
            }
        });
    }

    private static bool TryCreatePlayerSnapshot(object? player, out PlayerSnapshot snapshot)
    {
        snapshot = default;
        if (player is null)
        {
            return false;
        }

        if (!RuntimeReflectionHelpers.TryGetString(player, PlayerIdMemberNames, out var playerId))
        {
            return false;
        }

        if (!RuntimeReflectionHelpers.TryGetInt(player, PlayerGoldMemberNames, out var gold))
        {
            ModLog.Warn($"Shop event skipped because player gold was missing on '{player.GetType().FullName}'.");
            return false;
        }

        snapshot = new PlayerSnapshot(playerId, gold);
        return true;
    }

    private static bool TryCreateItemSnapshot(object? itemPurchased, out ItemSnapshot snapshot)
    {
        snapshot = default;
        if (itemPurchased is null)
        {
            return false;
        }

        var source = ResolveItemSource(itemPurchased);
        RuntimeReflectionHelpers.TryGetString(source, ItemIdMemberNames, out var itemId);
        RuntimeReflectionHelpers.TryGetString(source, ItemNameMemberNames, out var itemName);
        if (string.IsNullOrWhiteSpace(itemId) && string.IsNullOrWhiteSpace(itemName))
        {
            ModLog.Warn($"Shop event skipped because item details were missing on '{itemPurchased.GetType().FullName}'.");
            return false;
        }

        snapshot = new ItemSnapshot(
            DetectItemType(itemPurchased, source),
            string.IsNullOrWhiteSpace(itemId) ? null : itemId,
            string.IsNullOrWhiteSpace(itemName) ? null : itemName);
        return true;
    }

    private static object ResolveItemSource(object itemPurchased)
    {
        foreach (var memberName in NestedItemMemberNames)
        {
            var nested = RuntimeReflectionHelpers.GetMemberValue(itemPurchased, memberName);
            if (nested is null)
            {
                continue;
            }

            if (RuntimeReflectionHelpers.TryGetString(nested, ItemIdMemberNames, out _) ||
                RuntimeReflectionHelpers.TryGetString(nested, ItemNameMemberNames, out _))
            {
                return nested;
            }
        }

        return itemPurchased;
    }

    private static string DetectItemType(object itemPurchased, object source)
    {
        var typeNames = new[]
        {
            itemPurchased.GetType().FullName ?? itemPurchased.GetType().Name,
            source.GetType().FullName ?? source.GetType().Name
        };

        foreach (var typeName in typeNames)
        {
            var normalized = typeName.ToLowerInvariant();
            if (normalized.Contains("potion", StringComparison.Ordinal))
            {
                return "potion";
            }

            if (normalized.Contains("relic", StringComparison.Ordinal))
            {
                return "relic";
            }

            if (normalized.Contains("card", StringComparison.Ordinal))
            {
                return "card";
            }
        }

        return "other";
    }

    private readonly record struct PlayerSnapshot(string PlayerId, int Gold);

    private readonly record struct ItemSnapshot(string ItemType, string? ItemId, string? ItemName);
}
