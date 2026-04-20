using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.Logging;
using STS2Bridge.Ui;

namespace STS2Bridge.Integration;

public sealed class IMCommandBridgeService : IDisposable
{
    private readonly IExternalImClient _client;
    private readonly Func<BridgeSettings> _getSettings;
    private readonly IDisposable _subscription;

    public IMCommandBridgeService(GameEventBus eventBus, IExternalImClient client, Func<BridgeSettings> getSettings)
    {
        _client = client;
        _getSettings = getSettings;
        _subscription = eventBus.Subscribe(HandleEvent);
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }

    private void HandleEvent(GameEvent gameEvent)
    {
        var settings = _getSettings();
        var userId = _client.Status.CurrentUserId;

        if (!_client.Status.IsLoggedIn || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var queued = false;
        foreach (var rule in settings.CommandTriggerRules)
        {
            var repeatCount = CommandTriggerRuleEvaluator.GetRepeatCount(gameEvent, rule);
            if (repeatCount > 0)
            {
                ModLog.Info($"阈值规则命中: {EventSettingsDisplayLogic.BuildRuleSummaryText(rule.EventType, rule.Threshold, rule.RepeatCount, rule.Enabled)}");
            }

            for (var index = 0; index < repeatCount; index++)
            {
                queued = true;
                _ = _client.SendCommandAsync(userId, rule.CommandId);
            }
        }

        if (queued)
        {
            return;
        }

        var commandId = settings.GetCommandId(gameEvent.Type);
        if (!string.IsNullOrWhiteSpace(commandId))
        {
            _ = _client.SendCommandAsync(userId, commandId);
        }
    }
}
