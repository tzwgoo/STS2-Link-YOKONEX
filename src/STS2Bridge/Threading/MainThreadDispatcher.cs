using System.Collections.Concurrent;

namespace STS2Bridge.Threading;

public sealed class MainThreadDispatcher
{
    private readonly ConcurrentQueue<Action> _queue = new();

    public void Enqueue(Action action) => _queue.Enqueue(action);

    public DrainResult Drain()
    {
        var errors = new List<string>();
        var count = 0;

        while (_queue.TryDequeue(out var action))
        {
            try
            {
                action();
                count++;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }
        }

        return new DrainResult(count, errors);
    }
}

public sealed record DrainResult(int ExecutedCount, IReadOnlyList<string> Errors);
