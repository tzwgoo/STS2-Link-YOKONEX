using HarmonyLib;
using System.Reflection;

namespace STS2Bridge.Ui;

[HarmonyPatch]
internal static class SettingsScreenPatches
{
    [HarmonyPatch]
    private static class SettingsScreenReadyPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var settingsScreenType = Type.GetType("MegaCrit.Sts2.Core.Nodes.Screens.Settings.NSettingsScreen, sts2")
                ?? throw new InvalidOperationException("Could not locate MegaCrit.Sts2.Core.Nodes.Screens.Settings.NSettingsScreen.");

            return AccessTools.Method(settingsScreenType, "_Ready")
                ?? throw new InvalidOperationException($"Could not locate method '{settingsScreenType.FullName}._Ready'.");
        }

        [HarmonyPostfix]
        private static void Postfix(object? __instance)
        {
            SettingsScreenBridge.Install(__instance);
        }
    }
}
