namespace STS2Bridge.Hooks;

public static class RewardHooks
{
    public static IReadOnlyList<string> DescribeHooks() => ["reward.opened", "reward.selected", "event.option_selected"];
}
