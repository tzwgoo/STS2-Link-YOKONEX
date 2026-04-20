using Godot;
using STS2Bridge.Events;

namespace STS2Bridge.Ui;

public sealed class EventLogPopup : PanelContainer
{
    public const string PopupNodeName = "STS2LinkYOKONEXEventLogPopup";

    private readonly GameEventBus _eventBus;
    private readonly int _displayLimit;
    private VBoxContainer _list = null!;
    private Label _countLabel = null!;
    private long _lastRenderedVersion = -1;

    public EventLogPopup(GameEventBus eventBus, int displayLimit = 100)
    {
        Name = PopupNodeName;
        _eventBus = eventBus;
        _displayLimit = Math.Max(10, displayLimit);
        Visible = false;
        BuildUi();
    }

    public override void _Ready()
    {
        base._Ready();
        ApplyViewportBounds();
        SetProcess(true);
        if (GetViewport() is { } viewport)
        {
            viewport.SizeChanged += ApplyViewportBounds;
        }
        RefreshEntries();
    }

    public override void _ExitTree()
    {
        if (GetViewport() is { } viewport)
        {
            viewport.SizeChanged -= ApplyViewportBounds;
        }

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
        {
            return;
        }

        var currentVersion = _eventBus.RecentVersion;
        if (currentVersion != _lastRenderedVersion)
        {
            RefreshEntries();
        }
    }

    public void ToggleVisibility()
    {
        Visible = !Visible;
        RefreshEntries();
    }

    public void HidePopup()
    {
        Visible = false;
    }

    private void BuildUi()
    {
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 0;
        AnchorBottom = 0;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("10151d"), new Color("2a3240"), 24, 2));

        var root = new MarginContainer();
        root.AnchorRight = 1;
        root.AnchorBottom = 1;
        root.AddThemeConstantOverride("margin_left", 22);
        root.AddThemeConstantOverride("margin_top", 22);
        root.AddThemeConstantOverride("margin_right", 22);
        root.AddThemeConstantOverride("margin_bottom", 22);
        AddChild(root);

        var layout = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddThemeConstantOverride("separation", 14);
        root.AddChild(layout);

        layout.AddChild(BuildHeader());
        layout.AddChild(BuildLogBody());
        layout.AddChild(BuildFooter());
    }

    private Control BuildHeader()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("141b24"), new Color("344154"), 20, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 6);
        margin.AddChild(stack);

        stack.AddChild(CreateMutedLabel("事件观察台", 13, new Color("7ec7ff")));
        stack.AddChild(CreateTitleLabel("STS2-Link-YOKONEX Logs", 26));

        var subtitle = CreateMutedLabel("展示最近触发的游戏事件日志，方便你核对事件顺序、数值变化和外部联动前的原始触发情况。", 14, new Color("c0ccda"));
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(subtitle);

        _countLabel = CreateMutedLabel(string.Empty, 13, new Color("9db0c7"));
        stack.AddChild(_countLabel);

        return panel;
    }

    private Control BuildLogBody()
    {
        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 320),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("131a23"), new Color("2f3a49"), 20, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        card.AddChild(margin);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 280),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        margin.AddChild(scroll);

        _list = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_list);

        return card;
    }

    private Control BuildFooter()
    {
        var footer = new PanelContainer();
        footer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("141b24"), new Color("344154"), 18, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        footer.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        margin.AddChild(row);

        var note = CreateMutedLabel("日志只记录最近事件，不会影响游戏逻辑。清空后，新的事件仍会继续追加进来。", 13, new Color("9db0c7"));
        note.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(note);

        var clearButton = CreateActionButton("清空日志", new Color("2e6c96"), new Color("4e96c6"));
        clearButton.Pressed += () =>
        {
            _eventBus.ClearRecentEvents();
            RefreshEntries();
        };
        row.AddChild(clearButton);

        var closeButton = CreateSecondaryButton("关闭");
        closeButton.Pressed += HidePopup;
        row.AddChild(closeButton);

        return footer;
    }

    public void RefreshEntries()
    {
        if (_list is null)
        {
            return;
        }

        foreach (Node child in _list.GetChildren())
        {
            child.QueueFree();
        }

        var events = _eventBus.GetRecentEvents(_displayLimit);
        _countLabel.Text = $"当前展示最近 {events.Count} / {_displayLimit} 条事件";
        _lastRenderedVersion = _eventBus.RecentVersion;

        if (events.Count == 0)
        {
            _list.AddChild(BuildEmptyState());
            return;
        }

        foreach (var gameEvent in events.Reverse())
        {
            _list.AddChild(BuildLogEntry(gameEvent));
        }
    }

    private Control BuildEmptyState()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("17202b"), new Color("2b394d"), 16, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        var label = CreateMutedLabel("还没有事件日志。触发一次战斗、打牌或数值变化后，这里就会显示记录。", 14, new Color("b8c5d6"));
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        margin.AddChild(label);
        return panel;
    }

    private Control BuildLogEntry(GameEvent gameEvent)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 94),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("17202b"), new Color("2b394d"), 16, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);

        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 4);
        margin.AddChild(stack);

        stack.AddChild(CreateSectionLabel(EventLogDisplayLogic.BuildTitle(gameEvent), 18, new Color("f4f7fb")));
        stack.AddChild(CreateMutedLabel(gameEvent.Type, 12, new Color("8ba0bb")));
        stack.AddChild(CreateMutedLabel(EventLogDisplayLogic.BuildMeta(gameEvent), 12, new Color("6f8299")));

        var summary = CreateMutedLabel(EventLogDisplayLogic.BuildSummary(gameEvent), 14, new Color("e7edf6"));
        summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(summary);

        return panel;
    }

    private void ApplyViewportBounds()
    {
        var viewportSize = GetViewportRect().Size;
        if (viewportSize == Vector2.Zero)
        {
            return;
        }

        var rect = EventSettingsLayoutLogic.CalculatePopupRect(viewportSize);
        OffsetLeft = rect.Position.X;
        OffsetTop = rect.Position.Y;
        OffsetRight = rect.End.X;
        OffsetBottom = rect.End.Y;

        if (_list is not null)
        {
            _list.CustomMinimumSize = new Vector2(0, Mathf.Max(260, rect.Size.Y - 240));
        }
    }

    private static StyleBoxFlat CreatePanelStyle(Color background, Color border, int radius, int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            ContentMarginLeft = 12,
            ContentMarginTop = 8,
            ContentMarginRight = 12,
            ContentMarginBottom = 8
        };
    }

    private static StyleBoxFlat CreateButtonStyle(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            ContentMarginLeft = 18,
            ContentMarginTop = 10,
            ContentMarginRight = 18,
            ContentMarginBottom = 10
        };
    }

    private static Button CreateActionButton(string text, Color baseColor, Color borderColor)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(118, 42)
        };
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(baseColor, borderColor));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f)));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(baseColor.Darkened(0.08f), borderColor.Darkened(0.08f)));
        button.AddThemeColorOverride("font_color", Colors.White);
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(96, 42)
        };
        var background = new Color("1d2733");
        var border = new Color("44556e");
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(background, border));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(background.Lightened(0.08f), border.Lightened(0.08f)));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(background.Darkened(0.08f), border.Darkened(0.08f)));
        button.AddThemeColorOverride("font_color", new Color("e5edf7"));
        return button;
    }

    private static Label CreateTitleLabel(string text, int fontSize)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color("f7f9fc"));
        return label;
    }

    private static Label CreateSectionLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label CreateMutedLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
