using System.Text.Json;

namespace STS2Bridge.Config;

public sealed class BridgeSettingsStore(string filePath)
{
    private const string LegacyDefaultImWebSocketUrl = "ws://103.236.55.92:3001";
    private const string CurrentDefaultImWebSocketUrl = "ws://103.236.55.92:43001";

    public string FilePath { get; } = filePath;

    public BridgeSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return BridgeSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<BridgeSettings>(json);
            return MergeWithDefaults(settings);
        }
        catch
        {
            return BridgeSettings.CreateDefault();
        }
    }

    public void Save(BridgeSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var payload = JsonSerializer.Serialize(MergeWithDefaults(settings), new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(FilePath, payload);
    }

    public static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "SlayTheSpire2", "mods", "STS2-Link-YOKONEX", "bridge-settings.json");
    }

    private static BridgeSettings MergeWithDefaults(BridgeSettings? settings)
    {
        var merged = BridgeSettings.CreateDefault();
        if (settings is null)
        {
            return merged;
        }

        foreach (var pair in settings.EventToggles)
        {
            if (EventCatalog.SupportedIds.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                merged = merged.SetEventEnabled(pair.Key, pair.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.ImWebSocketUrl))
        {
            merged = merged with
            {
                ImWebSocketUrl = NormalizeImWebSocketUrl(settings.ImWebSocketUrl)
            };
        }

        merged = merged.SetImCredentials(settings.ImUid, settings.ImToken);
        merged = merged.SetImAutoLogin(settings.ImAutoLogin);

        foreach (var pair in settings.EventCommandMap)
        {
            if (EventCatalog.SupportedIds.Contains(pair.Key, StringComparer.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(pair.Value))
            {
                merged = merged.SetCommandMapping(pair.Key, pair.Value);
            }
        }

        foreach (var rule in settings.CommandTriggerRules)
        {
            if (!rule.Enabled ||
                string.IsNullOrWhiteSpace(rule.EventType) ||
                string.IsNullOrWhiteSpace(rule.CommandId) ||
                rule.Threshold <= 0 ||
                rule.RepeatCount <= 0)
            {
                continue;
            }

            merged = merged.AddTriggerRule(rule);
        }

        return merged;
    }

    private static string NormalizeImWebSocketUrl(string value)
    {
        return string.Equals(value, LegacyDefaultImWebSocketUrl, StringComparison.OrdinalIgnoreCase)
            ? CurrentDefaultImWebSocketUrl
            : value;
    }
}
