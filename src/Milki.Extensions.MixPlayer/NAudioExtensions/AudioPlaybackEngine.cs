using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.Annotations;
using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.NAudioExtensions;

public sealed class AudioPlaybackEngine : IDisposable, INotifyPropertyChanged
{
    public delegate void PlaybackTimingChangedEvent(AudioPlaybackEngine sender, TimeSpan oldTimestamp,
        TimeSpan newTimestamp);

    public event PlaybackTimingChangedEvent? Updated;

    private VolumeSampleProvider? _volumeProvider;
    private TimingSampleProvider? _timingProvider;

    public AudioPlaybackEngine(IWavePlayer? outputDevice, int sampleRate = 44100, int channelCount = 2,
        bool notifyProgress = true,
        bool enableVolume = true)
    {
        OutputDevice = outputDevice;
        Initialize(sampleRate, channelCount, notifyProgress, enableVolume);
    }

    public AudioPlaybackEngine(DeviceDescription? deviceDescription, int sampleRate = 44100, int channelCount = 2,
        bool notifyProgress = true,
        bool enableVolume = true)
    {
        OutputDevice = DeviceCreationHelper.CreateDevice(out _, deviceDescription, Context);
        Initialize(sampleRate, channelCount, notifyProgress, enableVolume);
    }

    public IWavePlayer? OutputDevice { get; }
    public WaveFormat FileWaveFormat { get; private set; } = null!;
    public WaveFormat WaveFormat { get; private set; } = null!;
    public MixingSampleProvider RootMixer { get; private set; } = null!;
    public ISampleProvider RootSampleProvider { get; private set; } = null!;
    public SynchronizationContext Context { get; private set; } = null!;

    public float Volume
    {
        get => _volumeProvider?.Volume ?? 1;
        set
        {
            if (_volumeProvider == null) return;
            if (value.Equals(_volumeProvider.Volume)) return;
            _volumeProvider.Volume = value;
            OnPropertyChanged();
        }
    }

    public void AddMixerInput(ISampleProvider input)
    {
        if (!RootMixer.MixerInputs.Contains(input))
        {
            RootMixer.AddMixerInput(input);
        }
    }

    public void RemoveMixerInput(ISampleProvider input)
    {
        if (RootMixer.MixerInputs.Contains(input))
        {
            RootMixer.RemoveMixerInput(input);
        }
    }

    public ISampleProvider? PlaySound(CachedSound cachedSound, SampleControl? sampleControl = null)
    {
        var rootSample = RootMixer.PlaySound(cachedSound, sampleControl);
        return rootSample;
    }

    public ISampleProvider? PlaySound(CachedSound cachedSound, float volume, float balance = 0)
    {
        var rootSample = RootMixer.PlaySound(cachedSound, balance, volume);
        return rootSample;
    }

    public async Task<ISampleProvider?> PlaySound(string path, SampleControl? sampleControl = null)
    {
        var rootSample = await RootMixer.PlaySound(path, sampleControl).ConfigureAwait(false);
        return rootSample;
    }

    public async Task<ISampleProvider?> PlaySound(string path, float volume, float balance = 0)
    {
        var rootSample = await RootMixer.PlaySound(path, balance, volume).ConfigureAwait(false);
        return rootSample;
    }

    public void Dispose()
    {
        OutputDevice?.Dispose();

        if (Context is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void Initialize(int sampleRate, int channelCount, bool notifyProgress, bool enableVolume)
    {
        Context = /*SynchronizationContext.Current ??*/new StaSynchronizationContext("AudioPlaybackEngine_STA");
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
        FileWaveFormat = new WaveFormat(sampleRate, channelCount);
        RootMixer = new MixingSampleProvider(WaveFormat)
        {
            ReadFully = true
        };

        ISampleProvider root = RootMixer;

        if (enableVolume)
        {
            _volumeProvider = new VolumeSampleProvider(root);
            root = _volumeProvider;
        }

        if (notifyProgress)
        {
            _timingProvider = new TimingSampleProvider(root);
            _timingProvider.Updated += (oldTimestamp, newTimestamp) =>
            {
                Context.Send(_ => Updated?.Invoke(this, oldTimestamp, newTimestamp), null);
            };
            root = _timingProvider;
        }

        if (OutputDevice != null)
        {
            Context.Send(_ => OutputDevice.Init(root), null);
            OutputDevice.Play();
        }

        RootSampleProvider = root;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}