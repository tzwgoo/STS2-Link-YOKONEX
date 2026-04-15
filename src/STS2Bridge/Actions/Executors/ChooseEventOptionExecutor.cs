namespace STS2Bridge.Actions.Executors;

public sealed class ChooseEventOptionExecutor : IActionExecutor
{
    public string ActionName => "choose_event_option";

    public ActionResponse Execute(ActionRequest request, ActionExecutionContext context)
    {
        return ActionResponse.Ok(request.RequestId, "queued");
    }
}
