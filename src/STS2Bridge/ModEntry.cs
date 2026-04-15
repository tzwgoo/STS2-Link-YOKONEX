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

namespace STS2Bridge;

[ModInitializer("Initialize")]
public static class ModEntry
{
    private static RuntimeApiHost? _apiHost;
    private static IDisposable? _combatManagerBridgeSubscription;
    private static BridgeSettingsStore? _settingsStore;

    public static BridgeConfig Config { get; private set; } = BridgeConfig.CreateDefault();

    public static GameEventBus EventBus { get; private set; } = new();

    public static EventToggleService EventToggles { get; private set; } = new(BridgeSettings.CreateDefault());

    public static GameStateStore StateStore { get; private set; } = new();

    public static MainThreadDispatcher Dispatcher { get; private set; } = new();

    public static ActionRouter ActionRouter { get; private set; } = ActionRouter.CreateDefault(Config, Dispatcher, StateStore, EventBus);

    public static void Initialize()
    {
        ModLog.Info("STS2Bridge initializing...");

        try
        {
            Config = BridgeConfig.CreateDefault();
            _settingsStore = new BridgeSettingsStore(BridgeSettingsStore.GetDefaultPath());
            EventToggles = new EventToggleService(_settingsStore.Load());
            EventBus = new GameEventBus(200, Config.EventWhitelist, EventToggles.IsEventEnabled);
            StateStore = new GameStateStore();
            Dispatcher = new MainThreadDispatcher();
            ActionRouter = ActionRouter.CreateDefault(Config, Dispatcher, StateStore, EventBus);

            var harmonyResult = HookGuard.Run("patch-all", () =>
            {
                var harmony = new Harmony("com.sts2.bridge");
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
            ModLog.Info("STS2Bridge initialized.");
        }
        catch (Exception ex)
        {
            ModLog.Error("STS2Bridge initialization failed.", ex);
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
}
