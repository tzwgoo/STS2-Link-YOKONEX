using System.Text.Json;

namespace STS2Bridge.Config;

public sealed class BridgeSettingsStore(string filePath)
{
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
        return Path.Combine(appData, "SlayTheSpire2", "mods", "STS2Bridge", "bridge-settings.json");
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

        return merged;
    }
}
