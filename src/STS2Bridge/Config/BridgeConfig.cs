namespace STS2Bridge.Config;

public sealed record BridgeConfig
{
    public bool Enabled { get; init; } = true;

    public string BindHost { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 15526;

    public string AuthToken { get; init; } = "change-me";

    public bool EnableHttp { get; init; } = true;

    public bool EnableWebSocket { get; init; } = true;

    public bool EnableEventReplay { get; init; } = true;

    public bool EnableDebugLog { get; init; } = true;

    public IReadOnlyList<string> EventWhitelist { get; init; } =
    [
        "room.entered",
        "combat.started",
        "combat.ended",
        "turn.started",
        "player.hp_changed",
        "player.damaged",
        "player.healed",
        "player.energy_changed",
        "player.block_changed",
        "player.block_broken",
        "player.block_cleared",
        "player.died",
        "card.upgraded",
        "item.purchased",
        "reward.opened",
        "reward.selected",
        "event.option_selected"
    ];

    public IReadOnlyList<string> AllowedActions { get; init; } =
    [
        "play_card",
        "end_turn",
        "choose_reward",
        "choose_event_option",
        "proceed"
    ];

    public static BridgeConfig CreateDefault() => new();
}
