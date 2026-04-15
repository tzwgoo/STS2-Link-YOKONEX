using Godot;
using STS2Bridge.Ui;

namespace STS2Bridge.Tests.Ui;

public sealed class EventSettingsHotkeyLogicTests
{
    [Fact]
    public void ShouldTogglePopup_should_return_true_for_pressed_f8()
    {
        Assert.True(EventSettingsHotkeyLogic.ShouldTogglePopup(isPressed: true, isEcho: false, keycode: (long)Key.F8));
    }

    [Fact]
    public void DefaultToggleKeycode_should_be_godot_f8()
    {
        Assert.Equal(Key.F8, EventSettingsHotkeyLogic.DefaultToggleKeycode);
    }

    [Fact]
    public void ShouldTogglePopup_should_return_false_for_other_keys_or_key_up()
    {
        Assert.False(EventSettingsHotkeyLogic.ShouldTogglePopup(isPressed: true, isEcho: false, keycode: (long)Key.F7));
        Assert.False(EventSettingsHotkeyLogic.ShouldTogglePopup(isPressed: false, isEcho: false, keycode: (long)Key.F8));
    }
}
