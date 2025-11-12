#if NETSTANDARD2_0

namespace Milki.Extensions.Threading;

public sealed class SingleSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly BlockingQueue<SendOrPostCallbackItem> _queue;
    private readonly SingleThread _singleThread;
    private bool _disposed;

    public SingleSynchronizationContext(string? name = null, bool staThread = false,
        ThreadPriority threadPriority = ThreadPriority.Normal)
    {
        _queue = new BlockingQueue<SendOrPostCallbackItem>();
        _singleThread = new SingleThread(_queue, this, name, staThread, threadPriority);
        _singleThread.Start();
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        // If already on the single thread, execute inline to avoid deadlock
        if (SynchronizationContext.Current == this)
        {
            d(state);
            return;
        }

        // create an item for execution
        var item = new SendOrPostCallbackItem(d, state, ExecutionType.Send);
        // queue the item
        _queue.Enqueue(item);
        // wait for the item execution to end
        item.ExecutionCompleteWaitHandle.Wait();

        // if there was an exception, throw it on the caller thread, not the
        // sta thread.
        if (item.ExecutedWithException)
        {
            throw item.Exception!;
        }
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        // If already on the single thread, execute inline to reduce overhead
        if (SynchronizationContext.Current == this)
        {
            d(state);
            return;
        }

        // queue the item and don't wait for its execution. This is risky because
        // an unhandled exception will terminate the STA thread. Use with caution.
        var item = new SendOrPostCallbackItem(d, state, ExecutionType.Post);
        _queue.Enqueue(item);
    }

    public void Invoke(Action action)
    {
        // If already on the single thread, execute inline
        if (SynchronizationContext.Current == this)
        {
            action();
            return;
        }

        // create an item for execution
        var d = new SendOrPostCallback(_ => action());
        var item = new SendOrPostCallbackItem(d, null, ExecutionType.Send);
        // queue the item
        _queue.Enqueue(item);
        // wait for the item execution to end
        item.ExecutionCompleteWaitHandle.Wait();

        // if there was an exception, throw it on the caller thread, not the
        // sta thread.
        if (item.ExecutedWithException)
        {
            throw item.Exception!;
        }
    }

    public T Invoke<T>(Func<T> func)
    {
        // If already on the single thread, execute inline
        if (SynchronizationContext.Current == this)
        {
            return func();
        }

        // create an item for execution
        T result = default;
        var d = new SendOrPostCallback(_ => result = func());
        var item = new SendOrPostCallbackItem(d, null, ExecutionType.Send);
        // queue the item
        _queue.Enqueue(item);
        // wait for the item execution to end
        item.ExecutionCompleteWaitHandle.Wait();

        // if there was an exception, throw it on the caller thread, not the
        // sta thread.
        if (item.ExecutedWithException)
        {
            throw item.Exception!;
        }

        return result;
    }

    public async Task InvokeAsync(Action action)
    {
        // If already on the single thread, execute inline
        if (SynchronizationContext.Current == this)
        {
            action();
            return;
        }

        // create an item for execution
        var d = new SendOrPostCallback(_ => action());
        var item = new SendOrPostCallbackItem(d, null, ExecutionType.Send);
        // queue the item
        _queue.Enqueue(item);
        // wait for the item execution to end
        await item.ExecutionCompleteWaitHandle.WaitAsync();

        // if there was an exception, throw it on the caller thread, not the
        // sta thread.
        if (item.ExecutedWithException)
        {
            throw item.Exception!;
        }
    }

    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        // If already on the single thread, execute inline
        if (SynchronizationContext.Current == this)
        {
            return func();
        }

        // create an item for execution
        T result = default;
        var d = new SendOrPostCallback(_ => result = func());
        var item = new SendOrPostCallbackItem(d, null, ExecutionType.Send);
        // queue the item
        _queue.Enqueue(item);
        // wait for the item execution to end
        await item.ExecutionCompleteWaitHandle.WaitAsync();

        // if there was an exception, throw it on the caller thread, not the
        // sta thread.
        if (item.ExecutedWithException)
        {
            throw item.Exception!;
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _singleThread.Stop();
        _singleThread.Join();
        _queue.Dispose();
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}

#endif