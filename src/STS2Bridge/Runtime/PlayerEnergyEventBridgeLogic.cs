using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;

namespace STS2Bridge.Runtime;

internal static class PlayerEnergyEventBridgeLogic
{
    private static readonly string[] PlayerMemberNames = ["Player", "player", "_player"];
    private static readonly string[] PlayerIdMemberNames = ["PlayerId", "playerId"];
    private static readonly string[] EnergyMemberNames = ["Energy", "energy"];
    private static readonly string[] MaxEnergyMemberNames = ["MaxEnergy", "maxEnergy"];

    public static bool PublishEnergyChanged(GameEventBus eventBus, GameStateStore stateStore, object? playerCombatState, int previousEnergy)
    {
        if (!TryCreateSnapshot(playerCombatState, out var snapshot))
        {
            return false;
        }

        var delta = snapshot.Energy - previousEnergy;
        if (delta == 0)
        {
            return false;
        }

        var state = stateStore.GetSnapshot();
        stateStore.Update(state with
        {
            Player = state.Player with
            {
                Energy = snapshot.Energy
            }
        });

        var updated = stateStore.GetSnapshot();
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: EventTypes.PlayerEnergyChanged,
            RunId: updated.RunId,
            Floor: updated.Floor,
            RoomType: updated.RoomType,
            Payload: new
            {
                playerId = snapshot.PlayerId,
                delta,
                energy = snapshot.Energy,
                maxEnergy = snapshot.MaxEnergy
            }));

        return true;
    }

    public static bool TryGetEnergyValue(object? playerCombatState, out int energy)
    {
        energy = default;
        return playerCombatState is not null &&
               RuntimeReflectionHelpers.TryGetInt(playerCombatState, EnergyMemberNames, out energy);
    }

    private static bool TryCreateSnapshot(object? playerCombatState, out PlayerEnergySnapshot snapshot)
    {
        snapshot = default;
        if (playerCombatState is null)
        {
            return false;
        }

        var player = GetFirstExistingMember(playerCombatState, PlayerMemberNames);
        if (!RuntimeReflectionHelpers.TryGetString(player, PlayerIdMemberNames, out var playerId) ||
            !RuntimeReflectionHelpers.TryGetInt(playerCombatState, EnergyMemberNames, out var energy) ||
            !RuntimeReflectionHelpers.TryGetInt(playerCombatState, MaxEnergyMemberNames, out var maxEnergy))
        {
            ModLog.Warn($"Player energy event skipped because required members were missing on '{playerCombatState.GetType().FullName}'.");
            return false;
        }

        snapshot = new PlayerEnergySnapshot(playerId, energy, maxEnergy);
        return true;
    }

    private static object? GetFirstExistingMember(object? instance, IReadOnlyList<string> memberNames)
    {
        if (instance is null)
        {
            return null;
        }

        foreach (var memberName in memberNames)
        {
            var value = RuntimeReflectionHelpers.GetMemberValue(instance, memberName);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private readonly record struct PlayerEnergySnapshot(string PlayerId, int Energy, int MaxEnergy);
}
