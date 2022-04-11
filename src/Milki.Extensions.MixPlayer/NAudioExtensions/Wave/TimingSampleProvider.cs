﻿using System;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public class TimingSampleProvider : ISampleProvider
{
    public delegate void TimingChangedEvent(TimeSpan oldTimestamp, TimeSpan newTimestamp);

    private readonly ISampleProvider _sourceProvider;
    public TimeSpan CurrentTime { get; private set; } = TimeSpan.Zero;

    public event TimingChangedEvent? Updated;

    public TimingSampleProvider(ISampleProvider sourceProvider)
    {
        _sourceProvider = sourceProvider;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _sourceProvider.Read(buffer, offset, count);
        var oldTime = CurrentTime;
        CurrentTime += SamplesToTimeSpan(samplesRead);
        if (oldTime != CurrentTime)
            Updated?.Invoke(oldTime, CurrentTime);
        return samplesRead;
    }

    public WaveFormat WaveFormat => _sourceProvider.WaveFormat;

    private int TimeSpanToSamples(TimeSpan time)
    {
        var samples = (int)(time.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
        return samples;
    }

    private TimeSpan SamplesToTimeSpan(int samples)
    {
        return TimeSpan.FromSeconds((samples / WaveFormat.Channels) / (double)WaveFormat.SampleRate);
    }
}