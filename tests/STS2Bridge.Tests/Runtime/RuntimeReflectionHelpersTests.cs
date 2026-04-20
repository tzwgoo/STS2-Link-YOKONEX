using STS2Bridge.Runtime;

namespace STS2Bridge.Tests.Runtime;

public sealed class RuntimeReflectionHelpersTests
{
    [Fact]
    public void TryGetInt_should_support_decimal_members()
    {
        var instance = new FakeDecimalStats
        {
            CurrentHp = 31m
        };

        var success = RuntimeReflectionHelpers.TryGetInt(instance, ["CurrentHp"], out var value);

        Assert.True(success);
        Assert.Equal(31, value);
    }

    [Fact]
    public void TryConvertToInt_should_support_common_numeric_runtime_values()
    {
        Assert.True(RuntimeReflectionHelpers.TryConvertToInt(7m, out var decimalValue));
        Assert.Equal(7, decimalValue);

        Assert.True(RuntimeReflectionHelpers.TryConvertToInt((short)5, out var shortValue));
        Assert.Equal(5, shortValue);

        Assert.True(RuntimeReflectionHelpers.TryConvertToInt((long)12, out var longValue));
        Assert.Equal(12, longValue);
    }

    [Fact]
    public void TryGetIdentifierString_should_support_numeric_members()
    {
        var instance = new FakeNumericIdentifier
        {
            PlayerId = 123UL
        };

        var success = RuntimeReflectionHelpers.TryGetIdentifierString(instance, ["PlayerId"], out var value);

        Assert.True(success);
        Assert.Equal("123", value);
    }

    private sealed class FakeDecimalStats
    {
        public decimal CurrentHp { get; init; }
    }

    private sealed class FakeNumericIdentifier
    {
        public ulong PlayerId { get; init; }
    }
}
