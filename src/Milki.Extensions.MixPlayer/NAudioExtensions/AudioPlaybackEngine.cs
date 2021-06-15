using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.NAudioExtensions
{
    public sealed class AudioPlaybackEngine : IDisposable
    {
        public delegate void PlaybackTimingChangedEvent(AudioPlaybackEngine sender, TimeSpan oldTimestamp, TimeSpan newTimestamp);

        public IWavePlayer? OutputDevice { get; }
        public event PlaybackTimingChangedEvent? Updated;

        public SynchronizationContext Context { get; set; }

        private readonly VolumeSampleProvider _volumeProvider;
        private readonly TimingSampleProvider _timingProvider;

        public MixingSampleProvider RootMixer { get; }
        public ISampleProvider Root => _timingProvider;

        public float RootVolume
        {
            get => _volumeProvider.Volume;
            set => _volumeProvider.Volume = value;
        }

        public AudioPlaybackEngine()
        {
            Context = SynchronizationContext.Current ??
                      new StaSynchronizationContext("AudioPlaybackEngine_STA");
            RootMixer = new MixingSampleProvider(WaveFormatFactory.IeeeWaveFormat)
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

        public AudioPlaybackEngine(DeviceInfo deviceInfo)
        {
            Context = SynchronizationContext.Current ??
                      new StaSynchronizationContext("AudioPlaybackEngine_STA");
            RootMixer = new MixingSampleProvider(WaveFormatFactory.IeeeWaveFormat)
            {
                ReadFully = true
            };
            _volumeProvider = new VolumeSampleProvider(RootMixer);
            _timingProvider = new TimingSampleProvider(_volumeProvider);
            _timingProvider.Updated += (a, b) =>
            {
                Context.Send(_ => Updated?.Invoke(this, a, b), null);
            };
            OutputDevice = DeviceCreationHelper.CreateDevice(out var actualDeviceInfo, deviceInfo, Context);
            Context.Send(_ => OutputDevice.Init(_timingProvider), null);
            OutputDevice.Play();
        }

        public void AddRootSample(ISampleProvider input)
        {
            if (!RootMixer.MixerInputs.Contains(input))
                RootMixer.AddMixerInput(input);
        }

        public void RemoveRootSample(ISampleProvider input)
        {
            if (RootMixer.MixerInputs.Contains(input))
                RootMixer.RemoveMixerInput(input);
        }

        public async Task<ISampleProvider?> PlayRootSound(string path, SampleControl sampleControl)
        {
            var rootSample = await RootMixer
                .PlaySound(path, sampleControl)
                .ConfigureAwait(false);
            return rootSample;
        }

        public void Dispose()
        {
            if (OutputDevice != null)
                Context.Send(_ => OutputDevice.Dispose(), null);
            if (Context is IDisposable id)
                id.Dispose();
        }
    }
}