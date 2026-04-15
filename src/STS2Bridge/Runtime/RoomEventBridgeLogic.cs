using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.State;

namespace STS2Bridge.Runtime;

internal static class RoomEventBridgeLogic
{
    private static readonly string[] RoomTypeMemberNames = ["RoomType", "roomType"];
    private static readonly string[] ModelIdMemberNames = ["ModelId", "modelId"];

    public static bool PublishRoomEntered(GameEventBus eventBus, GameStateStore stateStore, object? room)
    {
        if (!TryCreateRoomSnapshot(room, out var snapshot))
        {
            return false;
        }

        var state = stateStore.GetSnapshot();
        stateStore.Update(state with
        {
            RoomType = snapshot.RoomType
        });

        var updated = stateStore.GetSnapshot();
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
        snapshot = new RoomSnapshot(
            NormalizeRoomType(roomType),
            string.IsNullOrWhiteSpace(modelId) ? null : modelId);
        return true;
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

    private readonly record struct RoomSnapshot(string RoomType, string? ModelId);
}
