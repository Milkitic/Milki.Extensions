using System;
using System.IO;

namespace Milki.Extensions.MixPlayer.Utilities;

// https://en.wikipedia.org/wiki/List_of_file_signatures
public static class WaveTypeHelper
{
    private static readonly byte[] AIFF =
    {
        0x46, 0x4F, 0x52, 0x4D,
        0, 0, 0, 0,
        0x41, 0x49, 0x46, 0x46
    };

    private static readonly byte[] WAV =
    {
        0x52, 0x49, 0x46, 0x46,
        0, 0, 0, 0,
        0x57, 0x41, 0x56, 0x45
    };

    private static readonly byte[] OGG =
    {
        0x4F, 0x67, 0x67, 0x53,
    };

    private static readonly byte?[] MP3_1 = { 0xFF, 0xFB };
    private static readonly byte?[] MP3_2 = { 0xFF, 0xF3 };
    private static readonly byte?[] MP3_3 = { 0xFF, 0xF2 };
    private static readonly byte[] MP3_ID3 = { 0x49, 0x44, 0x33 };

    public static WaveType GetWaveTypeFromStream(Stream sourceStream)
    {
        var firstByte = sourceStream.ReadByte();
        if (firstByte == 0x46)
        {
            Span<byte> span = stackalloc byte[3];
            var readBytes = sourceStream.Read(span);
            if (readBytes < 3) return WaveType.Others;
            if (!span.SequenceEqual(AIFF.AsSpan(1, 3)))
                return WaveType.Others;

            readBytes = sourceStream.Read(stackalloc byte[4]);
            if (readBytes < 4) return WaveType.Others;

            span = stackalloc byte[4];
            readBytes = sourceStream.Read(span);
            if (readBytes < 4) return WaveType.Others;
            if (!span.SequenceEqual(AIFF.AsSpan(8, 4)))
                return WaveType.Others;

            return WaveType.Wav;
        }

        if (firstByte == 0x52)
        {
            Span<byte> span = stackalloc byte[3];
            var readBytes = sourceStream.Read(span);
            if (readBytes < 3) return WaveType.Others;
            if (!span.SequenceEqual(WAV.AsSpan(1, 3)))
                return WaveType.Others;

            readBytes = sourceStream.Read(stackalloc byte[4]);
            if (readBytes < 4) return WaveType.Others;

            span = stackalloc byte[4];
            readBytes = sourceStream.Read(span);
            if (readBytes < 4) return WaveType.Others;
            if (!span.SequenceEqual(WAV.AsSpan(8, 4)))
                return WaveType.Others;

            return WaveType.Wav;
        }

        if (firstByte == 0x4F)
        {
            Span<byte> span = stackalloc byte[3];
            var readBytes = sourceStream.Read(span);
            if (readBytes < 3) return WaveType.Others;
            if (!span.SequenceEqual(OGG.AsSpan(1)))
                return WaveType.Others;

            return WaveType.Ogg;
        }

        if (firstByte == 0x49)
        {
            Span<byte> span = stackalloc byte[2];
            var readBytes = sourceStream.Read(span);
            if (readBytes < 2) return WaveType.Others;
            if (!span.SequenceEqual(MP3_ID3.AsSpan(1)))
                return WaveType.Others;

            return WaveType.Mp3;
        }

        if (firstByte == 0xFF)
        {
            var nextByte = sourceStream.ReadByte();
            if (nextByte is not (0xFB or 0xF3 or 0xF2 or 0xE2))
                return WaveType.Others;

            return WaveType.Mp3;
        }

        return WaveType.Others;
    }
}