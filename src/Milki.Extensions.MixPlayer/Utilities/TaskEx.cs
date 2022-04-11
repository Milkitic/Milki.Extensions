using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milki.Extensions.MixPlayer.Utilities;

[Fody.ConfigureAwait(false)]
public static class TaskEx
{
    public static bool TaskSleep(int milliseconds, CancellationTokenSource? cts = null)
    {
        return TaskSleep(TimeSpan.FromMilliseconds(milliseconds), cts);
    }

    public static bool TaskSleep(TimeSpan time, CancellationTokenSource? cts = null)
    {
        try
        {
            Task.Delay(time).Wait(cts?.Token ?? CancellationToken.None);
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return true;
    }

    public static async Task WhenAllSkipNull(params Task?[] playingTask)
    {
        await Task.WhenAll(playingTask.Where(k => !(k is null)));
    }

    public static void WaitAllSkipNull(params Task[] playingTask)
    {
        Task.WaitAll(playingTask.Where(k => !(k is null)).ToArray());
    }

    public static bool IsTaskFree(this Task? task)
    {
        return task != null && (task.IsCanceled || task.IsCompleted || task.IsFaulted);
    }

    public static bool IsTaskBusy(this Task? task)
    {
        return task != null && !task.IsCanceled && !task.IsCompleted && !task.IsFaulted;
    }
}