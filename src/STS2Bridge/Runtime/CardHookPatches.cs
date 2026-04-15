using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Runtime;

internal static class CardHookPatches
{
    [HarmonyPatch]
    private static class AfterCardPlayedPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterCardPlayed");

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            var cardPlay = CardEventBridgeLogic.FindCardPlayArgument(__args);
            if (cardPlay is null)
            {
                return;
            }

            CardEventBridgeLogic.PublishCardPlayed(ModEntry.EventBus, ModEntry.StateStore, cardPlay);
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
