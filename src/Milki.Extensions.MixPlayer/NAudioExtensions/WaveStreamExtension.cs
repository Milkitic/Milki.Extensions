using System;
using System.Buffers;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions;

public static class WaveStreamExtension
{
    public static float[] ToIeeeSampleBytes(this WaveStream waveStream)
    {
        if (waveStream is not ISampleProvider sampleProvider)
            sampleProvider = new NAudio.Wave.SampleProviders.SampleChannel(waveStream, false);

        var wholeData = new float[(int)(waveStream.Length / 4)];
        var actualWaveFormat = waveStream.WaveFormat;

        var length = actualWaveFormat.SampleRate * actualWaveFormat.Channels;
        var readBuffer = ArrayPool<float>.Shared.Rent(length);
        try
        {
            int samplesRead;
            int offset = 0;
            while ((samplesRead = sampleProvider.Read(readBuffer, 0, length)) > 0)
            {
                var read = offset + samplesRead;
                if (wholeData.Length < read)
                {
                    var realloc = new float[read];
                    wholeData.CopyTo(realloc, 0);
                    wholeData = realloc;
                }

                readBuffer.AsSpan(0, samplesRead).CopyTo(wholeData.AsSpan(offset, samplesRead));
                offset += samplesRead;
            }

            return wholeData;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(readBuffer);
        }
    }
}