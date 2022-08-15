﻿// Modified from https://github.com/naudio/NAudio/blob/56e9419325d20524cf749ed362ada5066178feaa/NAudio/Wave/SampleProviders/VolumeSampleProvider.cs

#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

using System;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

/// <summary>
/// Very simple sample provider supporting adjustable gain
/// </summary>
public class EnhancedVolumeSampleProvider : ISampleProvider
{
    /// <summary>
    /// Initializes a new instance of VolumeSampleProvider
    /// </summary>
    /// <param name="source">Source Sample Provider</param>
    public EnhancedVolumeSampleProvider(ISampleProvider source)
    {
        Source = source;
        Volume = 1.0f;
    }

    /// <summary>
    /// Source Sample Provider
    /// </summary>
    public ISampleProvider? Source { get; set; }

    /// <summary>
    /// WaveFormat
    /// </summary>
    public WaveFormat WaveFormat => Source?.WaveFormat ?? throw new InvalidOperationException("Source not ready");

    /// <summary>
    /// Reads samples from this sample provider
    /// </summary>
    /// <param name="buffer">Sample buffer</param>
    /// <param name="offset">Offset into sample buffer</param>
    /// <param name="sampleCount">Number of samples desired</param>
    /// <returns>Number of samples read</returns>
    public int Read(float[] buffer, int offset, int sampleCount)
    {
        if (Source == null)
        {
            Array.Clear(buffer, offset, sampleCount);
            return sampleCount;
        }

        int samplesRead = Source.Read(buffer, offset, sampleCount);
        if (Volume != 1f)
        {
#if NETCOREAPP3_1_OR_GREATER
            FastPath(buffer, offset, sampleCount);
#else
            for (int n = 0; n < sampleCount; n++)
            {
                buffer[offset + n] *= Volume;
            }
#endif
        }
        return samplesRead;
    }

    /// <summary>
    /// Allows adjusting the volume, 1.0f = full volume
    /// </summary>
    public float Volume { get; set; }

#if NETCOREAPP3_1_OR_GREATER
    private unsafe void FastPath(float[] buffer, int offset, int sampleCount)
    {
        fixed (float* b = buffer)
        {
            var pStart = b + offset;
            var pCurrent = pStart;
            var pEnd = pStart;

            if (Avx.IsSupported)
            {
                var volume = Vector256.Create(Volume);
                var vector256SampleCount = sampleCount & ~7;
                pEnd = pStart + vector256SampleCount;
                while (pCurrent < pEnd)
                {
                    var input = Avx.LoadVector256(pCurrent);
                    var output = Avx.Multiply(input, volume);
                    Avx.Store(pCurrent, output);
                    pCurrent += 8;
                }
            }

            if (Sse.IsSupported)
            {
                var volume = Vector128.Create(Volume);
                var vector128SampleCount = sampleCount & ~3;
                pEnd = pStart + vector128SampleCount;
                while (pCurrent < pEnd)
                {
                    var input = Sse.LoadVector128(pCurrent);
                    var output = Sse.Multiply(input, volume);
                    Sse.Store(pCurrent, output);
                    pCurrent += 4;
                }
            }

            pEnd = pStart + sampleCount;
            while (pCurrent < pEnd)
            {
                *pCurrent *= Volume;
                pCurrent++;
            }
        }
    }
#endif
}