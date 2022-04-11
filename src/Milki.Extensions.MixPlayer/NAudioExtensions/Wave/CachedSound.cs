using System;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public sealed class CachedSound : IEquatable<CachedSound>
{
    public readonly string SourcePath;
    public readonly float[] AudioData;
    public readonly TimeSpan Duration;
    public readonly WaveFormat WaveFormat;

    internal CachedSound(string filePath, float[] audioData, TimeSpan duration, WaveFormat waveFormat)
    {
        SourcePath = filePath;
        AudioData = audioData;
        Duration = duration;
        WaveFormat = waveFormat;
    }

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
}