namespace STS2Bridge.Config;

public sealed record BridgeSettings
{
    public Dictionary<string, bool> EventToggles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string ImWebSocketUrl { get; init; } = "ws://103.236.55.92:43001";
    public string ImUid { get; init; } = string.Empty;
    public string ImToken { get; init; } = string.Empty;
    public bool ImAutoLogin { get; init; }
    public Dictionary<string, string> EventCommandMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CommandTriggerRule> CommandTriggerRules { get; init; } = [];

    public bool IsEventEnabled(string eventType)
    {
        return EventToggles.TryGetValue(eventType, out var enabled) ? enabled : true;
    }

    public BridgeSettings SetEventEnabled(string eventType, bool enabled)
    {
        var next = new Dictionary<string, bool>(EventToggles, StringComparer.OrdinalIgnoreCase)
        {
            [eventType] = enabled
        };

        return this with
        {
            EventToggles = next
        };
    }

    public BridgeSettings SetImCredentials(string uid, string token)
    {
        return this with
        {
            ImUid = uid,
            ImToken = token
        };
    }

    public BridgeSettings SetImAutoLogin(bool enabled)
    {
        return this with
        {
            ImAutoLogin = enabled
        };
    }

    public BridgeSettings SetCommandMapping(string eventType, string commandId)
    {
        var next = new Dictionary<string, string>(EventCommandMap, StringComparer.OrdinalIgnoreCase)
        {
            [eventType] = commandId
        };

        return this with
        {
            EventCommandMap = next
        };
    }

    public string? GetCommandId(string eventType)
    {
        return EventCommandMap.TryGetValue(eventType, out var commandId) ? commandId : null;
    }

    public BridgeSettings AddTriggerRule(CommandTriggerRule rule)
    {
        var next = CommandTriggerRules
            .Where(item => !string.Equals(item.EventType, rule.EventType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        next.Add(rule);

        return this with
        {
            CommandTriggerRules = next
        };
    }

    public CommandTriggerRule? GetTriggerRule(string eventType)
    {
        return CommandTriggerRules.FirstOrDefault(item => string.Equals(item.EventType, eventType, StringComparison.OrdinalIgnoreCase));
    }

    public static BridgeSettings CreateDefault()
    {
        var settings = new BridgeSettings();
        foreach (var eventType in EventCatalog.SupportedIds)
        {
            settings = settings.SetEventEnabled(eventType, true);
        }

        foreach (var pair in EventCommandCatalog.DefaultMap)
        {
            settings = settings.SetCommandMapping(pair.Key, pair.Value);
        }

        return settings;
    }
}
