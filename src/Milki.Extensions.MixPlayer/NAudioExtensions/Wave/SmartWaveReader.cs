using System;
using System.IO;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Vorbis;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public class SmartWaveReader : WaveStream, ISampleProvider
{
    private readonly NAudio.Wave.SampleProviders.SampleChannel _sampleChannel;
    private readonly int _destBytesPerSample;
    private readonly int _sourceBytesPerSample;
    private readonly long _length;
    private readonly object _lockObject;
    private readonly Stream _stream;
    private bool _isDisposed;
    private WaveStream _readerStream = null!;

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
        _stream.Seek(0, SeekOrigin.Begin);
        if (_stream is FileStream fs)
        {
            FileName = fs.Name;
        }
        else
        {
            FileName = "stream";
        }

        CreateReaderStream(_stream);
        _sourceBytesPerSample = (ReaderStream.WaveFormat.BitsPerSample / 8) * ReaderStream.WaveFormat.Channels;
        _sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(ReaderStream, false);
        _destBytesPerSample = 4 * _sampleChannel.WaveFormat.Channels;
        _length = SourceToDest(ReaderStream.Length);
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
    /// Actual based WaveStream 
    /// </summary>
    public WaveStream ReaderStream
    {
        get
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ReaderStream));
            return _readerStream;
        }
        private set
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ReaderStream));
            _readerStream = value;
        }
    }

    /// <summary>
    /// Position of this stream (in bytes)
    /// </summary>
    public override long Position
    {
        get
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ReaderStream));
            return SourceToDest(ReaderStream.Position);
        }
        set
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ReaderStream));
            lock (_lockObject) ReaderStream.Position = DestToSource(value);
        }
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
        if (disposing && ReaderStream != null)
        {
            ReaderStream.Dispose();
            ReaderStream = null;
            _stream.Dispose();
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }

    private void CreateReaderStream(Stream sourceStream)
    {
        var fileFormat = FileFormatHelper.GetFileFormatFromStream(sourceStream);
        sourceStream.Seek(0, SeekOrigin.Begin);

        if (fileFormat == FileFormat.Wav)
        {
            ReaderStream = new WaveFileReader(sourceStream);
            if (ReaderStream.WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
                return;
            ReaderStream = WaveFormatConversionStream.CreatePcmStream(ReaderStream);
            ReaderStream = new BlockAlignReductionStream(ReaderStream);
        }
        else if (fileFormat == FileFormat.Mp3)
        {
            ReaderStream = new NLayerMp3FileReader(sourceStream);
        }
        else if (fileFormat == FileFormat.Ogg)
        {
            ReaderStream = new VorbisWaveReader(sourceStream);
        }
        else if (fileFormat == FileFormat.Aiff)
        {
            ReaderStream = new AiffFileReader(sourceStream);
        }
        else
        {
            ReaderStream = new StreamMediaFoundationReader(sourceStream);
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
