using System;

namespace Milki.Extensions.Audio.Threading
{
    internal interface IQueueWriter<in T> : IDisposable
    {
        void Enqueue(T data);
    }
}