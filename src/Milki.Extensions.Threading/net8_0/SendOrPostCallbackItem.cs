#if NET8_0_OR_GREATER

using System.Runtime.ExceptionServices;

namespace Milki.Extensions.Threading;

internal class SendOrPostCallbackItem
{
    private SendOrPostCallback? _method;
    private object? _state;
    private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SendOrPostCallbackItem() { }

    public ExceptionDispatchInfo? ExceptionInfo { get; private set; }
    public Task Task => _tcs.Task;
    public ExecutionType ExecutionType { get; private set; }

    public void Initialize(SendOrPostCallback callback, object? state, ExecutionType type)
    {
        _method = callback;
        _state = state;
        ExecutionType = type;
    }

    public virtual void Reset()
    {
        _method = null;
        _state = null;
        ExceptionInfo = null;

        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }


    internal void Execute(SingleSynchronizationContext context)
    {
        if (_method == null) return;
        try
        {
            _method(_state);
            if (ExecutionType == ExecutionType.Send)
            {
                _tcs.TrySetResult(true);
            }
        }
        catch (Exception e)
        {
            if (ExecutionType == ExecutionType.Send)
            {
                ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                _tcs.TrySetResult(false);
            }
            else
            {
                context.RaiseUnhandledException(e);
            }
        }
    }
}

#endif