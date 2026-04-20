using System.Text.Json;
using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Ui;

public static class EventLogDisplayLogic
{
    public static string BuildTitle(GameEvent gameEvent)
    {
        return EventCatalog.Supported.FirstOrDefault(item => string.Equals(item.Id, gameEvent.Type, StringComparison.OrdinalIgnoreCase))?.ZhHansName
            ?? gameEvent.Type;
    }

    public static string BuildMeta(GameEvent gameEvent)
    {
        var localTime = DateTimeOffset.FromUnixTimeMilliseconds(gameEvent.Timestamp).ToLocalTime();
        var roomText = string.IsNullOrWhiteSpace(gameEvent.RoomType) ? "未知房间" : gameEvent.RoomType!;
        return $"{localTime:HH:mm:ss} | 第 {gameEvent.Floor} 层 | {roomText}";
    }

    public static string BuildSummary(GameEvent gameEvent)
    {
        return gameEvent.Type switch
        {
            EventTypes.PlayerDamaged => BuildPlayerDamagedSummary(gameEvent),
            EventTypes.PlayerHealed => BuildPlayerHealedSummary(gameEvent),
            EventTypes.PlayerBlockChanged => BuildPlayerBlockChangedSummary(gameEvent),
            EventTypes.PlayerEnergyChanged => BuildPlayerEnergyChangedSummary(gameEvent),
            EventTypes.CombatStarted => "战斗开始",
            EventTypes.CombatEnded => "战斗结束",
            EventTypes.RoomEntered => "进入新房间",
            _ => BuildFallbackSummary(gameEvent.Payload)
        };
    }

    private static string BuildPlayerDamagedSummary(GameEvent gameEvent)
    {
        var amount = GetInt(gameEvent.Payload, "amount");
        var currentHp = GetInt(gameEvent.Payload, "currentHp");
        var maxHp = GetInt(gameEvent.Payload, "maxHp");

        if (amount.HasValue && currentHp.HasValue && maxHp.HasValue)
        {
            return $"掉血 {amount.Value}，当前生命 {currentHp.Value}/{maxHp.Value}";
        }

        if (amount.HasValue)
        {
            return $"掉血 {amount.Value}";
        }

        return BuildFallbackSummary(gameEvent.Payload);
    }

    private static string BuildPlayerHealedSummary(GameEvent gameEvent)
    {
        var amount = GetInt(gameEvent.Payload, "amount");
        return amount.HasValue ? $"回血 {amount.Value}" : BuildFallbackSummary(gameEvent.Payload);
    }

    private static string BuildPlayerBlockChangedSummary(GameEvent gameEvent)
    {
        var delta = GetInt(gameEvent.Payload, "delta");
        var currentBlock = GetInt(gameEvent.Payload, "block");
        var reason = GetString(gameEvent.Payload, "reason");

        if (delta.HasValue && currentBlock.HasValue)
        {
            return $"格挡变化 {delta.Value:+#;-#;0}，当前格挡 {currentBlock.Value}" +
                   (string.IsNullOrWhiteSpace(reason) ? string.Empty : $"，原因 {reason}");
        }

        if (delta.HasValue)
        {
            return $"格挡变化 {delta.Value:+#;-#;0}";
        }

        return BuildFallbackSummary(gameEvent.Payload);
    }

    private static string BuildPlayerEnergyChangedSummary(GameEvent gameEvent)
    {
        var delta = GetInt(gameEvent.Payload, "delta");
        var energy = GetInt(gameEvent.Payload, "energy");
        var maxEnergy = GetInt(gameEvent.Payload, "maxEnergy");

        if (delta.HasValue && energy.HasValue && maxEnergy.HasValue)
        {
            return $"能量变化 {delta.Value:+#;-#;0}，当前能量 {energy.Value}/{maxEnergy.Value}";
        }

        if (delta.HasValue)
        {
            return $"能量变化 {delta.Value:+#;-#;0}";
        }

        return BuildFallbackSummary(gameEvent.Payload);
    }

    private static string BuildFallbackSummary(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return json.Length <= 140 ? json : $"{json[..137]}...";
    }

    private static int? GetInt(object payload, string propertyName)
    {
        if (!TryGetProperty(payload, propertyName, out var value))
        {
            return null;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            short shortValue => shortValue,
            byte byteValue => byteValue,
            decimal decimalValue => (int)decimalValue,
            float floatValue => (int)floatValue,
            double doubleValue => (int)doubleValue,
            JsonElement { ValueKind: JsonValueKind.Number } jsonNumber when jsonNumber.TryGetInt32(out var parsed) => parsed,
            _ => null
        };
    }

    private static string? GetString(object payload, string propertyName)
    {
        if (!TryGetProperty(payload, propertyName, out var value))
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString(),
            _ => value?.ToString()
        };
    }

    private static bool TryGetProperty(object payload, string propertyName, out object? value)
    {
        value = null;

        if (payload is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        var propertyInfo = payload.GetType().GetProperties()
            .FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (propertyInfo is null)
        {
            return false;
        }

        value = propertyInfo.GetValue(payload);
        return true;
    }
}
