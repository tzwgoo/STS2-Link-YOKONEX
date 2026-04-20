using STS2Bridge.Events;
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
    public void BuildSummary_should_render_orb_passive_details()
    {
        var gameEvent = new GameEvent(
            "evt-2",
            EventTypes.OrbPassiveTriggered,
            "run-1",
            8,
            "Combat",
            new { orbType = "frost", amountKind = "block", amount = 2, ownerId = "7" });

        var summary = EventLogDisplayLogic.BuildSummary(gameEvent);

        Assert.Contains("冰霜球被动", summary);
        Assert.Contains("格挡 2", summary);
        Assert.Contains("拥有者 7", summary);
    }

    [Fact]
    public void BuildSummary_should_render_orb_evoked_details()
    {
        var gameEvent = new GameEvent(
            "evt-3",
            EventTypes.OrbEvoked,
            "run-1",
            8,
            "Combat",
            new { orbType = "plasma", amountKind = "energy", amount = 2, ownerId = "unknown" });

        var summary = EventLogDisplayLogic.BuildSummary(gameEvent);

        Assert.Contains("等离子球激发", summary);
        Assert.Contains("能量 2", summary);
        Assert.DoesNotContain("拥有者", summary);
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
