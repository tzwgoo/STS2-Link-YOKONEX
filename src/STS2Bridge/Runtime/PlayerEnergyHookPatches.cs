using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Runtime;

internal static class PlayerEnergyHookPatches
{
    [HarmonyPatch]
    private static class PlayerCombatStateSetEnergyPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredPlayerCombatStateMethod("set_Energy");

        [HarmonyPrefix]
        private static void Prefix(object? __instance, ref int __state)
        {
            __state = PlayerEnergyEventBridgeLogic.TryGetEnergyValue(__instance, out var energy) ? energy : int.MinValue;
        }

        [HarmonyPostfix]
        private static void Postfix(object? __instance, int __state)
        {
            if (__state == int.MinValue)
            {
                return;
            }

            PlayerEnergyEventBridgeLogic.PublishEnergyChanged(ModEntry.EventBus, ModEntry.StateStore, __instance, __state);
        }
    }

    private static MethodBase GetRequiredPlayerCombatStateMethod(string methodName)
    {
        var playerCombatStateType = Type.GetType("MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState, sts2")
            ?? throw new InvalidOperationException("Could not locate MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState.");

        return AccessTools.Method(playerCombatStateType, methodName)
            ?? throw new InvalidOperationException($"Could not locate player combat state method '{playerCombatStateType.FullName}.{methodName}'.");
    }
}
