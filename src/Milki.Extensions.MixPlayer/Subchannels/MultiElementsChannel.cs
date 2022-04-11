using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.Subchannels
{
    public abstract class MultiElementsChannel : Subchannel, ISoundElementsProvider
    {
        private static readonly ILogger? Logger = Configuration.Instance.GetCurrentClassLogger();
        private readonly VariableStopwatch _sw = new VariableStopwatch();

        protected List<SoundElement>? SoundElements;
        public IReadOnlyCollection<SoundElement>? SoundElementCollection =>
            SoundElements == null ? null : new ReadOnlyCollection<SoundElement>(SoundElements);
        //protected readonly SingleMediaChannel? ReferenceChannel;
        private ConcurrentQueue<SoundElement>? _soundElementsQueue;

        private VolumeSampleProvider? _volumeProvider;
        private bool _isVolumeEnabled = false;

        private Task? _playingTask;
        //private Task _calibrationTask;
        private CancellationTokenSource? _cts;
        private readonly object _skipLock = new object();

        private readonly LoopProviders _loopProviders = new LoopProviders();

        private float _playbackRate;
        private readonly MixSettings _mixSettings;

        public bool IsPlayRunning => _playingTask != null &&
                                     !_playingTask.IsCanceled &&
                                     !_playingTask.IsCompleted &&
                                     !_playingTask.IsFaulted;

        public override TimeSpan Duration { get; protected set; }

        public override TimeSpan Position => _sw.Elapsed;

        public override TimeSpan ChannelStartTime => TimeSpan.FromMilliseconds(Configuration.Instance.GeneralOffset);

        public int ManualOffset
        {
            get => -(int)_sw.ManualOffset.TotalMilliseconds;
            set => _sw.ManualOffset = -TimeSpan.FromMilliseconds(value);
        }

        public sealed override float PlaybackRate
        {
            get => _sw.Rate;
            protected set => _sw.Rate = value;
        }
        public float BalanceFactor { get; set; } = 0.35f;

        public sealed override bool UseTempo { get; protected set; }

        public MixingSampleProvider? Submixer { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="mixSettings"></param>
        /// <param name="referencedChannels"></param>
        public MultiElementsChannel(AudioPlaybackEngine engine,
            MixSettings? mixSettings = null,
            //SingleMediaChannel? referenceChannel = null,
            ICollection<Subchannel>? referencedChannels = null) : base(engine, referencedChannels)
        {
            mixSettings ??= new MixSettings();
            _mixSettings = mixSettings;
            if (!mixSettings.EnableVolume) Submixer = engine.RootMixer;
            //ReferenceChannel = referenceChannel;
        }

        public override async Task Initialize()
        {
            if (Submixer == null)
            {
                Submixer = new MixingSampleProvider(Engine.WaveFormat)
                {
                    ReadFully = true
                };
                _volumeProvider = new VolumeSampleProvider(Submixer);
                _isVolumeEnabled = true;
                Engine.AddRootSample(_volumeProvider);
            }

            await Stop();

            SampleControl.VolumeChanged = f =>
            {
                if (_volumeProvider != null) _volumeProvider.Volume = f;
            };
            if (_volumeProvider != null) _volumeProvider.Volume = Volume;

            await RequeueAsync(TimeSpan.Zero);
            var elements = SoundElements ?? new List<SoundElement>();

            //var ordered = soundElements.OrderBy(k => k.Offset).ToArray();
            var lasts = elements
                .Skip(elements.Count > 9 ? elements.Count - 9 : elements.Count)
                .AsParallel()
                .Select(async k => (k, await k.GetNearEndTimeAsync(Engine.FileWaveFormat)));
            await Task.WhenAll(lasts);
            var last9Elements = lasts.Select(k => k.Result).ToArray();

            var max = TimeSpan.FromMilliseconds(last9Elements.Length == 0
                ? 0
                : last9Elements.Max(k => k.Item2));

            Duration = MathUtils.Max(
                TimeSpan.FromMilliseconds(elements.Count == 0 ? 0 : elements.Max(k => k.Offset)), max);

            //await Task.Run(() =>
            //{
            //    elements
            //        .Where(k => k.FilePath != null &&
            //                    (k.ControlType == SlideControlType.None || k.ControlType == SlideControlType.StartNew))
            //        .AsParallel()
            //        .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
            //        .ForAll(k => CachedSound.CreateCacheSounds(new[] { k.FilePath }).Wait());
            //});

            //await CachedSound.CreateCacheSounds(SoundElements
            //    .Where(k => k.FilePath != null)
            //    .Select(k => k.FilePath));

            var configuration = Configuration.Instance;
            await SetPlaybackRate(configuration.PlaybackRate, configuration.KeepTune);
            PlayStatus = PlayStatus.Ready;
        }

        public override async Task Play()
        {
            if (PlayStatus == PlayStatus.Playing) return;

            await ReadyLoopAsync();

            StartPlayTask();
            RaisePositionUpdated(_sw.Elapsed, true);
            //StartCalibrationTask();
            PlayStatus = PlayStatus.Playing;
        }

        public override async Task Pause()
        {
            if (PlayStatus == PlayStatus.Paused) return;

            await CancelLoopAsync();

            RaisePositionUpdated(_sw.Elapsed, true);
            PlayStatus = PlayStatus.Paused;
        }

        public override async Task Stop()
        {
            if (PlayStatus is PlayStatus.Paused or PlayStatus.Ready or PlayStatus.Unknown &&
                Position == TimeSpan.Zero) return;

            await CancelLoopAsync();
            await SkipTo(TimeSpan.Zero);
            PlayStatus = PlayStatus.Paused;
        }

        public override async Task Restart()
        {
            if (Position == TimeSpan.Zero) return;

            await SkipTo(TimeSpan.Zero);
            await Play();
        }

        public override async Task SkipTo(TimeSpan time)
        {
            if (time == Position) return;

            _loopProviders.RemoveAll(Submixer);

            await Task.Run(() =>
            {
                lock (_skipLock)
                {
                    var status = PlayStatus;
                    PlayStatus = PlayStatus.Reposition;

                    _sw.SkipTo(time);
                    Logger?.LogDebug("{0} want skip: {1}; actual: {2}", Description, time, Position);
                    RequeueAsync(time).Wait();

                    PlayStatus = status;
                }
            });
            RaisePositionUpdated(_sw.Elapsed, true);
        }

        public override async Task Sync(TimeSpan time)
        {
            _sw.SkipTo(time);
            await Task.CompletedTask;
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            PlaybackRate = rate;
            UseTempo = useTempo;
            AdjustModOffset();
            await Task.CompletedTask;
        }

        private void AdjustModOffset()
        {
            if (Math.Abs(_sw.Rate - 0.75) < 0.001 && !UseTempo)
                _sw.VariableOffset = TimeSpan.FromMilliseconds(-25);
            else if (Math.Abs(_sw.Rate - 1.5) < 0.001 && UseTempo)
                _sw.VariableOffset = TimeSpan.FromMilliseconds(15);
            else
                _sw.VariableOffset = TimeSpan.Zero;
        }

        private void StartPlayTask()
        {
            if (IsPlayRunning) return;

            _playingTask = new Task(async () =>
            {
                while (_soundElementsQueue!.Count > 0)
                {
                    if (_cts!.IsCancellationRequested)
                    {
                        _sw.Stop();
                        break;
                    }

                    //Position = _sw.Elapsed;
                    RaisePositionUpdated(_sw.Elapsed, false);
                    lock (_skipLock)
                    {
                        // wow nothing here
                    }

                    await SelectElements((int)_sw.ElapsedMilliseconds);

                    if (!TaskEx.TaskSleep(1, _cts)) break;
                }

                if (!_cts!.Token.IsCancellationRequested)
                {
                    PlayStatus = PlayStatus.Finished;
                    await SkipTo(TimeSpan.Zero);
                }
            }, TaskCreationOptions.LongRunning);
            _playingTask.Start();
        }

        public async Task SelectElements(int offset)
        {
            while (_soundElementsQueue!.TryPeek(out var soundElement) &&
                   soundElement.Offset <= offset &&
                   _soundElementsQueue.TryDequeue(out soundElement))
            {
                lock (_skipLock)
                {
                    // wow nothing here
                }

                try
                {
                    switch (soundElement.ControlType)
                    {
                        case SlideControlType.None:
                            var cachedSound = await soundElement.GetCachedSoundAsync(Submixer!.WaveFormat);
                            var flag = Submixer.PlaySound(cachedSound, soundElement.Volume,
                                soundElement.Balance * BalanceFactor);
                            if (soundElement.SubSoundElement != null)
                                soundElement.SubSoundElement.RelatedProvider = flag;

                            break;
                        case SlideControlType.StopNote:
                            if (soundElement.RelatedProvider != null)
                            {
                                if (_mixSettings.ForceStopFadeoutDuration > 0)
                                {
                                    Submixer!.RemoveMixerInput(soundElement.RelatedProvider);
                                    var fadeOut = new FadeInOutSampleProvider(soundElement.RelatedProvider);
                                    fadeOut.BeginFadeOut(400);
                                    Submixer.AddMixerInput(fadeOut);
                                }
                                else
                                {
                                    Submixer!.RemoveMixerInput(soundElement.RelatedProvider);
                                }
                            }

                            break;
                        case SlideControlType.StartNew:
                            if (_mixSettings.ForceMode &&
                                soundElement.LoopChannel != null &&
                                _loopProviders.ShouldRemoveAll(soundElement.LoopChannel.Value))
                            {
                                _loopProviders.RemoveAll(Submixer);
                            }

                            await _loopProviders.CreateAsync(soundElement, Submixer!, BalanceFactor);
                            break;
                        case SlideControlType.StopRunning:
                            _loopProviders.Remove(soundElement.LoopChannel, Submixer);
                            break;
                        case SlideControlType.ChangeBalance:
                            _loopProviders.ChangeAllBalances(soundElement.Balance * BalanceFactor);
                            break;
                        case SlideControlType.ChangeVolume:
                            _loopProviders.ChangeAllVolumes(soundElement.Volume);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Error while playing target element. Source: {0}; {1}",
                        soundElement.FilePath, soundElement.ControlType);
                }
            }
        }

        protected async Task RequeueAsync(TimeSpan startTime)
        {
            var queue = new ConcurrentQueue<SoundElement>();
            if (SoundElements == null)
            {
                var elements = new List<SoundElement>(await GetSoundElements());
                var subElements = elements
                    .Where(k => k.SubSoundElement != null)
                    .Select(k => k.SubSoundElement!)
                    .ToArray();
                elements.AddRange(subElements);
                SoundElements = elements;
                Duration = TimeSpan.FromMilliseconds(SoundElements.Count == 0 ? 0 : SoundElements.Max(k => k.Offset));
                SoundElements.Sort(new SoundElementTimingComparer());
            }

            await Task.Run(() =>
            {
                foreach (var i in SoundElements)
                {
                    if (i.Offset < startTime.TotalMilliseconds)
                        continue;
                    queue.Enqueue(i);
                }
            });

            _soundElementsQueue = queue;
        }

        private async Task ReadyLoopAsync()
        {
            _cts = new CancellationTokenSource();
            _sw.Start();
            await Task.CompletedTask;
        }

        private async Task CancelLoopAsync()
        {
            _sw.Stop();
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            await TaskEx.WhenAllSkipNull(_playingTask/*, _calibrationTask*/);
            Logger?.LogDebug(@"{0} task canceled.", Description);
        }

        public abstract Task<IEnumerable<SoundElement>> GetSoundElements();

        public override async ValueTask DisposeAsync()
        {
            await Stop();
            Logger?.LogDebug($"Disposing: Stopped.");

            _loopProviders.RemoveAll(Submixer);

            _cts?.Dispose();
            Logger?.LogDebug($"Disposing: Disposed {nameof(_cts)}.");
            if (_volumeProvider != null)
                Engine.RemoveRootSample(_volumeProvider);
            //await base.DisposeAsync();
            //Logger.Debug($"Disposing: Disposed base.");
        }
    }

    public class MixSettings
    {
        public bool EnableVolume { get; set; } = true;

        public int ForceStopFadeoutDuration { get; set; } = 400;
        ///// <summary>
        ///// negative: unlimited
        ///// </summary>
        //public int AllowLoopChannelCount { get; set; } = -1;

        /// <summary>
        /// 如果上个相同ID的循环轨未播放完成，则清除所有轨
        /// </summary>
        public bool ForceMode { get; set; } = false;
    }
}