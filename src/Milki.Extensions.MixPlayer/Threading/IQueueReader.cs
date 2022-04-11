using System;

namespace Milki.Extensions.MixPlayer.Threading;

internal interface IQueueReader<out T> : IDisposable
{
    T? Dequeue();
    void ReleaseReader();
}