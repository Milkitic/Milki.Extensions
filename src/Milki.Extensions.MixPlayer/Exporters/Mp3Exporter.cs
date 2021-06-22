using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Lame;

namespace Milki.Extensions.MixPlayer.Exporters
{
    public class Mp3Exporter : ExporterBase
    {
        public Mp3Exporter(MultiElementsChannel channel, AudioPlaybackEngine engine)
            : base(channel, engine)
        {
        }

        public Mp3Exporter(IEnumerable<MultiElementsChannel> channels, AudioPlaybackEngine engine)
            : base(channels, engine)
        {
        }

        public override async Task ExportAsync(string filepath, Action<double>? progressCallback = null)
        {
            await ExportAsync(filepath, 320000, null, progressCallback);
        }

        public async Task ExportAsync(string filepath, int bitRate = 320000, ID3TagData? id3 = null,
            Action<double>? progressCallback = null)
        {
            await using var outStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            await using Stream writer = new LameMP3FileWriter(outStream, WaveFormat, bitRate / 1000, id3);
            await ExportCoreAsync(async (bytes, offset, count) =>
            {
                await writer.WriteAsync(bytes, offset, count);
            }, d => progressCallback?.Invoke(d));

            await outStream.FlushAsync();
        }
    }
}
