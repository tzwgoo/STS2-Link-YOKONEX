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

            Assert.True(settings.IsEventEnabled(EventTypes.PlayerDamaged));
            Assert.True(settings.IsEventEnabled(EventTypes.PlayerDied));
            Assert.Equal("ws://103.236.55.92:43001", settings.ImWebSocketUrl);
            Assert.Equal(string.Empty, settings.ImUid);
            Assert.Equal(string.Empty, settings.ImToken);
            Assert.False(settings.ImAutoLogin);
            Assert.Equal("player_hurt", settings.GetCommandId(EventTypes.PlayerDamaged));
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
            var settings = BridgeSettings.CreateDefault()
                .SetEventEnabled(EventTypes.PlayerEnergyChanged, false)
                .SetImCredentials("987654", "token-123")
                .SetImAutoLogin(true)
                .SetCommandMapping(EventTypes.PlayerEnergyChanged, "player_energy_changed_custom");

            store.Save(settings);
            var loaded = store.Load();

            Assert.False(loaded.IsEventEnabled(EventTypes.PlayerEnergyChanged));
            Assert.True(loaded.IsEventEnabled(EventTypes.PlayerDamaged));
            Assert.Equal("987654", loaded.ImUid);
            Assert.Equal("token-123", loaded.ImToken);
            Assert.True(loaded.ImAutoLogin);
            Assert.Equal("player_energy_changed_custom", loaded.GetCommandId(EventTypes.PlayerEnergyChanged));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Save_and_load_should_roundtrip_trigger_rules()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(tempDir.FullName, "bridge-settings.json");
            var store = new BridgeSettingsStore(path);
            var settings = BridgeSettings.CreateDefault().AddTriggerRule(new CommandTriggerRule(
                Enabled: true,
                EventType: EventTypes.PlayerDamaged,
                Threshold: 5,
                RepeatCount: 2,
                CommandId: "player_hurt"));

            store.Save(settings);
            var loaded = store.Load();

            var rule = Assert.Single(loaded.CommandTriggerRules);
            Assert.Equal(EventTypes.PlayerDamaged, rule.EventType);
            Assert.Equal(5, rule.Threshold);
            Assert.Equal(2, rule.RepeatCount);
            Assert.Equal("player_hurt", rule.CommandId);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_should_migrate_legacy_default_im_websocket_url()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(tempDir.FullName, "bridge-settings.json");
            File.WriteAllText(path, """
            {
              "ImWebSocketUrl": "ws://103.236.55.92:3001"
            }
            """);

            var store = new BridgeSettingsStore(path);
            var settings = store.Load();

            Assert.Equal("ws://103.236.55.92:43001", settings.ImWebSocketUrl);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
