using System;
using System.Collections.Generic;
using System.Threading;

namespace Milki.Extensions.MixPlayer.Threading;

internal class BlockingQueue<T> : IQueueReader<T>, IQueueWriter<T>
{
    // use a .NET queue to store the data
    private readonly Queue<T> _queue = new Queue<T>();
    // create a semaphore that contains the items in the queue as resources.
    // initialize the semaphore to zero available resources (empty queue).
    private readonly Semaphore _semaphore = new Semaphore(0, int.MaxValue);
    // a event that gets triggered when the reader thread is exiting
    private readonly ManualResetEvent _killThread = new ManualResetEvent(false);
    // wait handles that are used to unblock a Dequeue operation.
    // Either when there is an item in the queue
    // or when the reader thread is exiting.
    private readonly WaitHandle[] _waitHandles;

    public BlockingQueue()
    {
        _waitHandles = new WaitHandle[] { _semaphore, _killThread };
    }

    public void Enqueue(T data)
    {
        lock (_queue) _queue.Enqueue(data);
        // add an available resource to the semaphore,
        // because we just put an item
        // into the queue.
        _semaphore.Release();
    }

    public T? Dequeue()
    {
        // wait until there is an item in the queue
        WaitHandle.WaitAny(_waitHandles);
        lock (_queue)
        {
            if (_queue.Count > 0)
                return _queue.Dequeue();
        }

        return default;
    }

    public void ReleaseReader()
    {
        _killThread.Set();
    }


    void IDisposable.Dispose()
    {
        _semaphore.Close();
        _queue.Clear();
    }
}