namespace STS2Bridge.Config;

public sealed record BridgeSettings
{
    public Dictionary<string, bool> EventToggles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

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

    public static BridgeSettings CreateDefault()
    {
        var settings = new BridgeSettings();
        foreach (var eventType in EventCatalog.SupportedIds)
        {
            settings = settings.SetEventEnabled(eventType, true);
        }

        return settings;
    }
}
