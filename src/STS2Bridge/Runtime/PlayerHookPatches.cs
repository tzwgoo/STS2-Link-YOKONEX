using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Runtime;

internal static class PlayerHookPatches
{
    private const string HookTypeName = "MegaCrit.Sts2.Core.Hooks.Hook, sts2";

    [HarmonyPatch]
    private static class AfterCurrentHpChangedPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterCurrentHpChanged");

        [HarmonyPostfix]
        private static void Postfix(object? __2, int __3)
        {
            PlayerEventBridgeLogic.PublishHpChanged(ModEntry.EventBus, ModEntry.StateStore, __2, __3);
        }
    }

    [HarmonyPatch]
    private static class AfterBlockGainedPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterBlockGained");

        [HarmonyPostfix]
        private static void Postfix(object? __1, int __2)
        {
            PlayerEventBridgeLogic.PublishBlockChanged(ModEntry.EventBus, ModEntry.StateStore, __1, __2, "gained");
        }
    }

    [HarmonyPatch]
    private static class AfterBlockClearedPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterBlockCleared");

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            var creature = FindPlayerCreatureArgument(__args);
            if (creature is null)
            {
                return;
            }

            PlayerEventBridgeLogic.PublishBlockCleared(ModEntry.EventBus, ModEntry.StateStore, creature);
        }
    }

    [HarmonyPatch]
    private static class AfterBlockBrokenPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterBlockBroken");

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            var creature = FindPlayerCreatureArgument(__args);
            if (creature is null)
            {
                return;
            }

            var previousBlock = ModEntry.StateStore.GetSnapshot().Player.Block;
            PlayerEventBridgeLogic.PublishBlockBrokenFromTransition(ModEntry.EventBus, ModEntry.StateStore, creature, previousBlock);
        }
    }

    [HarmonyPatch]
    private static class AfterDeathPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredHookMethod("AfterDeath");

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            var creature = FindPlayerCreatureArgument(__args);
            if (creature is null)
            {
                return;
            }

            var wasRemovalPrevented = false;
            if (__args is not null)
            {
                foreach (var arg in __args)
                {
                    if (arg is bool flag)
                    {
                        wasRemovalPrevented = flag;
                        break;
                    }
                }
            }

            PlayerEventBridgeLogic.PublishPlayerDied(ModEntry.EventBus, ModEntry.StateStore, creature, wasRemovalPrevented);
        }
    }

    [HarmonyPatch]
    private static class CreatureLoseBlockInternalPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => GetRequiredCreatureMethod("LoseBlockInternal");

        [HarmonyPrefix]
        private static void Prefix(object? __instance, ref int __state)
        {
            __state = PlayerEventBridgeLogic.TryGetBlockValue(__instance, out var block) ? block : -1;
        }

        [HarmonyPostfix]
        private static void Postfix(object? __instance, int __state)
        {
            if (__state < 0)
            {
                return;
            }

            PlayerEventBridgeLogic.PublishBlockLossFromTransition(ModEntry.EventBus, ModEntry.StateStore, __instance, __state, "lost");
        }
    }

    private static MethodBase GetRequiredHookMethod(string methodName)
    {
        var hookType = Type.GetType(HookTypeName)
            ?? throw new InvalidOperationException($"Could not locate hook type '{HookTypeName}'.");

        return AccessTools.Method(hookType, methodName)
            ?? throw new InvalidOperationException($"Could not locate hook method '{hookType.FullName}.{methodName}'.");
    }

    private static MethodBase GetRequiredCreatureMethod(string methodName)
    {
        var creatureType = Type.GetType("MegaCrit.Sts2.Core.Entities.Creatures.Creature, sts2")
            ?? throw new InvalidOperationException("Could not locate MegaCrit.Sts2.Core.Entities.Creatures.Creature.");

        return AccessTools.Method(creatureType, methodName)
            ?? throw new InvalidOperationException($"Could not locate creature method '{creatureType.FullName}.{methodName}'.");
    }

    private static object? FindPlayerCreatureArgument(object?[]? args)
    {
        if (args is null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (!PlayerEventBridgeLogic.TryGetBlockValue(arg, out _))
            {
                continue;
            }

            if (RuntimeReflectionHelpers.TryGetString(arg, ["PlayerId", "playerId"], out _))
            {
                return arg;
            }
        }

        return null;
    }
}
