using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.Subchannels;

internal class LoopProviders
{
    private readonly Dictionary<int, LoopProvider> _dictionary = new();

    public bool ShouldRemoveAll(int channel)
    {
        return _dictionary.ContainsKey(channel);
    }

    public bool ChangeAllVolumes(float volume)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;
            loopProvider.SetVolume(volume);
        }
        return true;
    }

    public bool ChangeAllBalances(float balance)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;
            loopProvider.SetBalance(balance);
        }

        return true;
    }

    public bool ChangeVolume(int? loopChannel, float volume)
    {
        if (loopChannel == null) return false;
        var lc = loopChannel.Value;
        if (!_dictionary.TryGetValue(lc, out var loopProvider)) return false;
        loopProvider.SetVolume(volume);
        return true;
    }

    public bool ChangeBalance(int? loopChannel, float balance)
    {
        if (loopChannel == null) return false;
        var lc = loopChannel.Value;
        if (!_dictionary.TryGetValue(lc, out var loopProvider)) return false;
        loopProvider.SetBalance(balance);
        return true;
    }

    public bool Remove(int? loopChannel, MixingSampleProvider? mixer)
    {
        if (loopChannel == null) return false;
        var lc = loopChannel.Value;
        if (_dictionary.TryGetValue(lc, out var loopProvider))
        {
            loopProvider.RemoveFrom(mixer);
            loopProvider.Dispose();
            return _dictionary.Remove(lc);
        }

        return false;
    }

    public void RemoveAll(MixingSampleProvider? mixer)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;

            loopProvider.RemoveFrom(mixer);
            loopProvider.Dispose();
            _dictionary.Remove(channel);
        }
    }

    public async Task CreateAsync(SoundElement soundElement, MixingSampleProvider mixer, float balanceFactor = 1)
    {
        var cachedSound = await soundElement.GetCachedSoundAsync(mixer.WaveFormat);
        if (cachedSound is null || soundElement.LoopChannel is null) return;

        var loopChannel = soundElement.LoopChannel.Value;
        Remove(loopChannel, mixer);

        var byteArray = new byte[cachedSound.AudioData.Length * sizeof(float)];
        Buffer.BlockCopy(cachedSound.AudioData, 0, byteArray, 0, byteArray.Length);

        var memoryStream = new MemoryStream(byteArray);
        var waveStream = new RawSourceWaveStream(memoryStream, cachedSound.WaveFormat);
        var loopStream = new LoopStream(waveStream);
        var volumeProvider = new VolumeSampleProvider(loopStream.ToSampleProvider())
        {
            Volume = soundElement.Volume
        };
        var balanceProvider = new BalanceSampleProvider(volumeProvider)
        {
            Balance = soundElement.Balance * balanceFactor
        };

        _dictionary.Add(loopChannel, new LoopProvider(balanceProvider, volumeProvider, memoryStream));
        mixer?.AddMixerInput(balanceProvider);
    }
}