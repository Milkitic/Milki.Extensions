using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Subchannels;
using Milki.Extensions.MixPlayer.Utilities;

namespace Milki.Extensions.MixPlayer;

[Fody.ConfigureAwait(false)]
public abstract class MultichannelPlayer : IChannel
{
    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;

    public virtual string Description { get; } = "Player";

    public TimeSpan Duration { get; protected set; }

    public TimeSpan Position => _innerTimelineSw.Elapsed;

    public float PlaybackRate
    {
        get => _innerTimelineSw.Rate;
        private set => _innerTimelineSw.Rate = value;
    }
    public bool KeepTune { get; private set; }

    public PlayStatus PlayStatus
    {
        get => _playStatus;
        protected set
        {
            if (value == _playStatus) return;
            _playStatus = value;
            Engine.Context.Send(_ => PlayStatusChanged?.Invoke(value), null);
        }
    }

    public StopMode StopMode { get; set; }

    public float Volume
    {
        get => Engine.RootVolume;
        set => Engine.RootVolume = value;
    }

    protected ReadOnlyCollection<Subchannel> Subchannels => new(_subchannels);
    protected readonly AudioPlaybackEngine Engine;

    private readonly List<Subchannel> _subchannels = new();
    //private readonly IWavePlayer _outputDevice;

    private readonly VariableStopwatch _innerTimelineSw = new();
    private CancellationTokenSource? _cts;
    private Task? _playTask;

    private ConcurrentQueue<Subchannel>? _channelsQueue;
    private SortedSet<Subchannel> _runningChannels = new(new ChannelEndTimeComparer());
    private PlayStatus _playStatus;

    private static readonly ILogger? Logger = Configuration.Instance.GetCurrentClassLogger();
    private DateTime _lastPositionUpdateTime;
    public TimeSpan AutoRefreshInterval { get; protected set; } = TimeSpan.FromMilliseconds(500);

    public MultichannelPlayer(DeviceDescription deviceDescription)
    {
        #region Resample? 

        //try
        //{
        //    if (_outputDevice is WasapiOut wasapi)
        //    {
        //        WaveFormatFactory.Bits = 16;
        //        //WaveFormatFactory.Bits = wasapi.OutputWaveFormat.BitsPerSample > 24
        //        //    ? 24
        //        //    : wasapi.OutputWaveFormat.BitsPerSample;
        //        WaveFormatFactory.Channels = wasapi.OutputWaveFormat.Channels;
        //        WaveFormatFactory.SampleRate = wasapi.OutputWaveFormat.SampleRate;
        //    }
        //    else
        //    {
        //        if (actualDeviceInfo is DirectSoundOutInfo dsoi && dsoi.DeviceGuid == Guid.Empty)
        //        {
        //            wasapi = new WasapiOut();
        //        }
        //        else
        //        {
        //            var wasInfo = (WasapiInfo)DeviceCreationHelper.EnumerateAvailableDevices().First(k =>
        //                k.Name?.Contains(actualDeviceInfo.Name) == true && k is WasapiInfo);
        //            wasapi = new WasapiOut(wasInfo.Device, AudioClientShareMode.Shared, true,
        //                AppSettings.Default.Play.DesiredLatency);
        //        }

        //        WaveFormatFactory.Bits = 16;
        //        //WaveFormatFactory.Bits = wasapi.OutputWaveFormat.BitsPerSample > 24
        //        //    ? 24
        //        //    : wasapi.OutputWaveFormat.BitsPerSample;
        //        WaveFormatFactory.Channels = wasapi.OutputWaveFormat.Channels;
        //        WaveFormatFactory.SampleRate = wasapi.OutputWaveFormat.SampleRate;
        //    }

        //    Logger.Debug("BitsPerSample: {0}, Channels: {1}, SampleRate: {2}", WaveFormatFactory.Bits,
        //        WaveFormatFactory.Channels, WaveFormatFactory.SampleRate);
        //}
        //catch (Exception ex)
        //{
        //    Logger.Error(ex, "Error while getting device output wave format, use default settings.");
        //    WaveFormatFactory.Bits = 16;
        //    WaveFormatFactory.Channels = 2;
        //    WaveFormatFactory.SampleRate = 44100;
        //}

        #endregion
        Engine = new AudioPlaybackEngine(deviceDescription);
    }

    public virtual async Task Initialize()
    {
        bool addition = false;
        var endTimes = Subchannels
            .Where(k => k != null)
            .Select(k =>
            {
                if (k.Duration <= TimeSpan.FromMilliseconds(100) && k is SingleMediaChannel) addition = true;
                return k.ChannelEndTime;
            });
        var max = MathUtils.Max(endTimes);

        Duration = addition ? max + TimeSpan.FromSeconds(1) : max;
        PlayStatus = PlayStatus.Ready;

        await Task.CompletedTask;
    }

    public async Task Play()
    {
        if (_playTask?.Status == TaskStatus.Running)
            return;
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _innerTimelineSw.Start();
        RaisePositionUpdated(_innerTimelineSw.Elapsed, true);

        if (_channelsQueue == null)
            await RequeueChannel();

        _playTask = Task.Run(async () =>
        {
            var date = DateTime.Now;
            var lastEnsurePos = _innerTimelineSw.Elapsed;
            bool firstEnsure = true;

            var lastBuffPos = _innerTimelineSw.Elapsed;
            while (!_cts.IsCancellationRequested)
            {
                RaisePositionUpdated(_innerTimelineSw.Elapsed, false);
                //if (_runningChannels.Count > 0)
                //    Console.WriteLine(string.Join("; ",
                //        _runningChannels.Select(k => $"{k.Description}: {k.Position.TotalMilliseconds}")));
                if (_innerTimelineSw.Elapsed - lastEnsurePos >= TimeSpan.FromSeconds(1.5) || firstEnsure)
                {
                    if (!EnsureSoundElementsLoaded())
                    {
                        _innerTimelineSw.Stop();
                        foreach (var runningChannel in _runningChannels)
                        {
                            await runningChannel.Pause();
                            //RemoveSubchannel(runningChannel);
                        }

                        await BufferSoundElementsAsync();

                        foreach (var runningChannel in _runningChannels)
                        {
                            await runningChannel.Play();
                            //AddSubchannel(runningChannel);
                        }

                        _innerTimelineSw.Start();
                    }

                    lastEnsurePos = _innerTimelineSw.Elapsed;
                    firstEnsure = false;
                }

                if (_innerTimelineSw.Elapsed - lastBuffPos >= TimeSpan.FromSeconds(1.5))
                {
                    _ = BufferSoundElementsAsync(); // no await
                    lastBuffPos = _innerTimelineSw.Elapsed;
                }

                if (_channelsQueue!.Count > 0 &&
                    _channelsQueue.TryPeek(out var channel) &&
                    channel.ChannelStartTime <= _innerTimelineSw.Elapsed &&
                    _channelsQueue.TryDequeue(out channel))
                {
                    _runningChannels.Add(channel);
                    await channel.Play();
                    Logger?.LogDebug("[{0}] Play: {1}", _innerTimelineSw.Elapsed, channel.Description);

                    if (_channelsQueue.Count == 0)
                        Logger?.LogDebug("[{0}] All channels are playing.", _innerTimelineSw.Elapsed);
                }

                if (Position > Duration)
                {
                    _innerTimelineSw.Stop();
                    SetTime(Duration);
                    RaisePositionUpdated(_innerTimelineSw.Elapsed, true);
                    PlayStatus = PlayStatus.Finished;
                    break;
                }

                if (_runningChannels.Count > 0 &&
                    _runningChannels.First().ChannelEndTime < _innerTimelineSw.Elapsed)
                {
                    _runningChannels.Remove(_runningChannels.First());
                }

                if (DateTime.Now - date > TimeSpan.FromMilliseconds(50))
                {
                    await InnerSync();
                    date = DateTime.Now;
                }

                Thread.Sleep(1);
            }
        });

        foreach (var channel in _runningChannels.ToList())
        {
            await channel.Play();
        }

        PlayStatus = PlayStatus.Playing;
        await Task.CompletedTask;
    }

    public async Task Pause()
    {
        var pos = Position;
        await CancelTask();
        _innerTimelineSw.Stop();
        _innerTimelineSw.SkipTo(pos);

        foreach (var channel in _runningChannels.ToList())
        {
            await channel.Pause();
        }

        RaisePositionUpdated(_innerTimelineSw.Elapsed, true);
        PlayStatus = PlayStatus.Paused;
    }

    public async Task TogglePlay()
    {
        if (PlayStatus == PlayStatus.Ready ||
            PlayStatus == PlayStatus.Finished ||
            PlayStatus == PlayStatus.Paused)
        {
            await Play();
        }
        else if (PlayStatus == PlayStatus.Playing) await Pause();
    }

    public async Task Stop()
    {
        var pos = Position;
        await CancelTask();
        _innerTimelineSw.Stop();
        _innerTimelineSw.SkipTo(pos);

        foreach (var channel in _runningChannels.ToList())
        {
            Logger?.LogDebug("Will stop: {0}.", channel.Description);
            await channel.Stop();
            Logger?.LogDebug("{0} stopped.", channel.Description);
        }

        SetTime(TimeSpan.Zero);
        RaisePositionUpdated(_innerTimelineSw.Elapsed, true);
        PlayStatus = PlayStatus.Paused;
    }

    public async Task Restart()
    {
        await Stop();
        await Play();
    }

    public async Task SkipTo(TimeSpan time)
    {
        SetTime(time);
        await RequeueChannel();
        foreach (var channel in _runningChannels.ToList())
        {
            await channel.SkipTo(time - channel.ChannelStartTime);
            if (PlayStatus == PlayStatus.Playing)
            {
                await channel.Play();
            }
        }

        RaisePositionUpdated(_innerTimelineSw.Elapsed, true);
    }

    public async Task SetPlaybackRate(float rate, bool keepTune)
    {
        foreach (var channel in _subchannels.ToList())
        {
            await channel.SetPlaybackRate(rate, keepTune);
        }

        PlaybackRate = rate;
        if (keepTune != KeepTune)
        {
            KeepTune = keepTune;
            await SkipTo(Position);
        }
    }

    protected async Task DisposeSubChannelsAsync()
    {
        await Stop();

        foreach (var subchannel in _subchannels.ToList())
        {
            await subchannel.DisposeAsync();
            Logger?.LogDebug("Disposing: Disposed {0}.", subchannel.Description);
        }

        _subchannels.Clear();
        _channelsQueue = null;
    }

    public virtual async ValueTask DisposeAsync()
    {
        Logger?.LogDebug($"Disposing: Start to dispose.");
        await DisposeSubChannelsAsync();

        Engine.Dispose();

        Logger?.LogDebug("Disposing: Disposed {0}.", nameof(Engine));
        _cts?.Dispose();
        Logger?.LogDebug("Disposing: Disposed {0}.", nameof(_cts));
        await TaskEx.WhenAllSkipNull(_playTask);
        _playTask?.Dispose();
        Logger?.LogDebug("Disposing: Disposed {0}.", nameof(_playTask));
    }

    protected void AddSubchannel(Subchannel channel)
    {
        _subchannels.Add(channel);
    }

    protected void RemoveSubchannel(Subchannel channel)
    {
        _subchannels.Remove(channel);
    }

    protected IEnumerable<IChannel> EnumerateSubchannels()
    {
        foreach (var subchannel in _subchannels)
        {
            yield return subchannel;
        }
    }

    protected void RaisePositionUpdated(TimeSpan value, bool force)
    {
        if (!force && DateTime.Now - _lastPositionUpdateTime < AutoRefreshInterval) return;
        Engine.Context.Send(_ => PositionUpdated?.Invoke(value), null);
        _lastPositionUpdateTime = DateTime.Now;
    }

    private async Task CancelTask()
    {
        if (_playTask is null ||
            _playTask.Status == TaskStatus.Canceled ||
            _playTask.Status == TaskStatus.Faulted)
            return;

        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        await TaskEx.WhenAllSkipNull(_playTask);
    }

    private void SetTime(TimeSpan value)
    {
        _innerTimelineSw.SkipTo(value);
    }

    private async Task InnerSync()
    {
        var refer = _runningChannels.FirstOrDefault(k => k.IsReferenced);
        if (refer is null)
        {
            foreach (var channel in _runningChannels.ToList())
            {
                await channel.Sync(Position - channel.ChannelStartTime);
            }
        }
        else
        {
            var referChannelStartTime = refer.ChannelStartTime + refer.Position;
            //Console.WriteLine(referChannelStartTime);
            foreach (var channel in _runningChannels.Where(k => !k.IsReferenced).ToList())
            {
                var targetTime = referChannelStartTime - channel.ChannelStartTime;
                //targetTime = channel.Position +
                //             TimeSpan.FromMilliseconds((targetTime - channel.Position).TotalMilliseconds / 2);
                await channel.Sync(targetTime);
            }

            _innerTimelineSw.SkipTo(referChannelStartTime);
            RaisePositionUpdated(_innerTimelineSw.Elapsed, false);
        }
    }

    private async Task RequeueChannel()
    {
        _channelsQueue = new ConcurrentQueue<Subchannel>(_subchannels
            .Where(k => k.ChannelStartTime > Position)
            .OrderBy(k => k.ChannelStartTime)
        );

        foreach (var subchannel in _channelsQueue)
        {
            if (Position < subchannel.ChannelStartTime)
            {
                await subchannel.Stop();
            }
        }

        var old = _runningChannels.ToList();
        _runningChannels = new SortedSet<Subchannel>(_subchannels
                .Where(k => k.ChannelStartTime <= Position && k.ChannelEndTime > Position),
            new ChannelEndTimeComparer());
        foreach (var subchannel in old)
        {
            if (_runningChannels.Contains(subchannel)) continue;
            //if (subchannel.ChannelStartTime < Position)
            //{
            //    await subchannel.Stop();
            //}
            //else
            //{
            await subchannel.Pause();
            //}
        }
    }

    private bool EnsureSoundElementsLoaded()
    {
        var position = Position;
        foreach (var subchannel in Subchannels)
        {
            if (!(subchannel is MultiElementsChannel mec)) continue;
            var hitsounds = mec.SoundElementCollection
                .Where(k => k.FilePath != null)
                .Where(k => k.Offset >= position.TotalMilliseconds &&
                            k.Offset <= position.TotalMilliseconds + 3000);
            if (hitsounds.Any(k => !CachedSoundFactory.ContainsCache(k.FilePath)))
            {
                return false;
            }
        }

        return true;
    }

    protected async Task BufferSoundElementsAsync()
    {
        var position = Position;
        foreach (var subchannel in Subchannels)
        {
            if (subchannel is not MultiElementsChannel mec) continue;
            var hitsounds = mec.SoundElementCollection
                .Where(k => k.FilePath != null)
                .Where(k => k.Offset >= position.TotalMilliseconds &&
                            k.Offset <= position.TotalMilliseconds + 6000);
            foreach (var soundElement in hitsounds.Where(k => k.FilePath != null))
            {
                await CachedSoundFactory.GetOrCreateCacheSound(Engine.FileWaveFormat, soundElement.FilePath!);
            }
        }
    }
}