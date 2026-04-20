using STS2Bridge.Runtime;

namespace STS2Bridge.Tests.Runtime;

public sealed class PlayerBlockHookTargetCatalogTests
{
    [Fact]
    public void BlockLossMethodNames_should_cover_damage_and_direct_loss_paths()
    {
        Assert.Contains("LoseBlockInternal", PlayerBlockHookTargetCatalog.BlockLossMethodNames);
        Assert.Contains("DamageBlockInternal", PlayerBlockHookTargetCatalog.BlockLossMethodNames);
    }
}
