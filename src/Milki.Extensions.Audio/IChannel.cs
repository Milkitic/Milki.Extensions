﻿using System;
using System.Threading.Tasks;

namespace Milki.Extensions.Audio
{
    public interface IChannel : IAsyncDisposable
    {
        event Action<PlayStatus> PlayStatusChanged;
        event Action<TimeSpan> PositionUpdated;

        float Volume { get; set; }

        string Description { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; }
        float PlaybackRate { get; }
        bool UseTempo { get; }
        PlayStatus PlayStatus { get; }

        Task Initialize();
        Task Play();
        Task Pause();
        Task Stop();
        Task Restart();
        Task SkipTo(TimeSpan time);
        Task SetPlaybackRate(float rate, bool keepTune);
    }
}