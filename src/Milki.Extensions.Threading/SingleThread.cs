namespace Milki.Extensions.Threading;

internal sealed class SingleThread
{
    private readonly Thread _staThread;
    private readonly IQueueReader<SendOrPostCallbackItem> _queueConsumer;
    private readonly SynchronizationContext _syncContext;

    private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

    internal SingleThread(IQueueReader<SendOrPostCallbackItem> reader, SynchronizationContext syncContext,
        string? name = null, bool staThread = false)
    {
        _queueConsumer = reader;
        _syncContext = syncContext;
        _staThread = new Thread(Run)
        {
            Name = name ?? "Standalone Worker Thread",
            IsBackground = true
        };
        if (staThread)
        {
            _staThread.SetApartmentState(ApartmentState.STA);
        }
    }

    internal void Start()
    {
        _staThread.Start();
    }

    internal void Join()
    {
        _staThread.Join();
    }

    private void Run()
    {
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        while (true)
        {
            bool stop = _stopEvent.WaitOne(0);
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