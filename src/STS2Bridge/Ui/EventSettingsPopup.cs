using Godot;
using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.Integration;

namespace STS2Bridge.Ui;

public sealed class EventSettingsPopup : PanelContainer
{
    public const string PopupNodeName = "STS2LinkYOKONEXEventSettingsPopup";

    private static readonly RuleEditorDefinition[] RuleDefinitions =
    [
        new("PlayerDamagedRule", "单次掉血规则", EventTypes.PlayerDamaged, "player_hurt"),
        new("PlayerBlockLossRule", "单次掉甲规则", EventTypes.PlayerBlockChanged, "player_block_loss")
    ];

    private readonly EventToggleService _eventToggles;
    private readonly Func<ExternalImStatus> _getImStatus;
    private readonly Action<BridgeSettings> _updateSettings;
    private readonly Func<Task> _loginAsync;
    private readonly Func<Task> _logoutAsync;
    private readonly Action _saveSettings;
    private readonly Dictionary<string, RuleEditorControls> _ruleEditors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Label> _eventCommandLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CheckButton> _eventTogglesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _eventToggleStatusLabels = new(StringComparer.OrdinalIgnoreCase);

    private LineEdit _uidInput = null!;
    private LineEdit _tokenInput = null!;
    private Label _statusLabel = null!;
    private Label _eventCountLabel = null!;
    private ScrollContainer _leftScroll = null!;
    private ScrollContainer _rightScroll = null!;

    public EventSettingsPopup(
        EventToggleService eventToggles,
        Func<ExternalImStatus> getImStatus,
        Action<BridgeSettings> updateSettings,
        Func<Task> loginAsync,
        Func<Task> logoutAsync,
        Action saveSettings)
    {
        Name = PopupNodeName;
        _eventToggles = eventToggles;
        _getImStatus = getImStatus;
        _updateSettings = updateSettings;
        _loginAsync = loginAsync;
        _logoutAsync = logoutAsync;
        _saveSettings = saveSettings;
        Visible = false;
        BuildUi();
    }

    public override void _Ready()
    {
        base._Ready();
        RefreshAll();
        if (GetViewport() is { } viewport)
        {
            viewport.SizeChanged += ApplyViewportBounds;
        }
    }

    public override void _ExitTree()
    {
        if (GetViewport() is { } viewport)
        {
            viewport.SizeChanged -= ApplyViewportBounds;
        }

        base._ExitTree();
    }

    public void ToggleVisibility()
    {
        Visible = !Visible;
        if (Visible)
        {
            RequestRefreshAll();
        }
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
        layout.AddChild(BuildContent());
        layout.AddChild(BuildFooter());
    }

    private Control BuildHeader()
    {
        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("141b24"), new Color("344154"), 20, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        header.AddChild(margin);

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 6);
        margin.AddChild(stack);

        stack.AddChild(CreateMutedLabel("联动控制台", 13, new Color("f5b65a")));
        stack.AddChild(CreateTitleLabel("STS2-Link-YOKONEX Events", 26));

        var subtitle = CreateMutedLabel(
            "在这里统一管理 IM 登录、阈值规则和事件开关。修改会立即生效，按 F8 也能快速打开或关闭这个面板。",
            14,
            new Color("c0ccda"));
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(subtitle);

        return header;
    }

    private Control BuildContent()
    {
        var content = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 16);

        _leftScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _leftScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        content.AddChild(_leftScroll);

        var leftColumn = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        leftColumn.AddThemeConstantOverride("separation", 14);
        leftColumn.AddChild(BuildImCard());
        leftColumn.AddChild(BuildRulesCard());
        _leftScroll.AddChild(leftColumn);

        var rightColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        rightColumn.AddThemeConstantOverride("separation", 14);
        rightColumn.AddChild(BuildEventsCard());
        content.AddChild(rightColumn);

        return content;
    }

    private Control BuildImCard()
    {
        var card = CreateCard("IM 联动", "填写 UID 与 Token 后登录，触发事件时会按映射自动发送指令。");
        var body = card.Body;
        body.AddThemeConstantOverride("separation", 10);

        var serviceBox = CreateInfoBox();
        serviceBox.Body.AddChild(CreateKeyValueRow("服务地址", _eventToggles.GetSettings().ImWebSocketUrl));
        body.AddChild(serviceBox.Panel);

        _uidInput = CreateLineInput("UID", _eventToggles.GetSettings().ImUid, false);
        _uidInput.TextChanged += _ => PersistImSettings();
        body.AddChild(CreateLabeledField("UID", "填写外部 IM 用户 UID。", _uidInput));

        _tokenInput = CreateLineInput("Token", _eventToggles.GetSettings().ImToken, true);
        _tokenInput.TextChanged += _ => PersistImSettings();
        body.AddChild(CreateLabeledField("Token", "Token 会写入本地配置文件并在重新打开游戏时恢复。", _tokenInput));

        _statusLabel = CreateStatusPill(string.Empty);
        body.AddChild(_statusLabel);

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 10);

        var loginButton = CreateActionButton("登录 IM", new Color("ef9a34"), new Color("f7bb59"));
        loginButton.Pressed += async () =>
        {
            PersistImSettings();
            await _loginAsync();
            RefreshImStatus();
        };
        buttonRow.AddChild(loginButton);

        var logoutButton = CreateActionButton("登出 IM", new Color("b24141"), new Color("da6666"));
        logoutButton.Pressed += async () =>
        {
            await _logoutAsync();
            RefreshImStatus();
        };
        buttonRow.AddChild(logoutButton);

        body.AddChild(buttonRow);

        return card.Panel;
    }

    private Control BuildRulesCard()
    {
        var card = CreateCard("阈值规则", "按单次变化值判断。满足阈值后，会把对应指令连续发送指定次数。");
        var body = card.Body;
        body.AddThemeConstantOverride("separation", 10);

        foreach (var definition in RuleDefinitions)
        {
            body.AddChild(CreateRuleCard(definition));
        }

        return card.Panel;
    }

    private Control BuildEventsCard()
    {
        var card = CreateCard("事件开关", "右侧开关控制事件是否继续向桥接层发出。关闭后，不会发送到本地 WS，也不会触发 IM 指令。");
        card.Panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        var body = card.Body;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 10);

        _eventCountLabel = CreateMutedLabel(string.Empty, 13, new Color("9db0c7"));
        body.AddChild(_eventCountLabel);

        _rightScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _rightScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        body.AddChild(_rightScroll);

        var list = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        list.AddThemeConstantOverride("separation", 10);
        _rightScroll.AddChild(list);

        foreach (var definition in EventCatalog.Supported)
        {
            list.AddChild(CreateEventRow(definition));
        }

        return card.Panel;
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

        var note = CreateMutedLabel("提示：阈值规则优先于普通映射。命中规则时，会按配置的次数重复发送 commandId。", 13, new Color("9db0c7"));
        note.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(note);

        var closeButton = CreateSecondaryButton("关闭");
        closeButton.Pressed += HidePopup;
        row.AddChild(closeButton);

        var saveAndCloseButton = CreateActionButton("保存并关闭", new Color("ef9a34"), new Color("f7bb59"));
        saveAndCloseButton.Pressed += () =>
        {
            _saveSettings();
            HidePopup();
        };
        row.AddChild(saveAndCloseButton);

        return footer;
    }

    private Control CreateRuleCard(RuleEditorDefinition definition)
    {
        var settings = _eventToggles.GetSettings();
        var rule = settings.GetTriggerRule(definition.EventType) ?? new CommandTriggerRule(
            Enabled: false,
            EventType: definition.EventType,
            Threshold: 5,
            RepeatCount: 1,
            CommandId: definition.DefaultCommandId);

        var card = new PanelContainer
        {
            Name = definition.NodeName
        };
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("18212d"), new Color("2b3a4d"), 18, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        card.AddChild(margin);

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 8);
        margin.AddChild(stack);

        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 10);
        stack.AddChild(titleRow);

        var titleStack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleStack.AddThemeConstantOverride("separation", 4);
        titleRow.AddChild(titleStack);

        titleStack.AddChild(CreateSectionLabel(definition.DisplayName, 18, new Color("f4f7fb")));
        titleStack.AddChild(CreateMutedLabel(definition.EventType, 12, new Color("8ba0bb")));

        var enabledToggle = new CheckBox
        {
            Name = "EnabledToggle",
            Text = "启用",
            ButtonPressed = rule.Enabled,
            CustomMinimumSize = new Vector2(70, 0)
        };
        enabledToggle.Toggled += _ => PersistRuleSettings(definition);
        titleRow.AddChild(enabledToggle);

        var valuesRow = new HBoxContainer();
        valuesRow.AddThemeConstantOverride("separation", 8);
        stack.AddChild(valuesRow);

        var thresholdInput = CreateSpinInput(rule.Threshold, 1, 999);
        thresholdInput.Name = "ThresholdInput";
        thresholdInput.ValueChanged += _ => PersistRuleSettings(definition);
        valuesRow.AddChild(CreateCompactField("触发阈值", thresholdInput));

        var repeatInput = CreateSpinInput(rule.RepeatCount, 1, 20);
        repeatInput.Name = "RepeatInput";
        repeatInput.ValueChanged += _ => PersistRuleSettings(definition);
        valuesRow.AddChild(CreateCompactField("重复次数", repeatInput));

        var commandInput = CreateLineInput("commandId", rule.CommandId, false);
        commandInput.Name = "CommandInput";
        commandInput.TextChanged += _ => PersistRuleSettings(definition);
        commandInput.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        stack.AddChild(CreateLabeledField("指令 ID", EventSettingsDisplayLogic.BuildRuleHintText(definition.EventType), commandInput));

        var summaryLabel = CreateMutedLabel(string.Empty, 14, new Color("f6c56c"));
        summaryLabel.Name = "SummaryLabel";
        summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(summaryLabel);

        _ruleEditors[definition.EventType] = new RuleEditorControls(enabledToggle, thresholdInput, repeatInput, commandInput, summaryLabel);
        UpdateRuleSummary(definition.EventType);

        return card;
    }

    private Control CreateEventRow(EventDefinition eventDefinition)
    {
        var card = new PanelContainer
        {
            Name = $"{SanitizeNodeName(eventDefinition.Id)}Row"
        };
        card.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("17202b"), new Color("2b394d"), 16, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        card.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        margin.AddChild(row);

        var infoColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        infoColumn.AddThemeConstantOverride("separation", 3);
        row.AddChild(infoColumn);

        infoColumn.AddChild(CreateSectionLabel(eventDefinition.ZhHansName, 16, new Color("f4f7fb")));
        infoColumn.AddChild(CreateMutedLabel(eventDefinition.Id, 12, new Color("8ba0bb")));

        var commandLabel = CreateCommandPill(BuildCommandText(eventDefinition.Id));
        commandLabel.Name = "CommandLabel";
        infoColumn.AddChild(commandLabel);
        _eventCommandLabels[eventDefinition.Id] = commandLabel;

        var toggleColumn = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        toggleColumn.AddThemeConstantOverride("separation", 6);
        row.AddChild(toggleColumn);

        var statusLabel = CreateToggleStatePill(_eventToggles.IsEventEnabled(eventDefinition.Id));
        statusLabel.Name = $"{SanitizeNodeName(eventDefinition.Id)}Status";
        toggleColumn.AddChild(statusLabel);
        _eventToggleStatusLabels[eventDefinition.Id] = statusLabel;

        var toggle = new CheckButton
        {
            Name = $"{SanitizeNodeName(eventDefinition.Id)}Toggle",
            Text = string.Empty,
            ButtonPressed = _eventToggles.IsEventEnabled(eventDefinition.Id),
            TooltipText = eventDefinition.EnName,
            CustomMinimumSize = new Vector2(78, 30),
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        toggle.Alignment = HorizontalAlignment.Center;
        toggle.AddThemeColorOverride("font_color", Colors.Transparent);
        toggle.AddThemeColorOverride("font_hover_color", Colors.Transparent);
        toggle.AddThemeColorOverride("font_pressed_color", Colors.Transparent);
        toggle.AddThemeColorOverride("font_focus_color", Colors.Transparent);
        toggle.Toggled += enabled =>
        {
            _eventToggles.SetEventEnabled(eventDefinition.Id, enabled);
            _saveSettings();
            RefreshEventListStatus();
        };
        toggleColumn.AddChild(toggle);
        _eventTogglesById[eventDefinition.Id] = toggle;

        return card;
    }

    private void PersistImSettings()
    {
        var settings = _eventToggles.GetSettings().SetImCredentials(_uidInput.Text, _tokenInput.Text);
        _updateSettings(settings);
        _saveSettings();
        RefreshImStatus();
    }

    private void PersistRuleSettings(RuleEditorDefinition definition)
    {
        var editor = _ruleEditors[definition.EventType];
        var rule = new CommandTriggerRule(
            Enabled: editor.EnabledToggle.ButtonPressed,
            EventType: definition.EventType,
            Threshold: (int)Math.Round(editor.ThresholdInput.Value),
            RepeatCount: (int)Math.Round(editor.RepeatInput.Value),
            CommandId: editor.CommandInput.Text.Trim());

        var settings = _eventToggles.GetSettings().AddTriggerRule(rule);
        _updateSettings(settings);
        _saveSettings();
        UpdateRuleSummary(definition.EventType);
    }

    private void RefreshAll()
    {
        ApplyViewportBounds();
        SyncInputsFromSettings();
        RefreshImStatus();
        RefreshRuleEditors();
        RefreshEventListStatus();
    }

    private void ApplyViewportBounds()
    {
        var viewport = GetViewport();
        var viewportSize = viewport?.GetVisibleRect().Size ?? Vector2.Zero;
        if (!EventSettingsLayoutLogic.CanApplyViewportBounds(IsInsideTree(), viewportSize))
        {
            return;
        }

        var rect = EventSettingsLayoutLogic.CalculatePopupRect(viewportSize);
        OffsetLeft = rect.Position.X;
        OffsetTop = rect.Position.Y;
        OffsetRight = rect.End.X;
        OffsetBottom = rect.End.Y;

        var (leftWidth, _) = EventSettingsLayoutLogic.CalculateColumnWidths(rect.Size.X);

        if (_leftScroll is not null)
        {
            _leftScroll.CustomMinimumSize = new Vector2(leftWidth, EventSettingsLayoutLogic.CalculateSideColumnMinHeight(rect.Size.Y));
        }

        if (_rightScroll is not null)
        {
            _rightScroll.CustomMinimumSize = new Vector2(0, EventSettingsLayoutLogic.CalculateEventListMinHeight(rect.Size.Y));
        }
    }

    private void RequestRefreshAll()
    {
        if (IsInsideTree())
        {
            RefreshAll();
            return;
        }

        CallDeferred(nameof(RefreshAllDeferred));
    }

    private void RefreshAllDeferred()
    {
        RefreshAll();
    }

    private void SyncInputsFromSettings()
    {
        var settings = _eventToggles.GetSettings();
        if (_uidInput is not null)
        {
            _uidInput.Text = settings.ImUid;
        }

        if (_tokenInput is not null)
        {
            _tokenInput.Text = settings.ImToken;
        }
    }

    private void RefreshImStatus()
    {
        if (_statusLabel is null)
        {
            return;
        }

        _statusLabel.Text = BuildStatusText();
    }

    private void RefreshRuleEditors()
    {
        var settings = _eventToggles.GetSettings();

        foreach (var definition in RuleDefinitions)
        {
            if (!_ruleEditors.TryGetValue(definition.EventType, out var editor))
            {
                continue;
            }

            var rule = settings.GetTriggerRule(definition.EventType) ?? new CommandTriggerRule(
                Enabled: false,
                EventType: definition.EventType,
                Threshold: 5,
                RepeatCount: 1,
                CommandId: definition.DefaultCommandId);

            editor.EnabledToggle.ButtonPressed = rule.Enabled;
            editor.ThresholdInput.Value = rule.Threshold;
            editor.RepeatInput.Value = rule.RepeatCount;
            editor.CommandInput.Text = rule.CommandId;
            editor.SummaryLabel.Text = EventSettingsDisplayLogic.BuildRuleSummaryText(rule.EventType, rule.Threshold, rule.RepeatCount, rule.Enabled);
        }
    }

    private void RefreshEventListStatus()
    {
        var enabledCount = 0;
        foreach (var definition in EventCatalog.Supported)
        {
            var isEnabled = _eventToggles.IsEventEnabled(definition.Id);
            if (isEnabled)
            {
                enabledCount++;
            }

            if (_eventTogglesById.TryGetValue(definition.Id, out var toggle))
            {
                toggle.ButtonPressed = isEnabled;
            }

            if (_eventToggleStatusLabels.TryGetValue(definition.Id, out var statusLabel))
            {
                statusLabel.Text = EventSettingsDisplayLogic.BuildEventToggleText(isEnabled);
                statusLabel.AddThemeStyleboxOverride("normal", CreateToggleStateStyle(isEnabled));
            }

            if (_eventCommandLabels.TryGetValue(definition.Id, out var label))
            {
                label.Text = BuildCommandText(definition.Id);
            }
        }

        if (_eventCountLabel is not null)
        {
            _eventCountLabel.Text = $"当前已启用 {enabledCount} / {EventCatalog.Supported.Count} 个事件";
        }
    }

    private void UpdateRuleSummary(string eventType)
    {
        if (!_ruleEditors.TryGetValue(eventType, out var editor))
        {
            return;
        }

        editor.SummaryLabel.Text = EventSettingsDisplayLogic.BuildRuleSummaryText(
            eventType,
            (int)Math.Round(editor.ThresholdInput.Value),
            (int)Math.Round(editor.RepeatInput.Value),
            editor.EnabledToggle.ButtonPressed);
    }

    private string BuildStatusText()
    {
        return EventSettingsDisplayLogic.BuildStatusText(_getImStatus());
    }

    private string BuildCommandText(string eventId)
    {
        return EventSettingsDisplayLogic.BuildCommandText(_eventToggles.GetSettings().GetCommandId(eventId));
    }

    private static CardParts CreateCard(string title, string description)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("131a23"), new Color("2f3a49"), 20, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        var stack = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 10);
        margin.AddChild(stack);

        stack.AddChild(CreateSectionLabel(title, 20, new Color("f4f7fb")));

        var descriptionLabel = CreateMutedLabel(description, 13, new Color("9db0c7"));
        descriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(descriptionLabel);

        return new CardParts(panel, stack);
    }

    private static CardParts CreateInfoBox()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color("18212d"), new Color("2b3a4d"), 14, 1));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var stack = new VBoxContainer();
        margin.AddChild(stack);

        return new CardParts(panel, stack);
    }

    private static Control CreateKeyValueRow(string key, string value)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var keyLabel = CreateMutedLabel(key, 13, new Color("8ba0bb"));
        keyLabel.CustomMinimumSize = new Vector2(72, 0);
        row.AddChild(keyLabel);

        var valueLabel = CreateMutedLabel(value, 13, new Color("f4f7fb"));
        valueLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        valueLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(valueLabel);

        return row;
    }

    private static Control CreateLabeledField(string title, string hint, Control input)
    {
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 4);

        stack.AddChild(CreateSectionLabel(title, 15, new Color("ecf1f7")));

        var hintLabel = CreateMutedLabel(hint, 12, new Color("8ba0bb"));
        hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(hintLabel);
        stack.AddChild(input);

        return stack;
    }

    private static Control CreateCompactField(string title, Control input)
    {
        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stack.AddThemeConstantOverride("separation", 4);
        stack.AddChild(CreateMutedLabel(title, 12, new Color("8ba0bb")));
        stack.AddChild(input);
        return stack;
    }

    private static LineEdit CreateLineInput(string placeholder, string value, bool secret)
    {
        var input = new LineEdit
        {
            PlaceholderText = placeholder,
            Text = value,
            Secret = secret,
            CustomMinimumSize = new Vector2(0, 36)
        };
        input.AddThemeStyleboxOverride("normal", CreateInputStyle(new Color("0f151d"), new Color("314054")));
        input.AddThemeStyleboxOverride("focus", CreateInputStyle(new Color("121a24"), new Color("f0a33d"), 2));
        return input;
    }

    private static SpinBox CreateSpinInput(int value, int minValue, int maxValue)
    {
        var input = new SpinBox
        {
            MinValue = minValue,
            MaxValue = maxValue,
            Step = 1,
            Value = value,
            CustomMinimumSize = new Vector2(0, 36)
        };
        input.AddThemeStyleboxOverride("normal", CreateInputStyle(new Color("0f151d"), new Color("314054")));
        input.AddThemeStyleboxOverride("focus", CreateInputStyle(new Color("121a24"), new Color("f0a33d"), 2));
        input.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return input;
    }

    private static Button CreateActionButton(string text, Color baseColor, Color borderColor)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(120, 40)
        };
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(baseColor, borderColor));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f)));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(baseColor.Darkened(0.08f), borderColor.Darkened(0.08f)));
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_focus_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        return button;
    }

    private static Button CreateSecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(92, 40)
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
        var label = new Label
        {
            Text = text
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color("f7f9fc"));
        return label;
    }

    private static Label CreateSectionLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label CreateMutedLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label CreateStatusPill(string text)
    {
        var label = CreateMutedLabel(text, 13, new Color("eef3f9"));
        label.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color("203041"), new Color("46607d"), 14, 1));
        label.AddThemeConstantOverride("line_spacing", 2);
        return label;
    }

    private static Label CreateCommandPill(string text)
    {
        var label = CreateMutedLabel(text, 12, new Color("f7d794"));
        label.AddThemeStyleboxOverride("normal", CreatePanelStyle(new Color("2b2114"), new Color("876439"), 14, 1));
        return label;
    }

    private static Label CreateToggleStatePill(bool enabled)
    {
        var label = CreateMutedLabel(EventSettingsDisplayLogic.BuildEventToggleText(enabled), 12, new Color("eef3f9"));
        label.AddThemeStyleboxOverride("normal", CreateToggleStateStyle(enabled));
        return label;
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

    private static StyleBoxFlat CreateInputStyle(Color background, Color border, int borderWidth = 1)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
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

    private static StyleBoxFlat CreateToggleStateStyle(bool enabled)
    {
        return enabled
            ? CreatePanelStyle(new Color("183926"), new Color("39a36b"), 14, 1)
            : CreatePanelStyle(new Color("312028"), new Color("8f4a61"), 14, 1);
    }

    private static string SanitizeNodeName(string value)
    {
        return value.Replace(".", "_", StringComparison.Ordinal);
    }

    private sealed record RuleEditorDefinition(string NodeName, string DisplayName, string EventType, string DefaultCommandId);

    private sealed record RuleEditorControls(
        CheckBox EnabledToggle,
        SpinBox ThresholdInput,
        SpinBox RepeatInput,
        LineEdit CommandInput,
        Label SummaryLabel);

    private sealed record CardParts(PanelContainer Panel, VBoxContainer Body);
}
