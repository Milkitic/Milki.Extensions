using System;

namespace Milki.Extensions.MixPlayer.Threading
{
    internal interface IQueueWriter<in T> : IDisposable
    {
        void Enqueue(T data);
    }
}