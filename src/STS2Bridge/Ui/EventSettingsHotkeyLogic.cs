using Godot;

namespace STS2Bridge.Ui;

public static class EventSettingsHotkeyLogic
{
    public const Key DefaultToggleKeycode = Key.F8;

    public static bool ShouldTogglePopup(bool isPressed, bool isEcho, long keycode)
    {
        return isPressed && !isEcho && keycode == (long)DefaultToggleKeycode;
    }

    public static bool ShouldTogglePopup(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent)
        {
            return false;
        }

        if (!keyEvent.Pressed || keyEvent.Echo)
        {
            return false;
        }

        return keyEvent.Keycode == DefaultToggleKeycode || keyEvent.PhysicalKeycode == DefaultToggleKeycode;
    }
}
