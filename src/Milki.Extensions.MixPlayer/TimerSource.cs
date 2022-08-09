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
    private Task? _task;

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
        if (!created)
        {
            CreateTask();
        }
    }

    private void CreateTask()
    {
        _cts = new CancellationTokenSource();
        _task = Task.Run(() =>
        {
            TimerLoop();
        });
    }

    public void Stop()
    {
        _stopwatch.Stop();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    public void Restart()
    {
        _offset = 0;
        var created = _stopwatch.IsRunning;
        _stopwatch.Restart();
        if (!created)
        {
            CreateTask();
        }
    }

    public void Reset()
    {
        _offset = 0;
        _stopwatch.Reset();
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
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

    private void TimerLoop()
    {
        double lastTime = _stopwatch.ElapsedMilliseconds;
        var spinWait = new SpinWait();
        while (_cts?.IsCancellationRequested == false)
        {
            if (_stopwatch.IsRunning)
            {
                var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
                if (elapsedMilliseconds - lastTime > NotifyIntervalMillisecond)
                {
                    Updated?.Invoke(elapsedMilliseconds * Rate + _offset);
                    lastTime = elapsedMilliseconds;
                }
            }
            else
            {
                break;
            }

            spinWait.SpinOnce();
        }
    }
}