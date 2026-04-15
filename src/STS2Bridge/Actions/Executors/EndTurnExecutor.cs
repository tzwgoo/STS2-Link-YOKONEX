namespace STS2Bridge.Actions.Executors;

public sealed class EndTurnExecutor : IActionExecutor
{
    public string ActionName => "end_turn";

    public ActionResponse Execute(ActionRequest request, ActionExecutionContext context)
    {
        return ActionResponse.Ok(request.RequestId, "queued");
    }
}
