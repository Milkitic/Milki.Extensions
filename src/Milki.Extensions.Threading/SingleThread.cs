namespace Milki.Extensions.Threading;

internal sealed class SingleThread
{
    private readonly Thread _singleThread;
    private readonly IQueueReader<SendOrPostCallbackItem> _queueConsumer;
    private readonly SynchronizationContext _syncContext;
    private readonly ManualResetEventSlim _stopEvent = new(false);

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
            bool stop = _stopEvent.Wait(0);
            if (stop)
            {
                _queueConsumer.Dispose();
                break;
            }

            var workItem = _queueConsumer.Dequeue();
            if (workItem != null)
            {
                workItem.Execute();
            }
        }
    }

    internal void Stop()
    {
        _stopEvent.Set();
        _queueConsumer.ReleaseReader();
    }
}