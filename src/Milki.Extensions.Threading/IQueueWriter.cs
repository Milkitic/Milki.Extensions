namespace Milki.Extensions.Threading;

internal interface IQueueWriter<in T> : IDisposable
{
    void Enqueue(T data);
}