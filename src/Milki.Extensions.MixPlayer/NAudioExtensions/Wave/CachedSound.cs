using System;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public struct CachedSound : IEquatable<CachedSound>
{
    public string SourcePath { get; }
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }
    public TimeSpan Duration { get; }

    internal CachedSound(string filePath, float[] audioData, TimeSpan duration, WaveFormat waveFormat)
    {
        SourcePath = filePath;
        AudioData = audioData;
        Duration = duration;
        WaveFormat = waveFormat;
    }

    public override bool Equals(object? obj)
    {
        if (obj is CachedSound other)
            return Equals(other);
        return ReferenceEquals(this, obj);
    }

    public bool Equals(CachedSound other)
    {
        return SourcePath == other.SourcePath;
    }

    public override int GetHashCode()
    {
        return SourcePath.GetHashCode();
    }
}