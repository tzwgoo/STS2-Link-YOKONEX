using STS2Bridge.Events;
using STS2Bridge.Logging;

namespace STS2Bridge.Runtime;

public sealed class CombatManagerEventBridge(
    Type combatManagerType,
    Func<object?> combatManagerResolver,
    GameEventBus eventBus,
    Func<string> runIdProvider,
    Func<int> floorProvider,
    Func<string?> roomTypeProvider)
{
    private static readonly (string EventName, string GameEventType)[] Bindings =
    [
        ("CombatSetUp", EventTypes.CombatStarted),
        ("TurnStarted", EventTypes.TurnStarted),
        ("CombatEnded", EventTypes.CombatEnded)
    ];

    public IDisposable Install()
    {
        var combatManager = combatManagerResolver();
        if (combatManager is null)
        {
            ModLog.Warn($"CombatManager bridge skipped because '{combatManagerType.FullName}' instance is null.");
            return EmptyDisposable.Instance;
        }

        var subscriptions = new List<IDisposable>();
        foreach (var (eventName, gameEventType) in Bindings)
        {
            subscriptions.Add(ReflectionEventBinder.Bind(combatManager, eventName, () => Publish(gameEventType)));
        }

        ModLog.Info("CombatManager bridge installed.");
        return new CompositeDisposable(subscriptions);
    }

    private void Publish(string gameEventType)
    {
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: gameEventType,
            RunId: runIdProvider(),
            Floor: floorProvider(),
            RoomType: roomTypeProvider(),
            Payload: new { source = "combat_manager" }));
    }

    public static CombatManagerEventBridge CreateDefault(GameEventBus eventBus, Func<string> runIdProvider, Func<int> floorProvider, Func<string?> roomTypeProvider)
    {
        var combatManagerType = Type.GetType("MegaCrit.Sts2.Core.Combat.CombatManager, sts2")
            ?? throw new InvalidOperationException("Could not locate MegaCrit.Sts2.Core.Combat.CombatManager.");

        return new CombatManagerEventBridge(
            combatManagerType,
            () => combatManagerType.GetProperty("Instance")?.GetValue(null),
            eventBus,
            runIdProvider,
            floorProvider,
            roomTypeProvider);
    }

    private sealed class CompositeDisposable(IReadOnlyList<IDisposable> subscriptions) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
