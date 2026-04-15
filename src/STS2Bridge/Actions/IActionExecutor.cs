namespace STS2Bridge.Actions;

public interface IActionExecutor
{
    string ActionName { get; }

    ActionResponse Execute(ActionRequest request, ActionExecutionContext context);
}

public sealed record ActionExecutionContext(State.GameStateStore StateStore, Events.GameEventBus EventBus);
