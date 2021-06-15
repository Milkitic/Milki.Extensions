using System;
using System.Threading;

namespace Milki.Extensions.Audio.Threading
{
    public class StaSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingQueue<SendOrPostCallbackItem> _queue;
        private readonly StaThread _staThread;
        private readonly SynchronizationContext _oldSync;

        public StaSynchronizationContext(string? name = null)
        {
            _queue = new BlockingQueue<SendOrPostCallbackItem>();
            _staThread = new StaThread(_queue, this, name);
            _staThread.Start();
            _oldSync = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
        }

        public override void Send(SendOrPostCallback d, object state)
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
                throw item.Exception!;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            // queue the item and don't wait for its execution. This is risky because
            // an unhandled exception will terminate the STA thread. Use with caution.
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d, state,
                ExecutionType.Post);
            _queue.Enqueue(item);
        }

        public void Dispose()
        {
            _staThread.Stop();
            SynchronizationContext.SetSynchronizationContext(_oldSync);
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}