using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;

namespace STS2Bridge.Runtime;

internal static class CardEventBridgeLogic
{
    private static readonly string[] CardPlayCardMembers = ["Card", "card"];
    private static readonly string[] CardPlayTargetMembers = ["Target", "target"];
    private static readonly string[] CardIdMembers = ["Id", "CardId", "cardId", "CardModelId"];
    private static readonly string[] CardNameMembers = ["Name", "name"];
    private static readonly string[] CostMembers = ["Cost", "cost", "CurrentCost", "currentCost"];
    private static readonly string[] TargetIdMembers = ["TargetId", "targetId", "Id", "id"];
    private static readonly string[] PlayIndexMembers = ["PlayIndex", "playIndex"];
    private static readonly string[] PlayCountMembers = ["PlayCount", "playCount"];
    private static readonly string[] AutoPlayMembers = ["IsAutoPlay", "isAutoPlay"];

    public static bool PublishCardPlayed(GameEventBus eventBus, GameStateStore stateStore, object? cardPlay)
    {
        if (!TryCreateCardPlaySnapshot(cardPlay, out var snapshot))
        {
            return false;
        }

        var state = stateStore.GetSnapshot();
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: EventTypes.CardPlayed,
            RunId: state.RunId,
            Floor: state.Floor,
            RoomType: state.RoomType,
            Payload: new
            {
                cardId = snapshot.CardId,
                cardName = snapshot.CardName,
                cost = snapshot.Cost,
                targetId = snapshot.TargetId,
                playIndex = snapshot.PlayIndex,
                playCount = snapshot.PlayCount,
                isAutoPlay = snapshot.IsAutoPlay
            }));

        return true;
    }

    public static object? FindCardPlayArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (TryCreateCardPlaySnapshot(arg, out _))
            {
                return arg;
            }
        }

        return null;
    }

    private static bool TryCreateCardPlaySnapshot(object? cardPlay, out CardPlaySnapshot snapshot)
    {
        snapshot = default;
        if (cardPlay is null)
        {
            return false;
        }

        var card = RuntimeReflectionHelpers.GetMemberValue(cardPlay, "Card")
            ?? RuntimeReflectionHelpers.GetMemberValue(cardPlay, "card");
        var target = RuntimeReflectionHelpers.GetMemberValue(cardPlay, "Target")
            ?? RuntimeReflectionHelpers.GetMemberValue(cardPlay, "target");

        if (!RuntimeReflectionHelpers.TryGetString(card ?? cardPlay, CardIdMembers, out var cardId))
        {
            ModLog.Warn($"Card event skipped because card id was missing on '{cardPlay.GetType().FullName}'.");
            return false;
        }

        RuntimeReflectionHelpers.TryGetString(card, CardNameMembers, out var cardName);
        RuntimeReflectionHelpers.TryGetInt(card, CostMembers, out var cost);
        RuntimeReflectionHelpers.TryGetString(target, TargetIdMembers, out var targetId);
        RuntimeReflectionHelpers.TryGetString(cardPlay, TargetIdMembers, out var actionTargetId);
        RuntimeReflectionHelpers.TryGetInt(cardPlay, PlayIndexMembers, out var playIndex);
        RuntimeReflectionHelpers.TryGetInt(cardPlay, PlayCountMembers, out var playCount);
        RuntimeReflectionHelpers.TryGetBool(cardPlay, AutoPlayMembers, out var isAutoPlay);

        snapshot = new CardPlaySnapshot(
            cardId,
            string.IsNullOrWhiteSpace(cardName) ? null : cardName,
            cost,
            string.IsNullOrWhiteSpace(targetId) ? actionTargetId : targetId,
            playIndex,
            playCount,
            isAutoPlay);
        return true;
    }

    private readonly record struct CardPlaySnapshot(
        string CardId,
        string? CardName,
        int Cost,
        string? TargetId,
        int PlayIndex,
        int PlayCount,
        bool IsAutoPlay);
}
