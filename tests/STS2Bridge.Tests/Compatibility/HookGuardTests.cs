namespace STS2Bridge.Tests.Compatibility;

public sealed class HookGuardTests
{
    [Fact]
    public void Run_should_capture_exception_and_return_false()
    {
        var result = STS2Bridge.Compatibility.HookGuard.Run("test-hook", () => throw new InvalidOperationException("boom"));

        Assert.False(result.Success);
        Assert.Contains("boom", result.Message);
    }
}
