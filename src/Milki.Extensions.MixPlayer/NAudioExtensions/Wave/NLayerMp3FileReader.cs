using System.IO;
using NAudio.Wave;
using NLayer.NAudioSupport;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

/// <summary>
/// Class for reading from MP3 files
/// </summary>
public class NLayerMp3FileReader : Mp3FileReaderBase
{
    /// <summary>Supports opening a MP3 file</summary>
    public NLayerMp3FileReader(string mp3FileName)
        : base(File.OpenRead(mp3FileName), CreateAcmFrameDecompressor, true)
    {
    }

    /// <summary>
    /// Opens MP3 from a stream rather than a file
    /// Will not dispose of this stream itself
    /// </summary>
    /// <param name="inputStream">The incoming stream containing MP3 data</param>
    public NLayerMp3FileReader(Stream inputStream)
        : base(inputStream, CreateAcmFrameDecompressor, false)
    {

    }

    private static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format)
    {
        return new Mp3FrameDecompressor(mp3Format);
    }
}