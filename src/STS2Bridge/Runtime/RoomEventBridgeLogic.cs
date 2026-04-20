using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;

namespace STS2Bridge.Runtime;

internal static class RoomEventBridgeLogic
{
    private static readonly string[] RoomTypeMemberNames = ["RoomType", "roomType"];
    private static readonly string[] ModelIdMemberNames = ["ModelId", "modelId"];
    private static readonly string[] TotalFloorMemberNames = ["TotalFloor", "totalFloor", "ActFloor", "actFloor"];

    public static bool PublishRoomEntered(GameEventBus eventBus, GameStateStore stateStore, object? room)
    {
        return PublishRoomEntered(eventBus, stateStore, runState: null, room);
    }

    public static bool PublishRoomEntered(GameEventBus eventBus, GameStateStore stateStore, object? runState, object? room)
    {
        if (!TryCreateRoomSnapshot(runState, room, out var snapshot))
        {
            return false;
        }

        var state = stateStore.GetSnapshot();
        stateStore.Update(state with
        {
            RoomType = snapshot.RoomType,
            Floor = snapshot.Floor ?? state.Floor
        });

        var updated = stateStore.GetSnapshot();
        ModLog.Info($"AfterRoomEntered published. roomType={updated.RoomType} floor={updated.Floor} runStateType={runState?.GetType().FullName ?? "<null>"} roomTypeArg={room?.GetType().FullName ?? "<null>"}");
        eventBus.Publish(new GameEvent(
            EventId: $"evt-{Guid.NewGuid():N}",
            Type: EventTypes.RoomEntered,
            RunId: updated.RunId,
            Floor: updated.Floor,
            RoomType: updated.RoomType,
            Payload: new
            {
                roomType = snapshot.RoomType,
                modelId = snapshot.ModelId,
                floor = updated.Floor,
                source = "hook.after_room_entered"
            }));

        return true;
    }

    public static object? FindRoomArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (TryCreateRoomSnapshot(arg, out _))
            {
                return arg;
            }
        }

        return null;
    }

    private static bool TryCreateRoomSnapshot(object? room, out RoomSnapshot snapshot)
    {
        return TryCreateRoomSnapshot(runState: null, room, out snapshot);
    }

    private static bool TryCreateRoomSnapshot(object? runState, object? room, out RoomSnapshot snapshot)
    {
        snapshot = default;
        if (room is null)
        {
            return false;
        }

        if (!TryGetRoomType(room, out var roomType))
        {
            ModLog.Warn($"Room event skipped because room type was missing on '{room.GetType().FullName}'.");
            return false;
        }

        RuntimeReflectionHelpers.TryGetString(room, ModelIdMemberNames, out var modelId);
        int? floor = TryGetFloor(runState, out var totalFloor) ? totalFloor : null;
        snapshot = new RoomSnapshot(
            NormalizeRoomType(roomType),
            string.IsNullOrWhiteSpace(modelId) ? null : modelId,
            floor);
        return true;
    }

    private static bool TryGetFloor(object? runState, out int floor)
    {
        floor = default;
        return runState is not null && RuntimeReflectionHelpers.TryGetInt(runState, TotalFloorMemberNames, out floor);
    }

    private static bool TryGetRoomType(object room, out string roomType)
    {
        roomType = string.Empty;
        foreach (var memberName in RoomTypeMemberNames)
        {
            var value = RuntimeReflectionHelpers.GetMemberValue(room, memberName);
            if (value is null)
            {
                continue;
            }

            roomType = value.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(roomType))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeRoomType(string roomType)
    {
        return roomType.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private readonly record struct RoomSnapshot(string RoomType, string? ModelId, int? Floor);
}
