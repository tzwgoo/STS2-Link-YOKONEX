using Godot;
using STS2Bridge.Config;

namespace STS2Bridge.Ui;

public sealed class EventSettingsPopup : PanelContainer
{
    public const string PopupNodeName = "STS2LinkYOKONEXEventSettingsPopup";

    private readonly EventToggleService _eventToggles;
    private readonly Action _saveSettings;

    public EventSettingsPopup(EventToggleService eventToggles, Action saveSettings)
    {
        Name = PopupNodeName;
        _eventToggles = eventToggles;
        _saveSettings = saveSettings;
        Visible = false;
        BuildUi();
    }

    public void ToggleVisibility()
    {
        Visible = !Visible;
    }

    public void HidePopup()
    {
        Visible = false;
    }

    private void BuildUi()
    {
        AnchorLeft = 0.5f;
        AnchorTop = 0.5f;
        AnchorRight = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -280;
        OffsetTop = -260;
        OffsetRight = 280;
        OffsetBottom = 260;
        MouseFilter = MouseFilterEnum.Stop;

        var root = new MarginContainer
        {
            Name = "RootMargin"
        };
        root.AnchorRight = 1;
        root.AnchorBottom = 1;
        root.AddThemeConstantOverride("margin_left", 18);
        root.AddThemeConstantOverride("margin_top", 18);
        root.AddThemeConstantOverride("margin_right", 18);
        root.AddThemeConstantOverride("margin_bottom", 18);
        AddChild(root);

        var stack = new VBoxContainer
        {
            Name = "Stack"
        };
        root.AddChild(stack);

        var title = new Label
        {
            Name = "Title",
            Text = "STS2-Link-YOKONEX Events"
        };
        stack.AddChild(title);

        var subtitle = new Label
        {
            Name = "Subtitle",
            Text = "控制桥接事件是否继续向外发出。修改后立即生效。按 F8 也可以快速打开或关闭这个面板。"
        };
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(subtitle);

        var scroll = new ScrollContainer
        {
            Name = "Scroll"
        };
        scroll.CustomMinimumSize = new Vector2(0, 380);
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        stack.AddChild(scroll);

        var eventList = new VBoxContainer
        {
            Name = "EventList"
        };
        scroll.AddChild(eventList);

        foreach (var eventDefinition in EventCatalog.Supported)
        {
            eventList.AddChild(CreateEventRow(eventDefinition));
        }

        var footer = new HBoxContainer
        {
            Name = "Footer"
        };
        stack.AddChild(footer);

        var closeButton = new Button
        {
            Name = "CloseButton",
            Text = "关闭"
        };
        closeButton.Pressed += HidePopup;
        footer.AddChild(closeButton);
    }

    private Control CreateEventRow(EventDefinition eventDefinition)
    {
        var row = new HBoxContainer
        {
            Name = $"{SanitizeNodeName(eventDefinition.Id)}Row"
        };
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var toggle = new CheckBox
        {
            Name = $"{SanitizeNodeName(eventDefinition.Id)}Toggle",
            ButtonPressed = _eventToggles.IsEventEnabled(eventDefinition.Id),
            Text = $"{eventDefinition.ZhHansName}  {eventDefinition.Id}"
        };
        toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        toggle.TooltipText = eventDefinition.EnName;
        toggle.Toggled += enabled =>
        {
            _eventToggles.SetEventEnabled(eventDefinition.Id, enabled);
            _saveSettings();
        };

        row.AddChild(toggle);
        return row;
    }

    private static string SanitizeNodeName(string value)
    {
        return value.Replace(".", "_", StringComparison.Ordinal);
    }
}
