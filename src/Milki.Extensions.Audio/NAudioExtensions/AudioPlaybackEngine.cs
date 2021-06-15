using Milki.Extensions.Audio.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milki.Extensions.Audio.NAudioExtensions
{
    public sealed class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer? _outputDevice;
        public event Action<AudioPlaybackEngine, TimeSpan, TimeSpan>? Updated;

        private readonly TaskCompletionSource<object?>? _creationTask;
        private readonly TaskCompletionSource<object?>? _disposingTask;
        private readonly TaskCompletionSource<object?>? _completeTask;

        public SynchronizationContext DeviceSynchronizationContext { get; private set; }

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
            RootMixer = new MixingSampleProvider(WaveFormatFactory.IeeeWaveFormat)
            {
                ReadFully = true
            };
            _volumeProvider = new VolumeSampleProvider(RootMixer);
            _timingProvider = new TimingSampleProvider(_volumeProvider);
            _timingProvider.Updated += (a, b) => Updated?.Invoke(this, a, b);
            DeviceSynchronizationContext = SynchronizationContext.Current;
        }

        public AudioPlaybackEngine(IWavePlayer outputDevice)
        {
            RootMixer = new MixingSampleProvider(WaveFormatFactory.IeeeWaveFormat)
            {
                ReadFully = true
            };
            _volumeProvider = new VolumeSampleProvider(RootMixer);
            _timingProvider = new TimingSampleProvider(_volumeProvider);
            _timingProvider.Updated += (a, b) => Updated?.Invoke(this, a, b);
            _outputDevice = outputDevice;

            _creationTask = new TaskCompletionSource<object?>();
            _disposingTask = new TaskCompletionSource<object?>();
            _completeTask = new TaskCompletionSource<object?>();

            SynchronizationContext sc = SynchronizationContext.Current;
            var startThread = new Thread(() =>
            {
                sc = SynchronizationContext.Current;
                DeviceInstanceControl();
            })
            { IsBackground = true };
            startThread.SetApartmentState(ApartmentState.STA);
            startThread.Start();

            _creationTask.Task.Wait();

            DeviceSynchronizationContext = sc;
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

        public async Task<ISampleProvider> PlayRootSound(string path, SampleControl sampleControl)
        {
            var rootSample = await RootMixer.PlaySound(path, sampleControl).ConfigureAwait(false);
            return rootSample;
        }

        public void Dispose()
        {
            _disposingTask?.SetResult(default);
            _completeTask?.Task.Wait();
        }

        private void DeviceInstanceControl()
        {
            _outputDevice!.Init(_timingProvider);
            _creationTask!.SetResult(default);
            _disposingTask!.Task.Wait();
            _outputDevice!.Dispose();
            _completeTask!.SetResult(default);
        }
    }
}