using System;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public class TimingSampleProvider : ISampleProvider
{
    public delegate void TimingChangedEvent(TimeSpan oldTimestamp, TimeSpan newTimestamp);

    public event TimingChangedEvent? Updated;

    private readonly ISampleProvider _sourceProvider;

    public TimingSampleProvider(ISampleProvider sourceProvider)
    {
        _sourceProvider = sourceProvider;
    }

    public WaveFormat WaveFormat => _sourceProvider.WaveFormat;
    public TimeSpan CurrentTime { get; private set; } = TimeSpan.Zero;

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _sourceProvider.Read(buffer, offset, count);
        var oldTime = CurrentTime;
        CurrentTime += SamplesToTimeSpan(samplesRead);
        if (oldTime != CurrentTime)
        {
            Updated?.Invoke(oldTime, CurrentTime);
        }

        return samplesRead;
    }

    private int TimeSpanToSamples(TimeSpan time)
    {
        var samples = (int)(time.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
        return samples;
    }

    private TimeSpan SamplesToTimeSpan(int samples)
    {
        return WaveFormat.Channels switch
        {
            1 => TimeSpan.FromSeconds((samples) / (double)WaveFormat.SampleRate),
            2 => TimeSpan.FromSeconds((samples >> 1) / (double)WaveFormat.SampleRate),
            4 => TimeSpan.FromSeconds((samples >> 2) / (double)WaveFormat.SampleRate),
            8 => TimeSpan.FromSeconds((samples >> 3) / (double)WaveFormat.SampleRate),
            _ => TimeSpan.FromSeconds((samples / WaveFormat.Channels) / (double)WaveFormat.SampleRate)
        };
    }
}