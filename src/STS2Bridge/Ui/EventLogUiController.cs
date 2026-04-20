using Godot;

namespace STS2Bridge.Ui;

public static class EventLogUiController
{
    private static WeakReference<EventLogPopup>? _popupRef;

    public static void EnsurePopup(Node host)
    {
        RemoveDuplicatePopups(host);

        if (TryGetPopup(out _))
        {
            return;
        }

        var popup = new EventLogPopup(ModEntry.EventBus);
        host.AddChild(popup);
        _popupRef = new WeakReference<EventLogPopup>(popup);
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

    private static bool TryGetPopup(out EventLogPopup popup)
    {
        popup = null!;

        if (_popupRef is null || !_popupRef.TryGetTarget(out var existing) || !GodotObject.IsInstanceValid(existing))
        {
            return false;
        }

        popup = existing;
        return true;
    }

    private static void RemoveDuplicatePopups(Node host)
    {
        var sceneRoot = host.GetTree()?.CurrentScene ?? host.GetTree()?.Root;
        if (sceneRoot is null)
        {
            return;
        }

        var popups = FindPopups(sceneRoot).ToList();
        if (popups.Count == 0)
        {
            return;
        }

        EventLogPopup? keep = null;
        if (_popupRef is not null && _popupRef.TryGetTarget(out var existing) && GodotObject.IsInstanceValid(existing))
        {
            keep = existing;
        }
        else
        {
            keep = popups[0];
            _popupRef = new WeakReference<EventLogPopup>(keep);
        }

        foreach (var popup in popups)
        {
            if (!ReferenceEquals(popup, keep))
            {
                popup.QueueFree();
            }
        }
    }

    private static IEnumerable<EventLogPopup> FindPopups(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is EventLogPopup popup)
            {
                yield return popup;
            }

            foreach (var nested in FindPopups(child))
            {
                yield return nested;
            }
        }
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
