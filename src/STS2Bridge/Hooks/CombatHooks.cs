namespace STS2Bridge.Hooks;

public static class CombatHooks
{
    public static IReadOnlyList<string> DescribeHooks() => ["combat.started", "combat.ended"];
}
