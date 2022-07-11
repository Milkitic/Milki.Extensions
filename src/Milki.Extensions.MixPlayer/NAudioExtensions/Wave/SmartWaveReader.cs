using System;
using System.IO;
using NAudio.Vorbis;
using NAudio.Wave;
using TagLib;
using File = System.IO.File;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public class SmartWaveReader : WaveStream, ISampleProvider
{
    private WaveStream? _readerStream;
    private readonly NAudio.Wave.SampleProviders.SampleChannel _sampleChannel;
    private readonly int _destBytesPerSample;
    private readonly int _sourceBytesPerSample;
    private readonly long _length;
    private readonly object _lockObject;
    private readonly Stream _stream;

    public SmartWaveReader(string fileName)
        : this(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
    }

    public SmartWaveReader(byte[] buffer) : this(new MemoryStream(buffer))
    {
    }

    public SmartWaveReader(Stream stream)
    {
        _lockObject = new object();
        _stream = stream.CanSeek ? stream : new ReadSeekableStream(stream, 4096);
        if (_stream is FileStream fs)
        {
            FileName = fs.Name;
        }
        else
        {
            FileName = "stream";
        }

        CreateReaderStream(_stream);
        _sourceBytesPerSample = (_readerStream!.WaveFormat.BitsPerSample / 8) * _readerStream.WaveFormat.Channels;
        _sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(_readerStream, false);
        _destBytesPerSample = 4 * _sampleChannel.WaveFormat.Channels;
        _length = SourceToDest(_readerStream.Length);
    }

    /// <summary>
    /// File Name
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// WaveFormat of this stream
    /// </summary>
    public override WaveFormat WaveFormat => _sampleChannel.WaveFormat;

    /// <summary>
    /// Length of this stream (in bytes)
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// Position of this stream (in bytes)
    /// </summary>
    public override long Position
    {
        get => SourceToDest(_readerStream!.Position);
        set { lock (_lockObject) _readerStream!.Position = DestToSource(value); }
    }

    public float Volume
    {
        get => _sampleChannel.Volume;
        set => _sampleChannel.Volume = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var waveBuffer = new WaveBuffer(buffer);
        int samplesRequired = count >> 2;
        return Read(waveBuffer.FloatBuffer, offset >> 2, samplesRequired) << 2;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (_lockObject)
        {
            return _sampleChannel.Read(buffer, offset, count);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _readerStream != null)
        {
            _readerStream.Dispose();
            _readerStream = null;
            _stream.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CreateReaderStream(Stream sourceStream)
    {
        var file = TagLib.File.Create(new StreamAbstraction(sourceStream));
        sourceStream.Seek(0, SeekOrigin.Begin);
        var prop = file.Properties;
        if (prop == null)
        {
            _readerStream = new StreamMediaFoundationReader(sourceStream);
            return;
        }

        if (prop.MediaTypes != MediaTypes.Audio)
        {
            throw new Exception("Input is not a valid audio stream.");
        }

        if (prop.Description.IndexOf("PCM", StringComparison.Ordinal) != -1)
        {
            _readerStream = new WaveFileReader(sourceStream);
            if (_readerStream.WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
                return;
            _readerStream = WaveFormatConversionStream.CreatePcmStream(_readerStream);
            _readerStream = new BlockAlignReductionStream(_readerStream);
        }
        else if (prop.Description.IndexOf("Layer 3", StringComparison.Ordinal) != -1)
        {
            _readerStream = new Mp3FileReader(sourceStream);
        }
        else if (prop.Description.IndexOf("Vorbis", StringComparison.Ordinal) != -1)
        {
            _readerStream = new VorbisWaveReader(sourceStream);
        }
        else if (prop.Description.IndexOf("AIFF", StringComparison.Ordinal) != -1)
        {
            _readerStream = new AiffFileReader(sourceStream);
        }
        else
        {
            _readerStream = new StreamMediaFoundationReader(sourceStream);
        }
    }

    /// <summary>
    /// Helper to convert source to dest bytes
    /// </summary>
    private long SourceToDest(long sourceBytes)
    {
        return _sourceBytesPerSample switch
        {
            1 => _destBytesPerSample * (sourceBytes),
            2 => _destBytesPerSample * (sourceBytes >> 1),
            4 => _destBytesPerSample * (sourceBytes >> 2),
            8 => _destBytesPerSample * (sourceBytes >> 3),
            _ => _destBytesPerSample * (sourceBytes / _sourceBytesPerSample)
        };
    }

    /// <summary>
    /// Helper to convert dest to source bytes
    /// </summary>
    private long DestToSource(long destBytes)
    {
        return _destBytesPerSample switch
        {
            1 => _sourceBytesPerSample * (destBytes),
            2 => _sourceBytesPerSample * (destBytes >> 1),
            4 => _sourceBytesPerSample * (destBytes >> 2),
            8 => _sourceBytesPerSample * (destBytes >> 3),
            _ => _sourceBytesPerSample * (destBytes / _destBytesPerSample)
        };
    }
}