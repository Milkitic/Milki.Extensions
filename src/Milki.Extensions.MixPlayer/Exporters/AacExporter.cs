using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.Exporters
{
    public class AacExporter : ExporterBase
    {
        public AacExporter(MultiElementsChannel channel, AudioPlaybackEngine engine)
            : base(channel, engine)
        {
        }

        public AacExporter(IEnumerable<MultiElementsChannel> channels, AudioPlaybackEngine engine)
            : base(channels, engine)
        {
        }

        public override async Task ExportAsync(string filepath, Action<double>? progressCallback = null)
        {
            await ExportAsync(filepath, 320000, progressCallback);
        }

        public async Task ExportAsync(string filepath, int bitRate = 320000, Action<double>? progressCallback = null)
        {
            await using var outStream = new MemoryStream();

            var waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Adpcm, WaveFormat.SampleRate, WaveFormat.Channels,
                WaveFormat.AverageBytesPerSecond, WaveFormat.BlockAlign, WaveFormat.BitsPerSample);
            await using var writer = new WaveFileWriter(outStream, waveFormat);
            await ExportCoreAsync(async (bytes, offset, count) =>
            {
                await writer.WriteAsync(bytes, offset, count);
            }, d => progressCallback?.Invoke(d));

            outStream.Position = 0;
            await using var reader = new WaveFileReader(outStream);
            MediaFoundationEncoder.EncodeToAac(reader, filepath, bitRate);
        }
    }
}