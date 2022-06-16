using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.NAudioExtensions;

public sealed class AudioPlaybackEngine : IDisposable
{
    public delegate void PlaybackTimingChangedEvent(AudioPlaybackEngine sender, TimeSpan oldTimestamp, TimeSpan newTimestamp);

    public event PlaybackTimingChangedEvent? Updated;

    private readonly VolumeSampleProvider _volumeProvider;
    private readonly TimingSampleProvider _timingProvider;

    public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
    {
        Context = SynchronizationContext.Current ??
                  new StaSynchronizationContext("AudioPlaybackEngine_STA");
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
        FileWaveFormat = new WaveFormat(sampleRate, channelCount);
        RootMixer = new MixingSampleProvider(WaveFormat)
        {
            ReadFully = true
        };
        _volumeProvider = new VolumeSampleProvider(RootMixer);
        _timingProvider = new TimingSampleProvider(_volumeProvider);
        _timingProvider.Updated += (a, b) =>
        {
            Context.Send(_ => Updated?.Invoke(this, a, b), null);
        };
    }

    public AudioPlaybackEngine(DeviceDescription? deviceDescription, int sampleRate = 44100, int channelCount = 2)
    {
        Context = SynchronizationContext.Current ??
                  new StaSynchronizationContext("AudioPlaybackEngine_STA");
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
        FileWaveFormat = new WaveFormat(sampleRate, channelCount);

        RootMixer = new MixingSampleProvider(WaveFormat)
        {
            ReadFully = true
        };
        _volumeProvider = new VolumeSampleProvider(RootMixer);
        _timingProvider = new TimingSampleProvider(_volumeProvider);
        _timingProvider.Updated += (a, b) =>
        {
            Context.Send(_ => Updated?.Invoke(this, a, b), null);
        };
        OutputDevice = DeviceCreationHelper.CreateDevice(out var actualDeviceInfo, deviceDescription, Context);
        Context.Send(_ => OutputDevice.Init(_timingProvider), null);
        OutputDevice.Play();
    }

    public WaveFormat FileWaveFormat { get; set; }
    public MixingSampleProvider RootMixer { get; }
    public ISampleProvider Root => _timingProvider;
    public IWavePlayer? OutputDevice { get; }
    public SynchronizationContext Context { get; set; }

    public float RootVolume
    {
        get => _volumeProvider.Volume;
        set => _volumeProvider.Volume = value;
    }

    public WaveFormat WaveFormat { get; }

    public void AddRootSample(ISampleProvider input)
    {
        if (!RootMixer.MixerInputs.Contains(input))
        {
            RootMixer.AddMixerInput(input);
        }
    }

    public void RemoveRootSample(ISampleProvider input)
    {
        if (RootMixer.MixerInputs.Contains(input))
        {
            RootMixer.RemoveMixerInput(input);
        }
    }

    public async Task<ISampleProvider?> PlayRootSound(string path, SampleControl sampleControl)
    {
        var rootSample = await RootMixer.PlaySound(path, sampleControl).ConfigureAwait(false);
        return rootSample;
    }

    public void Dispose()
    {
        if (OutputDevice != null)
        {
            Context.Post(_ => OutputDevice.Dispose(), null);
        }

        if (Context is IDisposable id)
        {
            id.Dispose();
        }
    }
}