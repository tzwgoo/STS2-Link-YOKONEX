namespace STS2Bridge.Tests.Config;

public sealed class BridgeConfigTests
{
    [Fact]
    public void Default_should_match_documented_values()
    {
        var config = BridgeConfig.CreateDefault();

        Assert.True(config.Enabled);
        Assert.Equal("127.0.0.1", config.BindHost);
        Assert.Equal(15526, config.Port);
        Assert.Equal("change-me", config.AuthToken);
        Assert.Contains("player.damaged", config.EventWhitelist);
        Assert.DoesNotContain("player.hp_changed", config.EventWhitelist);
        Assert.DoesNotContain("player.block_changed", config.EventWhitelist);
        Assert.DoesNotContain("room.entered", config.EventWhitelist);
        Assert.DoesNotContain("combat.started", config.EventWhitelist);
        Assert.DoesNotContain("combat.ended", config.EventWhitelist);
        Assert.DoesNotContain("turn.started", config.EventWhitelist);
        Assert.Contains("end_turn", config.AllowedActions);
    }
}
