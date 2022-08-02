using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.NAudioExtensions;

internal static class MixingSampleProviderExtension
{
    internal static ISampleProvider? PlaySound(this MixingSampleProvider mixer, in CachedSound? sound,
        SampleControl? sampleControl)
    {
        PlaySound(mixer, sound, sampleControl, out var rootSample);
        return rootSample;
    }

    internal static ISampleProvider? PlaySound(this MixingSampleProvider mixer, in CachedSound? sound,
        float volume, float balance)
    {
        PlaySound(mixer, sound, volume, balance, out var rootSample);
        return rootSample;
    }

    public static async Task<ISampleProvider?> PlaySound(this MixingSampleProvider mixer, string path,
        SampleControl? sampleControl)
    {
        var waveFormat = new WaveFormat(mixer.WaveFormat.SampleRate, mixer.WaveFormat.Channels);
        var sound = await CachedSoundFactory.GetOrCreateCacheSound(waveFormat, path).ConfigureAwait(false);
        PlaySound(mixer, sound, sampleControl, out var rootSample);
        return rootSample;
    }

    public static async Task<ISampleProvider?> PlaySound(this MixingSampleProvider mixer, string path,
        float volume, float balance)
    {
        var waveFormat = new WaveFormat(mixer.WaveFormat.SampleRate, mixer.WaveFormat.Channels);
        var sound = await CachedSoundFactory.GetOrCreateCacheSound(waveFormat, path).ConfigureAwait(false);
        PlaySound(mixer, sound, volume, balance, out var rootSample);
        return rootSample;
    }

    public static void AddMixerInput(this MixingSampleProvider mixer, ISampleProvider input,
        SampleControl? sampleControl, out ISampleProvider rootSample)
    {
        if (sampleControl != null)
        {
            var adjustVolume = input.AddToAdjustVolume(sampleControl.Volume);
            var adjustBalance = adjustVolume.AddToBalanceProvider(sampleControl.Balance);
            sampleControl.VolumeChanged ??= f => adjustVolume.Volume = f;
            sampleControl.BalanceChanged ??= f => adjustBalance.Balance = f;
            rootSample = adjustBalance;
            mixer.AddMixerInput(adjustBalance);
        }
        else
        {
            rootSample = input;
            mixer.AddMixerInput(input);
        }
    }

    public static void AddMixerInput(this MixingSampleProvider mixer, ISampleProvider input,
        float volume, float balance, out ISampleProvider rootSample)
    {
        var adjustVolume = volume >= 1 ? input : input.AddToAdjustVolume(volume);
        var adjustBalance = balance == 0 ? adjustVolume : adjustVolume.AddToBalanceProvider(balance);

        rootSample = adjustBalance;
        mixer.AddMixerInput(adjustBalance);
    }

    private static void PlaySound(MixingSampleProvider mixer, in CachedSound? sound, SampleControl? sampleControl,
        out ISampleProvider? rootSample)
    {
        if (sound == null)
        {
            rootSample = default;
            return;
        }

        mixer.AddMixerInput(new CachedSoundSampleProvider(sound), sampleControl, out rootSample);
    }

    private static void PlaySound(MixingSampleProvider mixer, in CachedSound? sound, float volume, float balance,
        out ISampleProvider? rootSample)
    {
        if (sound == null)
        {
            rootSample = default;
            return;
        }

        mixer.AddMixerInput(new CachedSoundSampleProvider(sound), volume, balance, out rootSample);
    }

    private static EnhancedVolumeSampleProvider AddToAdjustVolume(this ISampleProvider input, float volume)
    {
        var volumeSampleProvider = new EnhancedVolumeSampleProvider(input)
        {
            Volume = volume
        };
        return volumeSampleProvider;
    }

    private static BalanceSampleProvider AddToBalanceProvider(this ISampleProvider input, float balance)
    {
        var volumeSampleProvider = new BalanceSampleProvider(input)
        {
            Balance = balance
        };
        return volumeSampleProvider;
    }
}