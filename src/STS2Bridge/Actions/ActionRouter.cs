using STS2Bridge.Actions.Executors;
using STS2Bridge.Config;
using STS2Bridge.Events;
using STS2Bridge.State;
using STS2Bridge.Threading;

namespace STS2Bridge.Actions;

public sealed class ActionRouter
{
    private readonly HashSet<string> _allowedActions;
    private readonly Dictionary<string, IActionExecutor> _executors;
    private readonly HashSet<string> _processedRequestIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly MainThreadDispatcher _dispatcher;
    private readonly ActionExecutionContext _context;
    private readonly Lock _lock = new();

    public ActionRouter(
        IEnumerable<string> allowedActions,
        IEnumerable<IActionExecutor> executors,
        MainThreadDispatcher dispatcher,
        GameStateStore stateStore,
        GameEventBus eventBus)
    {
        _allowedActions = new HashSet<string>(allowedActions, StringComparer.OrdinalIgnoreCase);
        _executors = executors.ToDictionary(item => item.ActionName, StringComparer.OrdinalIgnoreCase);
        _dispatcher = dispatcher;
        _context = new ActionExecutionContext(stateStore, eventBus);
    }

    public static ActionRouter CreateDefault(
        BridgeConfig config,
        MainThreadDispatcher dispatcher,
        GameStateStore stateStore,
        GameEventBus eventBus)
    {
        return new ActionRouter(
            config.AllowedActions,
            [
                new EndTurnExecutor(),
                new PlayCardExecutor(),
                new ChooseRewardExecutor(),
                new ChooseEventOptionExecutor()
            ],
            dispatcher,
            stateStore,
            eventBus);
    }

    public Task<ActionResponse> RouteAsync(ActionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_allowedActions.Contains(request.Action))
        {
            return Task.FromResult(ActionResponse.Fail(request.RequestId, "ACTION_NOT_ALLOWED", $"Action '{request.Action}' is not allowed."));
        }

        lock (_lock)
        {
            if (!_processedRequestIds.Add(request.RequestId))
            {
                return Task.FromResult(ActionResponse.Fail(request.RequestId, "DUPLICATE_REQUEST", "RequestId has already been processed."));
            }
        }

        if (!_executors.TryGetValue(request.Action, out var executor))
        {
            return Task.FromResult(ActionResponse.Fail(request.RequestId, "EXECUTOR_NOT_FOUND", $"Executor for '{request.Action}' was not found."));
        }

        ActionResponse response;
        try
        {
            response = executor.Execute(request, _context);
        }
        catch (Exception ex)
        {
            response = ActionResponse.Fail(request.RequestId, "ACTION_EXECUTION_ERROR", ex.Message);
        }

        if (response.Success)
        {
            _dispatcher.Enqueue(() => { });
        }

        return Task.FromResult(response);
    }
}
