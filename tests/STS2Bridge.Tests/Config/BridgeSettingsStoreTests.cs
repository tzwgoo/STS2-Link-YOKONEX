using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Tests.Config;

public sealed class BridgeSettingsStoreTests
{
    [Fact]
    public void Load_should_return_defaults_when_file_is_missing()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(tempDir.FullName, "bridge-settings.json");
            var store = new BridgeSettingsStore(path);

            var settings = store.Load();

            Assert.True(settings.IsEventEnabled(EventTypes.CombatStarted));
            Assert.True(settings.IsEventEnabled(EventTypes.PlayerDied));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Save_and_load_should_roundtrip_event_toggles()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(tempDir.FullName, "bridge-settings.json");
            var store = new BridgeSettingsStore(path);
            var settings = BridgeSettings.CreateDefault().SetEventEnabled(EventTypes.CardPlayed, false);

            store.Save(settings);
            var loaded = store.Load();

            Assert.False(loaded.IsEventEnabled(EventTypes.CardPlayed));
            Assert.True(loaded.IsEventEnabled(EventTypes.CombatStarted));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
