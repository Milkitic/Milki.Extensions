using System.IO;

namespace Milki.Extensions.MixPlayer.Utilities;

internal class StreamFileAbstraction : TagLib.File.IFileAbstraction
{
    private readonly bool _closeStream;

    public StreamFileAbstraction(Stream sourceStream, string name, bool closeStream = false)
    {
        _closeStream = closeStream;
        ReadStream = sourceStream;
        WriteStream = sourceStream;
        Name = name;
    }

    public void CloseStream(Stream stream)
    {
        if (_closeStream)
        {
            stream.Dispose();
        }
    }

    public string Name { get; set; }
    public Stream ReadStream { get; set; }
    public Stream WriteStream { get; set; }
}