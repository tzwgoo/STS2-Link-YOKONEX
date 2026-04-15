using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Ui;

[HarmonyPatch]
internal static class EventSettingsHotkeyPatches
{
    [HarmonyPatch]
    private static class NGameInputPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var gameType = Type.GetType("MegaCrit.Sts2.Core.Nodes.NGame, sts2")
                ?? throw new InvalidOperationException("Could not locate MegaCrit.Sts2.Core.Nodes.NGame.");

            return AccessTools.Method(gameType, "_Input")
                ?? throw new InvalidOperationException($"Could not locate method '{gameType.FullName}._Input'.");
        }

        [HarmonyPostfix]
        private static void Postfix(object?[] __args)
        {
            if (__args is null)
            {
                return;
            }

            foreach (var arg in __args)
            {
                if (arg is Godot.InputEvent inputEvent && EventSettingsHotkeyLogic.ShouldTogglePopup(inputEvent))
                {
                    EventSettingsUiController.TogglePopup();
                    return;
                }
            }
        }
    }
}
