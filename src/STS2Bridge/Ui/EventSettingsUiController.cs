using Godot;

namespace STS2Bridge.Ui;

public static class EventSettingsUiController
{
    private static WeakReference<EventSettingsPopup>? _popupRef;

    public static void EnsurePopup(Node host)
    {
        if (TryGetPopup(out _))
        {
            return;
        }

        var popup = new EventSettingsPopup(ModEntry.EventToggles, ModEntry.SaveSettings);
        host.AddChild(popup);
        _popupRef = new WeakReference<EventSettingsPopup>(popup);
    }

    public static void TogglePopup()
    {
        var host = ResolveHost();
        if (host is null)
        {
            return;
        }

        EnsurePopup(host);
        if (TryGetPopup(out var popup))
        {
            popup.ToggleVisibility();
        }
    }

    public static void HidePopup()
    {
        if (TryGetPopup(out var popup))
        {
            popup.HidePopup();
        }
    }

    private static bool TryGetPopup(out EventSettingsPopup popup)
    {
        popup = null!;

        if (_popupRef is null || !_popupRef.TryGetTarget(out var existing) || !GodotObject.IsInstanceValid(existing))
        {
            return false;
        }

        popup = existing;
        return true;
    }

    private static Node? ResolveHost()
    {
        var gameType = Type.GetType("MegaCrit.Sts2.Core.Nodes.NGame, sts2");
        var instance = gameType?.GetProperty("Instance")?.GetValue(null);
        if (instance is null)
        {
            return null;
        }

        return gameType?.GetProperty("RootSceneContainer")?.GetValue(instance) as Node
            ?? gameType?.GetProperty("CurrentRunNode")?.GetValue(instance) as Node
            ?? gameType?.GetProperty("MainMenu")?.GetValue(instance) as Node;
    }
}
