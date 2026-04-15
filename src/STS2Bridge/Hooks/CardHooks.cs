namespace STS2Bridge.Hooks;

public static class CardHooks
{
    public static IReadOnlyList<string> DescribeHooks() => ["card.played"];
}
