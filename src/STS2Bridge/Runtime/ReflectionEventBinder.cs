using System.Linq.Expressions;

namespace STS2Bridge.Runtime;

internal static class ReflectionEventBinder
{
    public static IDisposable Bind(object target, string eventName, Action callback)
    {
        var eventInfo = target.GetType().GetEvent(eventName);
        if (eventInfo is null)
        {
            return NoopDisposable.Instance;
        }

        var handler = CreateDelegate(eventInfo.EventHandlerType!, callback);
        eventInfo.AddEventHandler(target, handler);
        return new Subscription(() => eventInfo.RemoveEventHandler(target, handler));
    }

    private static Delegate CreateDelegate(Type eventHandlerType, Action callback)
    {
        var invokeMethod = eventHandlerType.GetMethod("Invoke")
            ?? throw new InvalidOperationException($"Event handler type '{eventHandlerType.FullName}' does not expose Invoke().");

        var parameters = invokeMethod.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();

        var body = Expression.Call(Expression.Constant(callback.Target), callback.Method);
        return Expression.Lambda(eventHandlerType, body, parameters).Compile();
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                unsubscribe();
            }
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
