using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2Bridge.Actions;
using STS2Bridge.Api;
using STS2Bridge.Compatibility;
using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.Runtime;
using STS2Bridge.State;
using STS2Bridge.Threading;
using STS2Bridge.Integration;
using System.Text;

namespace STS2Bridge;

[ModInitializer("Initialize")]
public static class ModEntry
{
    private static RuntimeApiHost? _apiHost;
    private static IDisposable? _combatManagerBridgeSubscription;
    private static IMCommandBridgeService? _imCommandBridgeService;
    private static IExternalImClient? _externalImClient;
    private static BridgeSettingsStore? _settingsStore;
    private static string? _debugLogPath;

    public static BridgeConfig Config { get; private set; } = BridgeConfig.CreateDefault();

    public static GameEventBus EventBus { get; private set; } = new();

    public static EventToggleService EventToggles { get; private set; } = new(BridgeSettings.CreateDefault());

    public static GameStateStore StateStore { get; private set; } = new();

    public static MainThreadDispatcher Dispatcher { get; private set; } = new();

    public static ActionRouter ActionRouter { get; private set; } = ActionRouter.CreateDefault(Config, Dispatcher, StateStore, EventBus);

    public static ExternalImStatus ImStatus => _externalImClient?.Status ?? new();

    public static void Initialize()
    {
        ModLog.Info("STS2-Link-YOKONEX initializing...");

        try
        {
            Config = BridgeConfig.CreateDefault();
            _debugLogPath = Path.Combine(
                Path.GetDirectoryName(BridgeSettingsStore.GetDefaultPath()) ?? string.Empty,
                "bridge-debug.log");
            ModLog.SetSink(Config.EnableDebugLog ? WriteDebugLine : null);
            _settingsStore = new BridgeSettingsStore(BridgeSettingsStore.GetDefaultPath());
            EventToggles = new EventToggleService(_settingsStore.Load());
            EventBus = new GameEventBus(200, Config.EventWhitelist, EventToggles.IsEventEnabled);
            StateStore = new GameStateStore();
            Dispatcher = new MainThreadDispatcher();
            ActionRouter = ActionRouter.CreateDefault(Config, Dispatcher, StateStore, EventBus);
            _externalImClient = new ExternalImWebSocketClient();
            _imCommandBridgeService?.Dispose();
            _imCommandBridgeService = new IMCommandBridgeService(EventBus, _externalImClient, () => EventToggles.GetSettings());

            var harmonyResult = HookGuard.Run("patch-all", () =>
            {
                var harmony = new Harmony("com.hosgoo.sts2-link-yokonex");
                harmony.PatchAll();
            });

            var combatManagerHookResult = HookGuard.Run("combat-manager-bridge", () =>
            {
                _combatManagerBridgeSubscription?.Dispose();
                _combatManagerBridgeSubscription = CombatManagerEventBridge.CreateDefault(
                    EventBus,
                    runIdProvider: () => StateStore.GetSnapshot().RunId,
                    floorProvider: () => StateStore.GetSnapshot().Floor,
                    roomTypeProvider: () => StateStore.GetSnapshot().RoomType).Install();
            });

            ModLog.Info($"Hook install result: success={harmonyResult.Success}, message={harmonyResult.Message}");
            ModLog.Info($"CombatManager bridge result: success={combatManagerHookResult.Success}, message={combatManagerHookResult.Message}");
            ModLog.Info($"Detected game version: {GameVersionDetector.Detect()}");

            var settings = EventToggles.GetSettings();
            if (settings.ImAutoLogin && !string.IsNullOrWhiteSpace(settings.ImUid) && !string.IsNullOrWhiteSpace(settings.ImToken))
            {
                _ = LoginImAsync();
            }

            ModLog.Info("STS2-Link-YOKONEX initialized.");
        }
        catch (Exception ex)
        {
            ModLog.Error("STS2-Link-YOKONEX initialization failed.", ex);
        }
    }

    public static async Task StartApiAsync(CancellationToken cancellationToken = default)
    {
        _apiHost = await LocalApiServer.StartRuntimeAsync(Config, EventBus, StateStore, ActionRouter, cancellationToken);
    }

    public static void DrainMainThreadQueue()
    {
        Dispatcher.Drain();
    }

    public static void SaveSettings()
    {
        _settingsStore?.Save(EventToggles.GetSettings());
    }

    public static void UpdateSettings(BridgeSettings settings)
    {
        EventToggles.UpdateSettings(_ => settings);
    }

    public static async Task LoginImAsync(CancellationToken cancellationToken = default)
    {
        if (_externalImClient is null)
        {
            return;
        }

        var settings = EventToggles.GetSettings();
        if (string.IsNullOrWhiteSpace(settings.ImUid) || string.IsNullOrWhiteSpace(settings.ImToken))
        {
            return;
        }

        await _externalImClient.ConnectAsync(settings.ImWebSocketUrl, cancellationToken);
        await _externalImClient.LoginAsync(settings.ImUid, settings.ImToken, cancellationToken);
    }

    public static async Task LogoutImAsync(CancellationToken cancellationToken = default)
    {
        if (_externalImClient is null)
        {
            return;
        }

        await _externalImClient.LogoutAsync(cancellationToken);
    }

    private static void WriteDebugLine(string line)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_debugLogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_debugLogPath)!);
                File.AppendAllText(_debugLogPath, line + System.Environment.NewLine, new UTF8Encoding(false));
            }
        }
        catch
        {
        }

        try
        {
            GD.Print(line);
        }
        catch
        {
        }
    }
}
