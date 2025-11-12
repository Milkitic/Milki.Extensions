#if NETSTANDARD2_0

namespace Milki.Extensions.Threading;

internal sealed class SingleThread
{
    private readonly Thread _singleThread;
    private readonly IQueueReader<SendOrPostCallbackItem> _queueConsumer;
    private readonly SynchronizationContext _syncContext;

    internal SingleThread(IQueueReader<SendOrPostCallbackItem> reader, SynchronizationContext syncContext,
        string? name = null, bool staThread = false, ThreadPriority threadPriority = ThreadPriority.Normal)
    {
        _queueConsumer = reader;
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
        while (true)
        {
            var workItem = _queueConsumer.Dequeue();
            if (workItem == null)
            {
                // Reader released and queue drained; exit thread
                break;
            }

            workItem.Execute();
        }
    }

    internal void Stop()
    {
        // Signal reader to unblock and allow clean drain then exit
        _queueConsumer.ReleaseReader();
    }
}

#endif