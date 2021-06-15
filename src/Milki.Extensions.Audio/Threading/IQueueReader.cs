using System;

namespace Milki.Extensions.Audio.Threading
{
    internal interface IQueueReader<out T> : IDisposable
    {
        T? Dequeue();
        void ReleaseReader();
    }
}