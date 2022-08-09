using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Milki.Extensions.MixPlayer;

public class TimerSource
{
    public event Action<double>? Updated;

    private readonly Stopwatch _stopwatch;
    private double _offset;
    private CancellationTokenSource? _cts;

    public TimerSource(double notifyIntervalMillisecond = 1)
    {
        _stopwatch = new Stopwatch();
        NotifyIntervalMillisecond = notifyIntervalMillisecond;
    }

    public long ElapsedMilliseconds =>
        (long)(_stopwatch.Elapsed.TotalMilliseconds * Rate + _offset);

    public TimeSpan Elapsed =>
        TimeSpan.FromMilliseconds(_stopwatch.Elapsed.TotalMilliseconds * Rate + _offset);

    public float Rate { get; set; } = 1;

    public double NotifyIntervalMillisecond { get; set; }

    public void Start()
    {
        var created = _stopwatch.IsRunning;
        _stopwatch.Start();
        Updated?.Invoke(ElapsedMilliseconds);
        if (!created)
        {
            CreateTask();
        }
    }

    public void Stop()
    {
        _stopwatch.Stop();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }
    }

    public void Restart()
    {
        _offset = 0;
        _stopwatch.Restart();
        Updated?.Invoke(ElapsedMilliseconds);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }

        CreateTask();
    }

    public void Reset()
    {
        _offset = 0;
        _stopwatch.Reset();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }
    }

    public void SkipTo(double offset)
    {
        _offset = offset;
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Restart();
        }
        else
        {
            _stopwatch.Reset();
        }
    }

    private void TimerLoop(CancellationTokenSource cts)
    {
        double loopLastTime = _stopwatch.Elapsed.TotalMilliseconds * Rate + _offset;
        Updated?.Invoke(loopLastTime);
        var spinWait = new SpinWait();
        while (!cts.IsCancellationRequested)
        {
            if (_stopwatch.IsRunning)
            {
                var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds * Rate + _offset;
                if (elapsedMilliseconds - loopLastTime > NotifyIntervalMillisecond)
                {
                    Updated?.Invoke(elapsedMilliseconds);
                    loopLastTime = elapsedMilliseconds;
                }
            }
            else
            {
                break;
            }

            spinWait.SpinOnce();
        }

        cts.Dispose();
    }

    private void CreateTask()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => TimerLoop(_cts));
    }
}