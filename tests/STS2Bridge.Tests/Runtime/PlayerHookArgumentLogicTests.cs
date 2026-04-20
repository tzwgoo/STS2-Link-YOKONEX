using STS2Bridge.Runtime;

namespace STS2Bridge.Tests.Runtime;

public sealed class PlayerHookArgumentLogicTests
{
    [Fact]
    public void FindDamageTargetPlayerArgument_should_return_target_when_target_is_player()
    {
        var target = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 55,
            MaxHp = 80,
            Block = 0
        };

        var actual = PlayerHookArgumentLogic.FindDamageTargetPlayerArgument(
        [
            new object(),
            new object(),
            new object(),
            target,
            new object()
        ]);

        Assert.Same(target, actual);
    }

    [Fact]
    public void FindDamageTargetPlayerArgument_should_not_fall_back_to_later_player_when_target_is_enemy()
    {
        var enemyTarget = new FakeEnemyCreature
        {
            MonsterId = "slime",
            CurrentHp = 10,
            MaxHp = 10,
            Block = 0
        };

        var playerLaterInArgs = new FakePlayerCreature
        {
            PlayerId = "ironclad",
            CurrentHp = 55,
            MaxHp = 80,
            Block = 0
        };

        var actual = PlayerHookArgumentLogic.FindDamageTargetPlayerArgument(
        [
            new object(),
            new object(),
            new object(),
            enemyTarget,
            new object(),
            playerLaterInArgs
        ]);

        Assert.Null(actual);
    }

    private sealed class FakePlayerCreature
    {
        public string? PlayerId { get; init; }

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }

    private sealed class FakeEnemyCreature
    {
        public string MonsterId { get; init; } = string.Empty;

        public int CurrentHp { get; init; }

        public int MaxHp { get; init; }

        public int Block { get; init; }
    }
}
