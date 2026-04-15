using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;

namespace STS2Bridge.Runtime;

internal static class PlayerEventBridgeLogic
{
    private static readonly string[] PlayerIdMemberNames = ["PlayerId", "playerId"];
    private static readonly string[] CurrentHpMemberNames = ["CurrentHp", "currentHp"];
    private static readonly string[] MaxHpMemberNames = ["MaxHp", "maxHp"];
    private static readonly string[] BlockMemberNames = ["Block", "block"];
    private static readonly string[] NonPlayerMarkers = ["MonsterId", "monsterId", "EnemyId", "enemyId"];

    public static bool PublishHpChanged(GameEventBus eventBus, GameStateStore stateStore, object? creature, int delta)
    {
        if (!TryCreatePlayerSnapshot(creature, out var player))
        {
            return false;
        }

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerHpChanged,
            player,
            new
            {
                playerId = player.PlayerId,
                delta,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp,
                block = player.Block
            });

        if (delta < 0)
        {
            Publish(
                eventBus,
                stateStore,
                EventTypes.PlayerDamaged,
                player,
                new
                {
                    playerId = player.PlayerId,
                    amount = Math.Abs(delta),
                    currentHp = player.CurrentHp,
                    maxHp = player.MaxHp,
                    block = player.Block
                },
                updateState: false);
        }
        else if (delta > 0)
        {
            Publish(
                eventBus,
                stateStore,
                EventTypes.PlayerHealed,
                player,
                new
                {
                    playerId = player.PlayerId,
                    amount = delta,
                    currentHp = player.CurrentHp,
                    maxHp = player.MaxHp,
                    block = player.Block
                },
                updateState: false);
        }

        return true;
    }

    public static bool PublishBlockChanged(GameEventBus eventBus, GameStateStore stateStore, object? creature, int? delta, string reason)
    {
        if (!TryCreatePlayerSnapshot(creature, out var player))
        {
            return false;
        }

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerBlockChanged,
            player,
            new
            {
                playerId = player.PlayerId,
                delta,
                block = player.Block,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp,
                reason
            });

        return true;
    }

    public static bool PublishBlockCleared(GameEventBus eventBus, GameStateStore stateStore, object? creature)
    {
        if (!TryCreatePlayerSnapshot(creature, out var player))
        {
            return false;
        }

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerBlockChanged,
            player,
            new
            {
                playerId = player.PlayerId,
                delta = (int?)null,
                block = player.Block,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp,
                reason = "cleared"
            });

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerBlockCleared,
            player,
            new
            {
                playerId = player.PlayerId,
                block = player.Block,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp
            },
            updateState: false);

        return true;
    }

    public static bool PublishBlockLossFromTransition(GameEventBus eventBus, GameStateStore stateStore, object? creature, int previousBlock, string reason)
    {
        if (!TryCreatePlayerSnapshot(creature, out var player))
        {
            return false;
        }

        var delta = player.Block - previousBlock;
        if (delta >= 0)
        {
            return false;
        }

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerBlockChanged,
            player,
            new
            {
                playerId = player.PlayerId,
                delta,
                block = player.Block,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp,
                reason
            });

        return true;
    }

    public static bool PublishBlockBrokenFromTransition(GameEventBus eventBus, GameStateStore stateStore, object? creature, int previousBlock)
    {
        if (!TryCreatePlayerSnapshot(creature, out var player))
        {
            return false;
        }

        var delta = player.Block - previousBlock;
        if (previousBlock <= 0 || delta >= 0)
        {
            return false;
        }

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerBlockChanged,
            player,
            new
            {
                playerId = player.PlayerId,
                delta,
                block = player.Block,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp,
                reason = "broken"
            });

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerBlockBroken,
            player,
            new
            {
                playerId = player.PlayerId,
                previousBlock,
                block = player.Block,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp
            },
            updateState: false);

        return true;
    }

    public static bool PublishPlayerDied(GameEventBus eventBus, GameStateStore stateStore, object? creature, bool wasRemovalPrevented)
    {
        if (!TryCreatePlayerSnapshot(creature, out var player))
        {
            return false;
        }

        Publish(
            eventBus,
            stateStore,
            EventTypes.PlayerDied,
            player,
            new
            {
                playerId = player.PlayerId,
                currentHp = player.CurrentHp,
                maxHp = player.MaxHp,
                block = player.Block,
                wasRemovalPrevented
            });

        return true;
    }

    public static bool TryGetBlockValue(object? creature, out int block)
    {
        block = default;
        return creature is not null && RuntimeReflectionHelpers.TryGetInt(creature, BlockMemberNames, out block);
    }

    private static void Publish(
        GameEventBus eventBus,
        GameStateStore stateStore,
        string eventType,
        PlayerSnapshot player,
        object payload,
        bool updateState = true)
    {
        if (updateState)
        {
            UpdateState(stateStore, player);
        }

        var snapshot = stateStore.GetSnapshot();
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: eventType,
            RunId: snapshot.RunId,
            Floor: snapshot.Floor,
            RoomType: snapshot.RoomType,
            Payload: payload));
    }

    private static void UpdateState(GameStateStore stateStore, PlayerSnapshot player)
    {
        var snapshot = stateStore.GetSnapshot();
        stateStore.Update(snapshot with
        {
            Player = snapshot.Player with
            {
                Hp = player.CurrentHp,
                MaxHp = player.MaxHp,
                Block = player.Block
            }
        });
    }

    private static bool TryCreatePlayerSnapshot(object? creature, out PlayerSnapshot player)
    {
        player = default;
        if (creature is null)
        {
            return false;
        }

        if (HasAnyMember(creature, NonPlayerMarkers))
        {
            return false;
        }

        if (!RuntimeReflectionHelpers.TryGetString(creature, PlayerIdMemberNames, out var playerId))
        {
            return false;
        }

        if (!RuntimeReflectionHelpers.TryGetInt(creature, CurrentHpMemberNames, out var currentHp) ||
            !RuntimeReflectionHelpers.TryGetInt(creature, MaxHpMemberNames, out var maxHp) ||
            !RuntimeReflectionHelpers.TryGetInt(creature, BlockMemberNames, out var block))
        {
            ModLog.Warn($"Player event skipped because required members were missing on '{creature.GetType().FullName}'.");
            return false;
        }

        player = new PlayerSnapshot(playerId, currentHp, maxHp, block);
        return true;
    }

    private static bool HasAnyMember(object instance, IReadOnlyList<string> memberNames)
    {
        foreach (var memberName in memberNames)
        {
            if (RuntimeReflectionHelpers.GetMemberValue(instance, memberName) is not null)
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct PlayerSnapshot(string PlayerId, int CurrentHp, int MaxHp, int Block);
}
