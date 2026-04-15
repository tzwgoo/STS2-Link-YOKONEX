namespace STS2Bridge.Tests.Threading;

public sealed class MainThreadDispatcherTests
{
    [Fact]
    public void Drain_should_execute_all_actions_in_order()
    {
        var dispatcher = new MainThreadDispatcher();
        var order = new List<int>();

        dispatcher.Enqueue(() => order.Add(1));
        dispatcher.Enqueue(() => order.Add(2));

        var result = dispatcher.Drain();

        Assert.Equal(2, result.ExecutedCount);
        Assert.Empty(result.Errors);
        Assert.Equal([1, 2], order);
    }
}
