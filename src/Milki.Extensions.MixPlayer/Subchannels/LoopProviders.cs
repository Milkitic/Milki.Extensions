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
    private readonly Dictionary<int, LoopProvider> _dictionary = new Dictionary<int, LoopProvider>();

    public bool ShouldRemoveAll(int channel)
    {
        return _dictionary.ContainsKey(channel);
    }

    public bool ChangeAllVolumes(float volume)
    {
        foreach (var (channel, loopProvider) in _dictionary.ToList())
            loopProvider.SetVolume(volume);
        return true;
    }

    public bool ChangeAllBalances(float balance)
    {
        foreach (var (channel, loopProvider) in _dictionary.ToList())
            loopProvider.SetBalance(balance);
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
        foreach (var (channel, loopProvider) in _dictionary.ToList())
        {
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

internal class LoopProvider : IDisposable
{
    private readonly BalanceSampleProvider _balanceProvider;
    private readonly VolumeSampleProvider _volumeProvider;
    private readonly MemoryStream _loopStream;

    public LoopProvider(BalanceSampleProvider balanceProvider,
        VolumeSampleProvider volumeProvider,
        MemoryStream loopStream)
    {
        _balanceProvider = balanceProvider;
        _volumeProvider = volumeProvider;
        _loopStream = loopStream;
    }

    public void SetBalance(float balance)
    {
        _balanceProvider.Balance = balance;
    }

    public void SetVolume(float volume)
    {
        _volumeProvider.Volume = volume;
    }

    public void RemoveFrom(MixingSampleProvider? mixer)
    {
        mixer?.RemoveMixerInput(_balanceProvider);
    }

    public void Dispose()
    {
        _loopStream.Dispose();
    }
}