namespace STS2Bridge.Config;

public sealed record CommandTriggerRule(
    bool Enabled,
    string EventType,
    int Threshold,
    int RepeatCount,
    string CommandId);
