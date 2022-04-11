using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer;

[Fody.ConfigureAwait(false)]
public abstract class ExporterBase
{
    private readonly ICollection<MultiElementsChannel> _channels;
    private readonly AudioPlaybackEngine _engine;
    private readonly WaveFloatTo16Provider _sourceProvider;
    protected WaveFormat WaveFormat { get; }

    public ExporterBase(MultiElementsChannel channel, AudioPlaybackEngine engine) : this(new[] { channel }, engine)
    {
    }

    public ExporterBase(IEnumerable<MultiElementsChannel> channels, AudioPlaybackEngine engine)
    {
        _channels = channels.ToArray();
        _engine = engine;

        var sourceProvider = _engine.Root.ToWaveProvider();
        _sourceProvider = new WaveFloatTo16Provider(sourceProvider);
        WaveFormat = _sourceProvider.WaveFormat;
    }

    public abstract Task ExportAsync(string filepath, Action<double>? progressCallback = null);

    protected async Task ExportCoreAsync(Func<byte[], int, int, Task> dataProcessed, Action<double> progressCallback)
    {
        if (dataProcessed == null) throw new ArgumentNullException(nameof(dataProcessed));

        foreach (var subchannel in _channels)
        {
            await subchannel.Initialize();
        }

        var maxEndTime = _channels.Count == 0 ? TimeSpan.Zero : _channels.Max(k => k.ChannelEndTime);

        double? p = null;

        _engine.Updated += (_, _, timestamp) =>
        {
            foreach (var subchannel in _channels)
            {
                try
                {
                    subchannel.SelectElements((int)timestamp.TotalMilliseconds).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

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
                {
                    if (subchannel.Submixer != null)
                    {
                        subchannel.Submixer.ReadFully = false;
                    }
                }
            }
        };

        var buffer = new byte[128];
        while (true)
        {
            int count = _sourceProvider.Read(buffer, 0, buffer.Length);
            if (count != 0)
                await dataProcessed(buffer, 0, count);
            else
                break;
        }
    }
}