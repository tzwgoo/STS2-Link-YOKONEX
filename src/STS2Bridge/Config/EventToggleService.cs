namespace STS2Bridge.Config;

public sealed class EventToggleService
{
    private readonly Lock _lock = new();
    private BridgeSettings _settings;

    public EventToggleService(BridgeSettings settings)
    {
        _settings = settings;
    }

    public BridgeSettings GetSettings()
    {
        lock (_lock)
        {
            return _settings;
        }
    }

    public bool IsEventEnabled(string eventType)
    {
        lock (_lock)
        {
            return _settings.IsEventEnabled(eventType);
        }
    }

    public void SetEventEnabled(string eventType, bool enabled)
    {
        lock (_lock)
        {
            _settings = _settings.SetEventEnabled(eventType, enabled);
        }
    }

    public void UpdateSettings(Func<BridgeSettings, BridgeSettings> update)
    {
        lock (_lock)
        {
            _settings = update(_settings);
        }
    }
}
