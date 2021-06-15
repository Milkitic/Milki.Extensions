using System;

namespace Milki.Extensions.Audio.Threading
{
    internal interface IQueueWriter<T> : IDisposable
    {
        void Enqueue(T data);
    }
}