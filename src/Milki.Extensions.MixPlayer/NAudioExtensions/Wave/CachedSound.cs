using System;
using System.Runtime.CompilerServices;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public sealed class CachedSound : IEquatable<CachedSound>
{
    public readonly string SourcePath;
    public readonly float[] AudioData;
    public readonly WaveFormat WaveFormat;

    internal CachedSound(string filePath, float[] audioData, WaveFormat waveFormat)
    {
        SourcePath = filePath;
        AudioData = audioData;
        WaveFormat = waveFormat;
    }

    public TimeSpan Duration => SamplesToTimeSpan(AudioData.Length);

    public bool Equals(CachedSound other)
    {
        return SourcePath == other.SourcePath;
    }

    public override bool Equals(object? obj)
    {
        return obj is CachedSound other && Equals(other);
    }

    public override int GetHashCode()
    {
        return SourcePath.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimeSpan SamplesToTimeSpan(int samples)
    {
        if (WaveFormat.Channels == 1)
            return TimeSpan.FromSeconds((samples) / (double)WaveFormat.SampleRate);
        if (WaveFormat.Channels == 2)
            return TimeSpan.FromSeconds((samples >> 1) / (double)WaveFormat.SampleRate);
        if (WaveFormat.Channels == 4)
            return TimeSpan.FromSeconds((samples >> 2) / (double)WaveFormat.SampleRate);
        if (WaveFormat.Channels == 8)
            return TimeSpan.FromSeconds((samples >> 3) / (double)WaveFormat.SampleRate);
        return TimeSpan.FromSeconds((samples / WaveFormat.Channels) / (double)WaveFormat.SampleRate);
    }
}