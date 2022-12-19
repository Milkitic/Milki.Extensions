namespace Milki.Extensions.Threading;

public sealed class SingleSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly BlockingQueue<SendOrPostCallbackItem> _queue;
    private readonly SingleThread _singleThread;

    public SingleSynchronizationContext(string? name = null, bool staThread = false)
    {
        _queue = new BlockingQueue<SendOrPostCallbackItem>();
        _singleThread = new SingleThread(_queue, this, name, staThread);
        _singleThread.Start();
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        // create an item for execution
        var item = new SendOrPostCallbackItem(d, state, ExecutionType.Send);
        // queue the item
        _queue.Enqueue(item);
        // wait for the item execution to end
        item.ExecutionCompleteWaitHandle.WaitOne();

        // if there was an exception, throw it on the caller thread, not the
        // sta thread.
        if (item.ExecutedWithException)
        {
            throw item.Exception!;
        }
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        // queue the item and don't wait for its execution. This is risky because
        // an unhandled exception will terminate the STA thread. Use with caution.
        var item = new SendOrPostCallbackItem(d, state, ExecutionType.Post);
        _queue.Enqueue(item);
    }

    public void Dispose()
    {
        _singleThread.Stop();
        _queue.Dispose();
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}