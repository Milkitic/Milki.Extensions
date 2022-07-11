using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.SoundTouch;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.Subchannels;

public class SingleMediaChannel : Subchannel
{
    private readonly string _path;

    private SmartWaveReader? _fileReader;
    private VariableSpeedSampleProvider? _speedProvider;
    private ISampleProvider? _actualRoot;

    private readonly CancellationTokenSource _cts = new();
    private readonly VariableStopwatch _sw = new();
    private ConcurrentQueue<double> _offsetQueue = new();
    private int? _referenceOffset;
    private Task? _backoffTask;

    private static readonly ILogger? Logger = Configuration.Instance.GetCurrentClassLogger();

    public override float Volume
    {
        get => _fileReader?.Volume ?? 0;
        set
        {
            if (_fileReader != null) _fileReader.Volume = value;
        }
    }

    public override TimeSpan Duration { get; protected set; }

    public override TimeSpan ChannelStartTime => TimeSpan.FromMilliseconds(Configuration.Instance.GeneralOffset);

    public sealed override float PlaybackRate { get; protected set; }
    public sealed override bool KeepTune { get; protected set; }

    public SingleMediaChannel(AudioPlaybackEngine engine, string path, float playbackRate, bool keepTune)
        : base(engine)
    {
        _path = path;
        PlaybackRate = playbackRate;
        KeepTune = keepTune;
    }

    public override async Task Initialize()
    {
        _fileReader = await ResampleHelper.GetResampledAudioFileReader(_path, Engine.FileWaveFormat).ConfigureAwait(false);

        _speedProvider = new VariableSpeedSampleProvider(_fileReader,
            10,
            new VariableSpeedOptions(KeepTune, false)
        )
        {
            PlaybackRate = PlaybackRate
        };

        Duration = _fileReader.TotalTime;

        SampleControl.Volume = 1;
        SampleControl.Balance = 0;
        PlayStatus = PlayStatus.Ready;
        _backoffTask = new Task(() =>
        {
            const int avgCount = 30;

            var oldTime = TimeSpan.Zero;
            var stdOffset = 0;
            while (!_cts.IsCancellationRequested)
            {
                Position = _sw.Elapsed /*newTime*/ - TimeSpan.FromMilliseconds(_referenceOffset ?? 0);
                RaisePositionUpdated(Position, false);
                var newTime = _fileReader.CurrentTime;
                if (oldTime != newTime)
                {
                    oldTime = newTime;
                    var offset = _sw.Elapsed - _fileReader.CurrentTime;
                    if (_offsetQueue.Count < avgCount)
                    {
                        _offsetQueue.Enqueue(offset.TotalMilliseconds);
                        if (_offsetQueue.Count == avgCount)
                        {
                            var avg = (int)_offsetQueue.Average();
                            stdOffset = avg;
                            Logger?.LogDebug("{0}: avg offset: {1}", Description, avg);
                        }
                    }
                    else
                    {
                        if (_offsetQueue.TryDequeue(out _))
                        {
                            _offsetQueue.Enqueue(offset.TotalMilliseconds);
                            var refOffset = (int)_offsetQueue.Average() - stdOffset;
                            if (refOffset != _referenceOffset)
                            {
                                _referenceOffset = refOffset;
                                //Logger.Debug("{0}: {1}: {2}", Description, nameof(_referenceOffset),
                                //    _referenceOffset);
                            }
                        }
                    }
                }

                Thread.Sleep(1);
            }
        }, TaskCreationOptions.LongRunning);
        _backoffTask.Start();
        await Task.CompletedTask;
    }

    public override async Task Play()
    {
        if (PlayStatus == PlayStatus.Playing) return;

        if (!Engine.RootMixer.MixerInputs.Contains(_actualRoot) && _speedProvider != null)
            Engine.RootMixer.AddMixerInput(_speedProvider, SampleControl, out _actualRoot);
        PlayStatus = PlayStatus.Playing;
        _sw.Start();
        RaisePositionUpdated(Position, true);
        await Task.CompletedTask;
    }

    public override async Task Pause()
    {
        if (PlayStatus == PlayStatus.Paused) return;

        Engine.RootMixer.RemoveMixerInput(_actualRoot);
        PlayStatus = PlayStatus.Paused;
        _sw.Stop();
        await Task.CompletedTask;
    }

    public override async Task Stop()
    {
        if (PlayStatus == PlayStatus.Paused && Position == TimeSpan.Zero) return;

        Engine.RootMixer.RemoveMixerInput(_actualRoot);
        Logger?.LogDebug("{0} will skip.", Description);
        await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
        PlayStatus = PlayStatus.Paused;
        _sw.Reset();
        await Task.CompletedTask;
    }

    public override async Task Restart()
    {
        if (Position == TimeSpan.Zero) return;

        await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
        await Play().ConfigureAwait(false);
        _sw.Restart();
        await Task.CompletedTask;
    }

    public override async Task SkipTo(TimeSpan time)
    {
        if (time == Position) return;

        var status = PlayStatus;
        PlayStatus = PlayStatus.Reposition;
        if (_fileReader != null && _fileReader.TotalTime > TimeSpan.Zero)
        {
            _fileReader.CurrentTime = time >= _fileReader.TotalTime
                ? _fileReader.TotalTime - TimeSpan.FromMilliseconds(1)
                : time;
        }

        _speedProvider?.Reposition();
        Position = time /*_fileReader.CurrentTime*/;
        RaisePositionUpdated(Position, true);
        Logger?.LogDebug("{0} skip: want: {1}; actual: {2}", Description, time, Position);
        _sw.SkipTo(time);

        _referenceOffset = null;
        _offsetQueue = new ConcurrentQueue<double>();
        PlayStatus = status;
        await Task.CompletedTask;
    }

    public override async Task Sync(TimeSpan time)
    {
        if (_fileReader != null)
        {
            _fileReader.CurrentTime = time >= _fileReader.TotalTime
                ? _fileReader.TotalTime - TimeSpan.FromMilliseconds(1)
                : time;
        }

        _speedProvider?.Reposition();
        Position = time /*_fileReader.CurrentTime*/;
        RaisePositionUpdated(Position, false);
        _sw.SkipTo(time);
        await Task.CompletedTask;
    }

    public override async Task SetPlaybackRate(float rate, bool keepTune)
    {
        bool changed = !rate.Equals(PlaybackRate) || keepTune != KeepTune;
        if (!PlaybackRate.Equals(rate))
        {
            PlaybackRate = rate;
            if (_speedProvider != null) _speedProvider.PlaybackRate = rate;
            _sw.Rate = rate;
        }

        if (KeepTune != keepTune)
        {
            _speedProvider?.SetSoundTouchProfile(new VariableSpeedOptions(keepTune, false));
            KeepTune = keepTune;
        }

        if (changed) await SkipTo(_sw.Elapsed).ConfigureAwait(false);
        await Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        Logger?.LogDebug($"Disposing: Canceled {nameof(_cts)}.");
        if (_backoffTask != null)
            await _backoffTask.ConfigureAwait(false);
        Logger?.LogDebug($"Disposing: Stopped task {nameof(_backoffTask)}.");
        _cts.Dispose();
        Logger?.LogDebug($"Disposing: Disposed {nameof(_cts)}.");
        await Stop().ConfigureAwait(false);
        Logger?.LogDebug($"Disposing: Stopped.");
        //await base.DisposeAsync();
        //Logger.Debug($"Disposing: Disposed base.");
        _speedProvider?.Dispose();
        Logger?.LogDebug($"Disposing: Disposed {nameof(_speedProvider)}.");
        if (_fileReader != null)
        {
            _fileReader.Dispose();
        }

        Logger?.LogDebug($"Disposing: Disposed {nameof(_fileReader)}.");
    }
}