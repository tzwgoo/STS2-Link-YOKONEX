using STS2Bridge.Integration;
using STS2Bridge.Ui;

namespace STS2Bridge.Tests.Ui;

public sealed class EventSettingsDisplayLogicTests
{
    [Fact]
    public void BuildStatusText_should_include_connection_state_and_error()
    {
        var text = EventSettingsDisplayLogic.BuildStatusText(new ExternalImStatus
        {
            ConnectionState = ExternalImConnectionState.LoginFailed,
            LastError = "获取 IM 签名失败"
        });

        Assert.Contains("登录失败", text);
        Assert.Contains("获取 IM 签名失败", text);
    }

    [Fact]
    public void BuildStatusText_should_render_connected_state_in_chinese()
    {
        var text = EventSettingsDisplayLogic.BuildStatusText(new ExternalImStatus
        {
            ConnectionState = ExternalImConnectionState.Connected
        });

        Assert.Equal("连接状态: 已连接", text);
    }

    [Fact]
    public void BuildCommandText_should_render_arrow_when_mapping_exists()
    {
        Assert.Equal("-> player_hurt", EventSettingsDisplayLogic.BuildCommandText("player_hurt"));
    }

    [Fact]
    public void BuildCommandText_should_render_default_text_when_mapping_is_missing()
    {
        Assert.Equal("未配置指令", EventSettingsDisplayLogic.BuildCommandText(null));
    }

    [Fact]
    public void BuildEventToggleText_should_render_enabled_text()
    {
        Assert.Equal("已启用", EventSettingsDisplayLogic.BuildEventToggleText(true));
    }

    [Fact]
    public void BuildEventToggleText_should_render_disabled_text()
    {
        Assert.Equal("已关闭", EventSettingsDisplayLogic.BuildEventToggleText(false));
    }

    [Fact]
    public void BuildRuleSummaryText_should_describe_damage_rule_in_chinese()
    {
        var text = EventSettingsDisplayLogic.BuildRuleSummaryText("player.damaged", threshold: 5, repeatCount: 3, enabled: true);

        Assert.Equal("玩家掉血5滴，共触发3次", text);
    }

    [Fact]
    public void BuildRuleSummaryText_should_describe_block_loss_rule_in_chinese()
    {
        var text = EventSettingsDisplayLogic.BuildRuleSummaryText("player.block_changed", threshold: 8, repeatCount: 2, enabled: true);

        Assert.Equal("玩家掉护甲8点，共触发2次", text);
    }

    [Fact]
    public void BuildRuleSummaryText_should_return_disabled_text_when_rule_is_off()
    {
        var text = EventSettingsDisplayLogic.BuildRuleSummaryText("player.damaged", threshold: 5, repeatCount: 3, enabled: false);

        Assert.Equal("规则未启用", text);
    }
}
