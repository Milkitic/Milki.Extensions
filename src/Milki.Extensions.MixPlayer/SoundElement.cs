using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer;

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
    public SoundNode SoundNode { get; private set; } = SoundNode.None;

    internal ISampleProvider? RelatedProvider { get; set; }
    internal SoundElement? SubSoundElement { get; private set; }

    public bool HasSound => FilePath != null;

    public async Task<double> GetNearEndTimeAsync(WaveFormat waveFormat)
    {
        var cachedSound = await GetCachedSoundAsync(waveFormat).ConfigureAwait(false);
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
            se.SubSoundElement = CreateStopNote(forceStopOffset.Value);
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
            SoundNode = SoundNode.StartLoop,
            PlaybackType = PlaybackType.Loop,
            LoopChannel = loopChannel
        };
    }

    public static SoundElement CreateLoopStopSignal(double offset, int loopChannel)
    {
        return new SoundElement
        {
            Offset = offset,
            SoundNode = SoundNode.StopLoop,
            LoopChannel = loopChannel,
        };
    }

    public static SoundElement CreateLoopVolumeSignal(double offset, float volume)
    {
        return new SoundElement
        {
            Offset = offset,
            Volume = volume,
            SoundNode = SoundNode.ChangeVolume
        };
    }

    public static SoundElement CreateLoopBalanceSignal(double offset, float balance)
    {
        return new SoundElement
        {
            Offset = offset,
            Balance = balance,
            SoundNode = SoundNode.ChangeBalance
        };
    }

    internal static SoundElement CreateStopNote(double offset)
    {
        var se = new SoundElement
        {
            Offset = offset,
            SoundNode = SoundNode.ForceStop
        };

        return se;
    }

    internal async Task<CachedSound?> GetCachedSoundAsync(WaveFormat waveFormat)
    {
        if (_cachedSound != null)
            return _cachedSound;

        if (FilePath == null)
            return null;

        var result = await CachedSoundFactory.GetOrCreateCacheSound(waveFormat, FilePath).ConfigureAwait(false);
        _cachedSound = result;
        return result;
    }
}