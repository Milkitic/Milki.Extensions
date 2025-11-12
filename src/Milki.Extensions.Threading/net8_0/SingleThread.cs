#if NET8_0_OR_GREATER

using System.Threading.Channels;

namespace Milki.Extensions.Threading;

internal sealed class SingleThread
{
    private readonly Thread _singleThread;
    private readonly ChannelReader<SendOrPostCallbackItem> _channelReader;
    private readonly SingleSynchronizationContext _syncContext;

    internal SingleThread(ChannelReader<SendOrPostCallbackItem> reader, SingleSynchronizationContext syncContext,
        string? name = null, bool staThread = false, ThreadPriority threadPriority = ThreadPriority.Normal)
    {
        _channelReader = reader;
        _syncContext = syncContext;
        _singleThread = new Thread(Run)
        {
            Name = name ?? "Standalone Worker Thread",
            IsBackground = true,
            Priority = threadPriority,
        };
        if (staThread)
        {
            _singleThread.SetApartmentState(ApartmentState.STA);
        }
    }

    internal void Start()
    {
        _singleThread.Start();
    }

    internal void Join()
    {
        _singleThread.Join();
    }

    private void Run()
    {
        SynchronizationContext.SetSynchronizationContext(_syncContext);

        try
        {
            while (true)
            {
                var waitTask = _channelReader.WaitToReadAsync();
                var hasMoreData = waitTask.IsCompletedSuccessfully
                    ? waitTask.GetAwaiter().GetResult()
                    : waitTask.AsTask().GetAwaiter().GetResult();

                if (!hasMoreData)
                {
                    break;
                }

                while (_channelReader.TryRead(out var workItem))
                {
                    workItem.Execute(_syncContext);

                    if (workItem.ExecutionType == ExecutionType.Post)
                    {
                        _syncContext.ReturnItemToPool(workItem);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Channel 关闭，正常退出
        }
        catch (Exception e)
        {
            _syncContext.RaiseUnhandledException(e);
        }
    }
}

#endif