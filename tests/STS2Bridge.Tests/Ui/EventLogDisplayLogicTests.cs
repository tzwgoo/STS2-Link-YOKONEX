using STS2Bridge.Ui;

namespace STS2Bridge.Tests.Ui;

public sealed class EventLogDisplayLogicTests
{
    [Fact]
    public void BuildTitle_should_prefer_catalog_chinese_name()
    {
        var gameEvent = new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 1, "Combat", new { amount = 6 });

        Assert.Equal("玩家受伤", EventLogDisplayLogic.BuildTitle(gameEvent));
    }

    [Fact]
    public void BuildSummary_should_render_player_damage_details()
    {
        var gameEvent = new GameEvent("evt-1", EventTypes.PlayerDamaged, "run-1", 2, "Combat", new { amount = 6, currentHp = 30, maxHp = 80 });

        var summary = EventLogDisplayLogic.BuildSummary(gameEvent);

        Assert.Contains("掉血 6", summary);
        Assert.Contains("30/80", summary);
    }

    [Fact]
    public void BuildSummary_should_render_player_energy_details()
    {
        var gameEvent = new GameEvent("evt-1", EventTypes.PlayerEnergyChanged, "run-1", 3, "Combat", new { delta = -1, energy = 2, maxEnergy = 3 });

        var summary = EventLogDisplayLogic.BuildSummary(gameEvent);

        Assert.Contains("能量变化 -1", summary);
        Assert.Contains("2/3", summary);
    }

    [Fact]
    public void BuildSummary_should_fallback_to_json_for_unknown_payload()
    {
        var gameEvent = new GameEvent("evt-1", "custom.event", "run-1", 1, "Event", new { foo = "bar" });

        var summary = EventLogDisplayLogic.BuildSummary(gameEvent);

        Assert.Contains("foo", summary);
        Assert.Contains("bar", summary);
    }
}
