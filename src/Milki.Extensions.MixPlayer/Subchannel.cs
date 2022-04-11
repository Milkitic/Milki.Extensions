using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions;

namespace Milki.Extensions.MixPlayer;

[Fody.ConfigureAwait(false)]
public abstract class Subchannel : IChannel
{
    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;
    public virtual float Volume { get => SampleControl.Volume; set => SampleControl.Volume = value; }

    private PlayStatus _playStatus;
    private TimeSpan _position;
    private DateTime _lastPositionUpdateTime;
    public TimeSpan AutoRefreshInterval { get; protected set; } = TimeSpan.FromMilliseconds(500);

    protected AudioPlaybackEngine Engine { get; }
    public ICollection<Subchannel> ReferencedChannels { get; }

    public SampleControl SampleControl { get; } = new();

    public Subchannel(AudioPlaybackEngine engine, ICollection<Subchannel>? referencedChannels = null)
    {
        Engine = engine;
        ReferencedChannels = referencedChannels ?? Array.Empty<Subchannel>();
    }

    public abstract TimeSpan ChannelStartTime { get; }
    public TimeSpan ChannelEndTime => ChannelStartTime + Duration;

    public virtual string Description { get; set; } = "Subchannel";

    public abstract TimeSpan Duration { get; protected set; }

    public virtual TimeSpan Position
    {
        get => _position;
        protected set => _position = value;
    }

    protected void RaisePositionUpdated(TimeSpan value, bool force)
    {
        if (!force && DateTime.Now - _lastPositionUpdateTime < AutoRefreshInterval) return;
        Engine.Context.Send(_ => PositionUpdated?.Invoke(value), null);
        _lastPositionUpdateTime = DateTime.Now;
    }

    public abstract float PlaybackRate { get; protected set; }
    public abstract bool KeepTune { get; protected set; }

    public bool IsReferenced { get; set; }

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

    public abstract Task Initialize();

    public abstract Task Play();

    public abstract Task Pause();

    public abstract Task Stop();

    public abstract Task Restart();

    public abstract Task SkipTo(TimeSpan time);

    public abstract Task Sync(TimeSpan time);

    public abstract Task SetPlaybackRate(float rate, bool keepTune);

    public virtual async ValueTask DisposeAsync()
    {
        //Engine?.Dispose();
        await Task.CompletedTask;
    }
}