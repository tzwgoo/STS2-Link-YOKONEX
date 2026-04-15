using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Runtime;

internal static class EnemyHookPatches
{
    [HarmonyPatch]
    private static class AfterCurrentHpChangedPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterCurrentHpChanged");

        [HarmonyPostfix]
        private static void Postfix(object? __2, int __3)
        {
            EnemyEventBridgeLogic.PublishHpChanged(ModEntry.EventBus, ModEntry.StateStore, __2, __3);
        }
    }

    [HarmonyPatch]
    private static class AfterDamageReceivedPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterDamageReceived");

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            var target = EnemyEventBridgeLogic.FindEnemyArgument(__args);
            var result = EnemyEventBridgeLogic.FindDamageResultArgument(__args);
            EnemyEventBridgeLogic.PublishDamaged(ModEntry.EventBus, ModEntry.StateStore, target, result);
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
