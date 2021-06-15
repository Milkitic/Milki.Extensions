using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Lame;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer
{
    public class Mp3Exporter
    {
        private readonly ICollection<MultiElementsChannel> _channels;
        private readonly AudioPlaybackEngine _engine;

        public Mp3Exporter(Func<MultiElementsChannel> channel) : this(new[] { channel })
        {
        }

        public Mp3Exporter(IEnumerable<Func<MultiElementsChannel>> channels)
        {
            _channels = channels.Select(k => k()).ToArray();
            _engine = new AudioPlaybackEngine();
        }

        public async Task ExportAsync(string filepath, int bitRate,
            ID3TagData? id3 = null,
            Action<double>? progressCallback = null)
        {
            foreach (var subchannel in _channels)
            {
                await subchannel.Initialize();
            }

            var maxEndTime = _channels.Count == 0 ? TimeSpan.Zero : _channels.Max(k => k.ChannelEndTime);

            double? p = null;

            _engine.Updated += (_, _, timestamp) =>
            {
                foreach (var subchannel in _channels)
                    subchannel.SelectElements((int)timestamp.TotalMilliseconds).Wait();

                var progress = timestamp.TotalMilliseconds / maxEndTime.TotalMilliseconds;
                if (!p.Equals(progress))
                {
                    progressCallback?.Invoke(progress);
                    p = progress;
                }

                if (timestamp > maxEndTime)
                {
                    _engine.RootMixer.ReadFully = false;
                    foreach (var subchannel in _channels)
                        if (subchannel.Submixer != null)
                            subchannel.Submixer.ReadFully = false;
                }
            };

            var sourceProvider = _engine.Root.ToWaveProvider();
            sourceProvider = new WaveFloatTo16Provider(sourceProvider);

            await using var outStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            await using var writer = new LameMP3FileWriter(outStream, sourceProvider.WaveFormat, bitRate, id3);

            var buffer = new byte[128];
            while (true)
            {
                int count = sourceProvider.Read(buffer, 0, buffer.Length);
                if (count != 0)
                    await writer.WriteAsync(buffer, 0, count);
                else
                    break;
            }

            outStream.Flush();
        }
    }
}
