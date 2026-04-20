using STS2Bridge.Runtime;

namespace STS2Bridge.Tests.Runtime;

public sealed class PlayerBlockTransitionCacheTests
{
    [Fact]
    public void Store_and_consume_should_round_trip_previous_block_for_same_creature()
    {
        PlayerBlockTransitionCache.Clear();
        var creature = new object();

        PlayerBlockTransitionCache.Store(creature, 7);

        var success = PlayerBlockTransitionCache.TryConsume(creature, out var previousBlock);

        Assert.True(success);
        Assert.Equal(7, previousBlock);
    }

    [Fact]
    public void TryConsume_should_remove_cached_value_after_read()
    {
        PlayerBlockTransitionCache.Clear();
        var creature = new object();

        PlayerBlockTransitionCache.Store(creature, 5);

        Assert.True(PlayerBlockTransitionCache.TryConsume(creature, out var firstValue));
        Assert.Equal(5, firstValue);
        Assert.False(PlayerBlockTransitionCache.TryConsume(creature, out _));
    }

    [Fact]
    public void Store_should_overwrite_previous_value_for_same_creature()
    {
        PlayerBlockTransitionCache.Clear();
        var creature = new object();

        PlayerBlockTransitionCache.Store(creature, 5);
        PlayerBlockTransitionCache.Store(creature, 9);

        var success = PlayerBlockTransitionCache.TryConsume(creature, out var previousBlock);

        Assert.True(success);
        Assert.Equal(9, previousBlock);
    }
}
