using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Runtime;

internal static class RoomHookPatches
{
    [HarmonyPatch]
    private static class AfterRoomEnteredPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterRoomEntered");

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            var runState = __args is { Length: > 0 } ? __args[0] : null;
            var room = RoomEventBridgeLogic.FindRoomArgument(__args);
            RoomEventBridgeLogic.PublishRoomEntered(ModEntry.EventBus, ModEntry.StateStore, runState, room);
        }
    }

    private static MethodBase GetRequiredHookMethod(string methodName)
    {
        var hookType = Type.GetType("MegaCrit.Sts2.Core.Hooks.Hook, sts2")
            ?? throw new InvalidOperationException("Could not locate MegaCrit.Sts2.Core.Hooks.Hook.");

        return AccessTools.Method(hookType, methodName)
            ?? throw new InvalidOperationException($"Could not locate hook method '{hookType.FullName}.{methodName}'.");
    }
}
