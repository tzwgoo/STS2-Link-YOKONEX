namespace STS2Bridge.Compatibility;

public static class HookGuard
{
    public static HookResult Run(string hookName, Action action)
    {
        try
        {
            action();
            return new HookResult(hookName, true, "ok");
        }
        catch (Exception ex)
        {
            return new HookResult(hookName, false, ex.Message);
        }
    }
}

public sealed record HookResult(string HookName, bool Success, string Message);
