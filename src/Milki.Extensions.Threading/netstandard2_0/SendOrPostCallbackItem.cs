#if NETSTANDARD2_0

namespace Milki.Extensions.Threading;

internal sealed class SendOrPostCallbackItem
{
    private readonly object? _state;
    private readonly ExecutionType _exeType;
    private readonly SendOrPostCallback _method;
    private readonly SemaphoreSlim _asyncWaitHandle = new(0, 1);

    internal SendOrPostCallbackItem(SendOrPostCallback callback,
        object? state, ExecutionType type)
    {
        _method = callback;
        _state = state;
        _exeType = type;
    }

    internal Exception? Exception { get; private set; } = null;

    internal bool ExecutedWithException => Exception != null;

    // this code must run ont the STA thread
    internal void Execute()
    {
        if (_exeType == ExecutionType.Send)
            Send();
        else
            Post();
    }

    // calling thread will block until mAsyncWaitHandle is set
    internal void Send()
    {
        try
        {
            // call the thread
            _method(_state);
        }
        catch (Exception e)
        {
            Exception = e;
        }
        finally
        {
            _asyncWaitHandle.Release();
        }
    }

    /// <summary>
    /// Unhandled exceptions will terminate the STA thread
    /// </summary>
    internal void Post()
    {
        _method(_state);
    }

    internal SemaphoreSlim ExecutionCompleteWaitHandle => _asyncWaitHandle;
}

#endif