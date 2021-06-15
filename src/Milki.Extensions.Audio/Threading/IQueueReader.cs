using System;

namespace Milki.Extensions.Audio.Threading
{
    internal interface IQueueReader<T> : IDisposable
    {
        T Dequeue();
        void ReleaseReader();
    }
}