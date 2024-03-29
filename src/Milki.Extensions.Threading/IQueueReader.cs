﻿namespace Milki.Extensions.Threading;

internal interface IQueueReader<out T> : IDisposable
{
    T? Dequeue();
    void ReleaseReader();
}