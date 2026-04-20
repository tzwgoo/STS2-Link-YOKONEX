using Godot;
using STS2Bridge.Logging;

namespace STS2Bridge.Ui;

public static class SettingsScreenBridge
{
    private const string EventsButtonNodeName = "STS2LinkYOKONEXEventsButton";
    private const string LogsButtonNodeName = "STS2LinkYOKONEXLogsButton";

    public static void Install(object? settingsScreenInstance)
    {
        if (settingsScreenInstance is not Control settingsScreen)
        {
            return;
        }

        if (settingsScreen.GetNodeOrNull(EventsButtonNodeName) is not null ||
            settingsScreen.GetNodeOrNull(LogsButtonNodeName) is not null)
        {
            return;
        }

        var eventsButton = new Button
        {
            Name = EventsButtonNodeName,
            Text = "STS2-Link-YOKONEX Events",
            TooltipText = "Open bridge event toggles"
        };
        eventsButton.AnchorLeft = 1;
        eventsButton.AnchorRight = 1;
        eventsButton.AnchorTop = 0;
        eventsButton.AnchorBottom = 0;
        eventsButton.OffsetLeft = -236;
        eventsButton.OffsetRight = -16;
        eventsButton.OffsetTop = 18;
        eventsButton.OffsetBottom = 56;
        eventsButton.Pressed += EventSettingsUiController.TogglePopup;
        settingsScreen.AddChild(eventsButton);

        var logsButton = new Button
        {
            Name = LogsButtonNodeName,
            Text = "STS2-Link-YOKONEX Logs",
            TooltipText = "Open recent event logs"
        };
        logsButton.AnchorLeft = 1;
        logsButton.AnchorRight = 1;
        logsButton.AnchorTop = 0;
        logsButton.AnchorBottom = 0;
        logsButton.OffsetLeft = -236;
        logsButton.OffsetRight = -16;
        logsButton.OffsetTop = 62;
        logsButton.OffsetBottom = 100;
        logsButton.Pressed += EventLogUiController.TogglePopup;
        settingsScreen.AddChild(logsButton);

        ModLog.Info("Installed STS2-Link-YOKONEX settings buttons.");
    }
}
