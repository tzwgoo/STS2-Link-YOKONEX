using STS2Bridge.Config;

namespace STS2Bridge.Tests.Config;

public sealed class EventCommandCatalogTests
{
    [Fact]
    public void DefaultMap_should_cover_all_supported_events()
    {
        var missing = EventCatalog.SupportedIds
            .Where(eventId => !EventCommandCatalog.DefaultMap.ContainsKey(eventId))
            .ToArray();

        Assert.Empty(missing);
    }
}
