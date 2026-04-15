using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;
using STS2Bridge.State.Dtos;

namespace STS2Bridge.Runtime;

internal static class EnemyEventBridgeLogic
{
    private static readonly string[] EnemyIdMemberNames = ["MonsterId", "monsterId", "EnemyId", "enemyId", "Id", "id"];
    private static readonly string[] EnemyNameMemberNames = ["Name", "name"];
    private static readonly string[] CurrentHpMemberNames = ["CurrentHp", "currentHp"];
    private static readonly string[] MaxHpMemberNames = ["MaxHp", "maxHp"];
    private static readonly string[] BlockMemberNames = ["Block", "block"];
    private static readonly string[] PlayerMarkers = ["PlayerId", "playerId"];
    private static readonly string[] BlockedDamageMemberNames = ["BlockedDamage", "blockedDamage"];
    private static readonly string[] UnblockedDamageMemberNames = ["UnblockedDamage", "unblockedDamage"];
    private static readonly string[] WasBlockBrokenMemberNames = ["WasBlockBroken", "wasBlockBroken"];
    private static readonly string[] WasFullyBlockedMemberNames = ["WasFullyBlocked", "wasFullyBlocked"];

    public static bool PublishHpChanged(GameEventBus eventBus, GameStateStore stateStore, object? creature, int delta)
    {
        if (!TryCreateEnemySnapshot(creature, out var enemy))
        {
            return false;
        }

        UpdateState(stateStore, enemy);

        var state = stateStore.GetSnapshot();
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: EventTypes.EnemyHpChanged,
            RunId: state.RunId,
            Floor: state.Floor,
            RoomType: state.RoomType,
            Payload: new
            {
                enemyId = enemy.EnemyId,
                enemyName = enemy.EnemyName,
                delta,
                currentHp = enemy.CurrentHp,
                maxHp = enemy.MaxHp,
                block = enemy.Block
            }));

        return true;
    }

    public static bool PublishDamaged(GameEventBus eventBus, GameStateStore stateStore, object? target, object? result)
    {
        if (!TryCreateEnemySnapshot(target, out var enemy) || !TryCreateDamageSnapshot(result, out var damage))
        {
            return false;
        }

        var state = stateStore.GetSnapshot();
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: EventTypes.EnemyDamaged,
            RunId: state.RunId,
            Floor: state.Floor,
            RoomType: state.RoomType,
            Payload: new
            {
                enemyId = enemy.EnemyId,
                enemyName = enemy.EnemyName,
                amount = damage.BlockedDamage + damage.UnblockedDamage,
                blockedDamage = damage.BlockedDamage,
                unblockedDamage = damage.UnblockedDamage,
                wasBlockBroken = damage.WasBlockBroken,
                wasFullyBlocked = damage.WasFullyBlocked
            }));

        return true;
    }

    public static object? FindEnemyArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (TryCreateEnemySnapshot(arg, out _))
            {
                return arg;
            }
        }

        return null;
    }

    public static object? FindDamageResultArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (TryCreateDamageSnapshot(arg, out _))
            {
                return arg;
            }
        }

        return null;
    }

    private static void UpdateState(GameStateStore stateStore, EnemySnapshot enemy)
    {
        var snapshot = stateStore.GetSnapshot();
        var enemies = snapshot.Enemies.ToList();
        var index = enemies.FindIndex(item => string.Equals(item.InstanceId, enemy.EnemyId, StringComparison.Ordinal));
        var next = new EnemyStateDto(
            enemy.EnemyId,
            enemy.EnemyName ?? enemy.EnemyId,
            enemy.CurrentHp,
            enemy.MaxHp,
            enemy.Block,
            enemy.CurrentHp > 0);

        if (index >= 0)
        {
            enemies[index] = next;
        }
        else
        {
            enemies.Add(next);
        }

        stateStore.Update(snapshot with
        {
            Enemies = enemies
        });
    }

    private static bool TryCreateEnemySnapshot(object? creature, out EnemySnapshot snapshot)
    {
        snapshot = default;
        if (creature is null || HasAnyMember(creature, PlayerMarkers))
        {
            return false;
        }

        if (!RuntimeReflectionHelpers.TryGetString(creature, EnemyIdMemberNames, out var enemyId) ||
            !RuntimeReflectionHelpers.TryGetInt(creature, CurrentHpMemberNames, out var currentHp) ||
            !RuntimeReflectionHelpers.TryGetInt(creature, MaxHpMemberNames, out var maxHp) ||
            !RuntimeReflectionHelpers.TryGetInt(creature, BlockMemberNames, out var block))
        {
            return false;
        }

        RuntimeReflectionHelpers.TryGetString(creature, EnemyNameMemberNames, out var enemyName);
        snapshot = new EnemySnapshot(enemyId, string.IsNullOrWhiteSpace(enemyName) ? null : enemyName, currentHp, maxHp, block);
        return true;
    }

    private static bool TryCreateDamageSnapshot(object? result, out DamageSnapshot snapshot)
    {
        snapshot = default;
        if (result is null)
        {
            return false;
        }

        if (!RuntimeReflectionHelpers.TryGetInt(result, BlockedDamageMemberNames, out var blockedDamage) ||
            !RuntimeReflectionHelpers.TryGetInt(result, UnblockedDamageMemberNames, out var unblockedDamage) ||
            !RuntimeReflectionHelpers.TryGetBool(result, WasBlockBrokenMemberNames, out var wasBlockBroken) ||
            !RuntimeReflectionHelpers.TryGetBool(result, WasFullyBlockedMemberNames, out var wasFullyBlocked))
        {
            ModLog.Warn($"Enemy damage event skipped because damage result members were missing on '{result.GetType().FullName}'.");
            return false;
        }

        snapshot = new DamageSnapshot(blockedDamage, unblockedDamage, wasBlockBroken, wasFullyBlocked);
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

    private readonly record struct EnemySnapshot(string EnemyId, string? EnemyName, int CurrentHp, int MaxHp, int Block);

    private readonly record struct DamageSnapshot(int BlockedDamage, int UnblockedDamage, bool WasBlockBroken, bool WasFullyBlocked);
}
