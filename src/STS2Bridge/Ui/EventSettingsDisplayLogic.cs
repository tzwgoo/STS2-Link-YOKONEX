using STS2Bridge.Integration;

namespace STS2Bridge.Ui;

public static class EventSettingsDisplayLogic
{
    public static string BuildStatusText(ExternalImStatus status)
    {
        return $"连接状态: {BuildConnectionStateText(status.ConnectionState)}{(string.IsNullOrWhiteSpace(status.LastError) ? string.Empty : $" | {status.LastError}")}";
    }

    public static string BuildCommandText(string? commandId)
    {
        return string.IsNullOrWhiteSpace(commandId) ? "未配置指令" : $"-> {commandId}";
    }

    public static string BuildEventToggleText(bool enabled)
    {
        return enabled ? "已启用" : "已关闭";
    }

    public static string BuildRuleSummaryText(string eventType, int threshold, int repeatCount, bool enabled)
    {
        if (!enabled)
        {
            return "规则未启用";
        }

        return eventType switch
        {
            "player.damaged" => $"玩家掉血{threshold}滴，共触发{repeatCount}次",
            "player.block_changed" => $"玩家掉护甲{threshold}点，共触发{repeatCount}次",
            _ => $"单次变化达到{threshold}，共触发{repeatCount}次"
        };
    }

    public static string BuildRuleHintText(string eventType)
    {
        return eventType switch
        {
            "player.damaged" => "当单次掉血达到阈值时，向外部 IM 重复发送指令。",
            "player.block_changed" => "只在掉护甲时生效，获得护甲不会触发。",
            _ => "满足阈值时重复发送对应指令。"
        };
    }

    private static string BuildConnectionStateText(ExternalImConnectionState state)
    {
        return state switch
        {
            ExternalImConnectionState.Disconnected => "未连接",
            ExternalImConnectionState.Connecting => "连接中",
            ExternalImConnectionState.Connected => "已连接",
            ExternalImConnectionState.LoggingIn => "登录中",
            ExternalImConnectionState.LoggedIn => "已登录",
            ExternalImConnectionState.LoginFailed => "登录失败",
            ExternalImConnectionState.Error => "连接异常",
            _ => state.ToString()
        };
    }
}
