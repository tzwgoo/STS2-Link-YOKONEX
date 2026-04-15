using Godot;
using STS2Bridge.Logging;

namespace STS2Bridge.Ui;

public static class SettingsScreenBridge
{
    private const string ButtonNodeName = "STS2LinkYOKONEXEventsButton";

    public static void Install(object? settingsScreenInstance)
    {
        if (settingsScreenInstance is not Control settingsScreen)
        {
            return;
        }

        if (settingsScreen.GetNodeOrNull(ButtonNodeName) is not null)
        {
            return;
        }

        var button = new Button
        {
            Name = ButtonNodeName,
            Text = "STS2-Link-YOKONEX Events",
            TooltipText = "Open bridge event toggles"
        };
        button.AnchorLeft = 1;
        button.AnchorRight = 1;
        button.AnchorTop = 0;
        button.AnchorBottom = 0;
        button.OffsetLeft = -236;
        button.OffsetRight = -16;
        button.OffsetTop = 18;
        button.OffsetBottom = 56;

        EventSettingsUiController.EnsurePopup(settingsScreen);

        button.Pressed += EventSettingsUiController.TogglePopup;
        settingsScreen.AddChild(button);

        ModLog.Info("Installed STS2-Link-YOKONEX settings button.");
    }
}
