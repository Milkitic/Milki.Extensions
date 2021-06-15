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
        private readonly IWavePlayer? _outputDevice;
        public event Action<AudioPlaybackEngine, TimeSpan, TimeSpan>? Updated;

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
            _outputDevice = DeviceCreationHelper.CreateDevice(out var actualDeviceInfo, deviceInfo, Context);
            Context.Send(_ => _outputDevice.Init(_timingProvider), null);
            _outputDevice.Play();
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
            if (_outputDevice != null)
                Context.Send(_ => _outputDevice.Dispose(), null);
            if (Context is IDisposable id)
                id.Dispose();
        }
    }
}