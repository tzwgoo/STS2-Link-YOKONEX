using STS2Bridge.Config;
using STS2Bridge.Events;

namespace STS2Bridge.Tests.Config;

public sealed class EventCatalogTests
{
    [Fact]
    public void Supported_should_include_currently_wired_events()
    {
        var ids = EventCatalog.Supported.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(EventTypes.CombatStarted, ids);
        Assert.Contains(EventTypes.TurnStarted, ids);
        Assert.Contains(EventTypes.CombatEnded, ids);
        Assert.Contains(EventTypes.CardPlayed, ids);
        Assert.Contains(EventTypes.PlayerHpChanged, ids);
        Assert.Contains(EventTypes.PlayerDamaged, ids);
        Assert.Contains(EventTypes.PlayerHealed, ids);
        Assert.Contains(EventTypes.PlayerBlockChanged, ids);
        Assert.Contains(EventTypes.PlayerBlockBroken, ids);
        Assert.Contains(EventTypes.PlayerBlockCleared, ids);
        Assert.Contains(EventTypes.PlayerDied, ids);
    }
}
