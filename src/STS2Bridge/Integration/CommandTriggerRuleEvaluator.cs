using System.Text.Json;
using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Integration;

internal static class CommandTriggerRuleEvaluator
{
    public static int GetRepeatCount(GameEvent gameEvent, CommandTriggerRule rule)
    {
        if (!rule.Enabled ||
            !string.Equals(gameEvent.Type, rule.EventType, StringComparison.OrdinalIgnoreCase) ||
            rule.Threshold <= 0 ||
            rule.RepeatCount <= 0)
        {
            return 0;
        }

        var value = TryGetSingleEventValue(gameEvent);
        return value >= rule.Threshold ? rule.RepeatCount : 0;
    }

    private static int TryGetSingleEventValue(GameEvent gameEvent)
    {
        var element = JsonSerializer.SerializeToElement(gameEvent.Payload);

        return gameEvent.Type switch
        {
            EventTypes.PlayerDamaged => TryGetInt(element, "amount"),
            EventTypes.PlayerBlockBroken => GetBlockBrokenValue(element),
            _ => 0
        };
    }

    private static int GetBlockBrokenValue(JsonElement element)
    {
        var previousBlock = TryGetNullableInt(element, "previousBlock");
        var block = TryGetNullableInt(element, "block");
        return previousBlock.HasValue && block.HasValue && previousBlock.Value > block.Value
            ? previousBlock.Value - block.Value
            : 0;
    }

    private static int TryGetInt(JsonElement element, string propertyName)
    {
        return TryGetNullableInt(element, propertyName) ?? 0;
    }

    private static int? TryGetNullableInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var value) => value,
            _ => null
        };
    }
}
