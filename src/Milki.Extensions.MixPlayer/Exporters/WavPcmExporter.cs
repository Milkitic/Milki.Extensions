﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Vorbis;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.Exporters
{
    public class WavPcmExporter : ExporterBase
    {
        public WavPcmExporter(MultiElementsChannel channel, AudioPlaybackEngine engine)
            : base(channel, engine)
        {
        }

        public WavPcmExporter(IEnumerable<MultiElementsChannel> channels, AudioPlaybackEngine engine)
            : base(channels, engine)
        {
        }

        public override async Task ExportAsync(string filepath, Action<double>? progressCallback = null)
        {
            await ExportAsync(filepath, 320000, progressCallback);
        }

        public async Task ExportAsync(string filepath, int bitRate = 320000, Action<double>? progressCallback = null)
        {
            await using var outStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            await using var writer = new WaveFileWriter(outStream, WaveFormat);
            await ExportCoreAsync(async (bytes, offset, count) =>
            {
                await writer.WriteAsync(bytes, offset, count);
            }, d => progressCallback?.Invoke(d));

            await outStream.FlushAsync();
        }
    }
}