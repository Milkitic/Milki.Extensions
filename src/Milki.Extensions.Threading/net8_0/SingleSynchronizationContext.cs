#if NET8_0_OR_GREATER

using System.Threading.Channels;
using Microsoft.Extensions.ObjectPool;

namespace Milki.Extensions.Threading;

public sealed class SingleSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly Channel<SendOrPostCallbackItem> _channel;
    private readonly SingleThread _singleThread;

    private readonly ObjectPool<SendOrPostCallbackItem> _pool;
    private bool _disposed;

    /// <summary>
    /// 当通过 Post 方法执行的委托抛出未处理的异常时触发。
    /// </summary>
    public event EventHandler<Exception>? UnhandledException;

    public SingleSynchronizationContext(string? name = null, bool staThread = false,
        ThreadPriority threadPriority = ThreadPriority.Normal)
    {
        _channel = Channel.CreateUnbounded<SendOrPostCallbackItem>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        _pool = new DefaultObjectPool<SendOrPostCallbackItem>(new DefaultPooledObjectPolicy<SendOrPostCallbackItem>());

        _singleThread = new SingleThread(_channel, this, name, staThread, threadPriority);
        _singleThread.Start();
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (SynchronizationContext.Current == this)
        {
            d(state);
            return;
        }

        EnqueueAndAwaitAsync(d, state).GetAwaiter().GetResult();
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        var item = _pool.Get();
        item.Initialize(d, state, ExecutionType.Post);

        if (!_channel.Writer.TryWrite(item))
        {
            _pool.Return(item);
        }
    }

    public void Invoke(Action action)
    {
        // If already on the single thread, execute inline
        if (SynchronizationContext.Current == this)
        {
            action();
            return;
        }

        var d = new SendOrPostCallback(_ => action());
        EnqueueAndAwaitAsync(d, null).GetAwaiter().GetResult();
    }

    public T Invoke<T>(Func<T> func)
    {
        if (SynchronizationContext.Current == this)
        {
            return func();
        }

        T result = default;
        var d = new SendOrPostCallback(_ => result = func());
        EnqueueAndAwaitAsync(d, null).GetAwaiter().GetResult();
        return result;
    }

    public async Task InvokeAsync(Action action)
    {
        if (SynchronizationContext.Current == this)
        {
            action();
            return;
        }

        var d = new SendOrPostCallback(_ => action());
        await EnqueueAndAwaitAsync(d, null);
    }

    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        if (SynchronizationContext.Current == this)
        {
            return func();
        }

        T result = default;
        var d = new SendOrPostCallback(_ => result = func());
        await EnqueueAndAwaitAsync(d, null);
        return result;
    }

    internal void RaiseUnhandledException(Exception e)
    {
        UnhandledException?.Invoke(this, e);
    }

    internal void ReturnItemToPool(SendOrPostCallbackItem item)
    {
        _pool.Return(item);
    }

    private async Task EnqueueAndAwaitAsync(SendOrPostCallback d, object? state)
    {
        var item = _pool.Get();
        item.Initialize(d, state, ExecutionType.Send);

        if (!_channel.Writer.TryWrite(item))
        {
            _pool.Return(item);
            throw new InvalidOperationException("Failed to write to the synchronization channel.");
        }

        try
        {
            await item.Task;
        }
        finally
        {
            var exceptionInfo = item.ExceptionInfo;
            _pool.Return(item);
            exceptionInfo?.Throw();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.Writer.Complete();
        _singleThread.Join();

        if (_pool is IDisposable disposablePool)
        {
            disposablePool.Dispose();
        }
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}

#endif