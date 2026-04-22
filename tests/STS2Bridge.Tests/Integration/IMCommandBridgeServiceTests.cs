using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.Integration;
using STS2Bridge.Logging;

namespace STS2Bridge.Tests.Integration;

public sealed class IMCommandBridgeServiceTests
{
    [Fact]
    public async Task Publish_should_send_command_when_event_has_mapping_and_client_is_logged_in()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault();
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 6 }));
        await client.WaitForDrainAsync();

        Assert.Single(client.SentCommands);
        Assert.Equal("123456", client.SentCommands[0].UserId);
        Assert.Equal("player_hurt", client.SentCommands[0].CommandId);
    }

    [Fact]
    public async Task Publish_should_not_send_when_event_is_disabled()
    {
        var settings = BridgeSettings.CreateDefault().SetEventEnabled(EventTypes.PlayerDamaged, false);
        var bus = new GameEventBus(isEventEnabled: settings.IsEventEnabled);
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 6 }));
        await client.WaitForDrainAsync();

        Assert.Empty(client.SentCommands);
    }

    [Fact]
    public async Task Publish_should_not_send_when_client_is_not_logged_in()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault();
        var client = new FakeExternalImClient(loggedInUserId: null);
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 6 }));
        await client.WaitForDrainAsync();

        Assert.Empty(client.SentCommands);
    }

    [Fact]
    public async Task Publish_should_not_send_when_mapping_is_missing()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault().SetCommandMapping(EventTypes.PlayerDamaged, string.Empty);
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 6 }));
        await client.WaitForDrainAsync();

        Assert.Empty(client.SentCommands);
    }

    [Fact]
    public async Task Publish_should_repeat_send_when_damage_rule_threshold_is_met()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault()
            .SetCommandMapping(EventTypes.PlayerDamaged, string.Empty)
            .AddTriggerRule(new CommandTriggerRule(
                Enabled: true,
                EventType: EventTypes.PlayerDamaged,
                Threshold: 5,
                RepeatCount: 3,
                CommandId: "player_hurt"));
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 6 }));
        await client.WaitForDrainAsync();

        Assert.Equal(3, client.SentCommands.Count);
        Assert.All(client.SentCommands, item => Assert.Equal("player_hurt", item.CommandId));
    }

    [Fact]
    public async Task Publish_should_not_repeat_send_when_damage_rule_threshold_is_not_met()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault()
            .SetCommandMapping(EventTypes.PlayerDamaged, string.Empty)
            .AddTriggerRule(new CommandTriggerRule(
                Enabled: true,
                EventType: EventTypes.PlayerDamaged,
                Threshold: 5,
                RepeatCount: 3,
                CommandId: "player_hurt"));
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 4 }));
        await client.WaitForDrainAsync();

        Assert.Empty(client.SentCommands);
    }

    [Fact]
    public async Task Publish_should_repeat_send_when_block_loss_rule_threshold_is_met()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault()
            .SetCommandMapping(EventTypes.PlayerBlockBroken, string.Empty)
            .AddTriggerRule(new CommandTriggerRule(
                Enabled: true,
                EventType: EventTypes.PlayerBlockBroken,
                Threshold: 8,
                RepeatCount: 2,
                CommandId: "player_block_loss"));
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerBlockBroken, "run-1", 1, "MonsterRoom", new { previousBlock = 9, block = 0 }));
        await client.WaitForDrainAsync();

        Assert.Equal(2, client.SentCommands.Count);
        Assert.All(client.SentCommands, item => Assert.Equal("player_block_loss", item.CommandId));
    }

    [Fact]
    public async Task Publish_should_ignore_block_gain_for_block_loss_rule()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault()
            .SetCommandMapping(EventTypes.PlayerBlockBroken, string.Empty)
            .AddTriggerRule(new CommandTriggerRule(
                Enabled: true,
                EventType: EventTypes.PlayerBlockBroken,
                Threshold: 8,
                RepeatCount: 2,
                CommandId: "player_block_loss"));
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        using var bridge = new IMCommandBridgeService(bus, client, () => settings);

        bus.Publish(new GameEvent("evt-1", EventTypes.PlayerBlockBroken, "run-1", 1, "MonsterRoom", new { previousBlock = 7, block = 0 }));
        await client.WaitForDrainAsync();

        Assert.Empty(client.SentCommands);
    }

    [Fact]
    public async Task Publish_should_write_chinese_rule_log_when_damage_rule_is_triggered()
    {
        var bus = new GameEventBus();
        var settings = BridgeSettings.CreateDefault()
            .SetCommandMapping(EventTypes.PlayerDamaged, string.Empty)
            .AddTriggerRule(new CommandTriggerRule(
                Enabled: true,
                EventType: EventTypes.PlayerDamaged,
                Threshold: 5,
                RepeatCount: 3,
                CommandId: "player_hurt"));
        var client = new FakeExternalImClient(loggedInUserId: "123456");
        string? captured = null;
        ModLog.SetSink(line => captured = line);

        try
        {
            using var bridge = new IMCommandBridgeService(bus, client, () => settings);

            bus.Publish(new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "MonsterRoom", new { amount = 6 }));
            await client.WaitForDrainAsync();
        }
        finally
        {
            ModLog.SetSink(null);
        }

        Assert.NotNull(captured);
        Assert.Contains("玩家掉血5滴，共触发3次", captured);
    }

    private sealed class FakeExternalImClient(string? loggedInUserId) : IExternalImClient
    {
        public ExternalImStatus Status { get; } = new()
        {
            ConnectionState = loggedInUserId is null ? ExternalImConnectionState.Connected : ExternalImConnectionState.LoggedIn,
            CurrentUserId = loggedInUserId
        };

        public List<SentCommand> SentCommands { get; } = [];

        public Task ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoginAsync(string uid, string token, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendCommandAsync(string userId, string commandId, CancellationToken cancellationToken = default)
        {
            SentCommands.Add(new SentCommand(userId, commandId));
            return Task.CompletedTask;
        }

        public Task WaitForDrainAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed record SentCommand(string UserId, string CommandId);
}
