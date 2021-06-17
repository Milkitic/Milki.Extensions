using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer
{
    public sealed class SoundElement
    {
        private CachedSound? _cachedSound;
        private SoundElement() { }

        /// <summary>
        /// File path of the source sound.
        /// Can be null if the <see cref="SoundElement"/> is a control type.
        /// </summary>
        public string? FilePath { get; private set; }

        public double Offset { get; private set; }

        public float Volume { get; private set; }
        public float Balance { get; private set; }
        public PlaybackType PlaybackType { get; private set; }
        public int? LoopChannel { get; private set; }
        public SlideControlType ControlType { get; private set; } = SlideControlType.None;

        public bool HasSound => FilePath != null;

        public async Task<double> GetNearEndTimeAsync()
        {
            var cachedSound = await GetCachedSoundAsync();
            if (cachedSound == null) return 0;
            return cachedSound.Duration.TotalMilliseconds + Offset;
        }

        public double GetNearEndTime()
        {
            var cachedSound = GetCachedSoundAsync().Result;
            if (cachedSound == null) return 0;
            return cachedSound.Duration.TotalMilliseconds + Offset;
        }

        public static SoundElement Create(double offset, float volume, float balance, string filePath,
            double? forceStopOffset = null)
        {
            var se = new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
            };

            if (forceStopOffset != null)
            {
                se.SubSoundElement = new SoundElement
                {
                    Offset = forceStopOffset.Value,
                    ControlType = SlideControlType.StopNote
                };
            }

            return se;
        }

        public static SoundElement CreateLoopSignal(double offset, float volume, float balance,
            string filePath, int loopChannel)
        {
            return new SoundElement
            {
                Offset = offset,
                Volume = volume,
                Balance = balance,
                FilePath = filePath,
                ControlType = SlideControlType.StartNew,
                PlaybackType = PlaybackType.Loop,
                LoopChannel = loopChannel
            };
        }

        public static SoundElement CreateLoopStopSignal(double offset, int loopChannel)
        {
            return new SoundElement
            {
                Offset = offset,
                ControlType = SlideControlType.StopRunning,
                LoopChannel = loopChannel,
            };
        }

        public static SoundElement CreateLoopVolumeSignal(double offset, float volume)
        {
            return new SoundElement
            {
                Offset = offset,
                Volume = volume,
                ControlType = SlideControlType.ChangeVolume
            };
        }

        public static SoundElement CreateLoopBalanceSignal(double offset, float balance)
        {
            return new SoundElement
            {
                Offset = offset,
                Balance = balance,
                ControlType = SlideControlType.ChangeBalance
            };
        }

        internal ISampleProvider? RelatedProvider { get; set; }
        internal SoundElement? SubSoundElement { get; private set; }

        internal async Task<CachedSound?> GetCachedSoundAsync()
        {
            if (_cachedSound != null)
                return _cachedSound;

            if (FilePath == null)
                return null;

            var result = await CachedSound.GetOrCreateCacheSound(FilePath).ConfigureAwait(false);
            _cachedSound = result;
            return result;
        }
    }

    public enum PlaybackType
    {
        Normal,
        Loop
    }
}