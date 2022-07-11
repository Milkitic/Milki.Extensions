#nullable disable
using System;
using System.IO;
using File = TagLib.File;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

public class StreamAbstraction : File.IFileAbstraction
{
    private readonly Stream _sourceStream;
    private readonly bool _closeStream;

    public StreamAbstraction(Stream sourceStream, bool closeStream = false)
    {
        _sourceStream = sourceStream;
        _closeStream = closeStream;
    }

    public string Name => _sourceStream is FileStream fs ? fs.Name : "stream";

    public Stream ReadStream => _sourceStream;
    public Stream WriteStream => _sourceStream;

    public void CloseStream(Stream stream)
    {
        if (!_closeStream) return;
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        stream.Close();
    }
}