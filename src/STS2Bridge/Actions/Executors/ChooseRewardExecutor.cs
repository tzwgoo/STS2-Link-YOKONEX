namespace STS2Bridge.Actions.Executors;

public sealed class ChooseRewardExecutor : IActionExecutor
{
    public string ActionName => "choose_reward";

    public ActionResponse Execute(ActionRequest request, ActionExecutionContext context)
    {
        return ActionResponse.Ok(request.RequestId, "queued");
    }
}
