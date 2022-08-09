using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public bool ChangeAllVolumes(float volume, float volumeFactor = 1.25f)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;
            loopProvider.SetVolume(volume * volumeFactor);
        }
        return true;
    }

    public bool ChangeAllBalances(float balance, float balanceFactor = 1)
    {
        foreach (var kvp in _dictionary.ToList())
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;
            loopProvider.SetBalance(balance * balanceFactor);
        }

        return true;
    }

    public bool ChangeVolume(int channel, float volume, float volumeFactor = 1.25f)
    {
        if (!_dictionary.TryGetValue(channel, out var loopProvider)) return false;
        loopProvider.SetVolume(volume * volumeFactor);
        return true;
    }

    public bool ChangeBalance(int channel, float balance, float balanceFactor = 1)
    {
        if (!_dictionary.TryGetValue(channel, out var loopProvider)) return false;
        loopProvider.SetBalance(balance * balanceFactor);
        return true;
    }

    public bool Remove(int soundElement, MixingSampleProvider? mixer)
    {
        if (!_dictionary.TryGetValue(soundElement, out var loopProvider)) return false;
        loopProvider.RemoveFrom(mixer);
        loopProvider.Dispose();
        return _dictionary.Remove(soundElement);

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

    public void PauseAll(MixingSampleProvider? mixer)
    {
        foreach (var kvp in _dictionary)
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;

            loopProvider.RemoveFrom(mixer);
        }
    }

    public void RecoverAll(MixingSampleProvider? mixer)
    {
        foreach (var kvp in _dictionary)
        {
            var channel = kvp.Key;
            var loopProvider = kvp.Value;

            loopProvider.AddTo(mixer);
        }
    }

    public void Create(SoundElement controlNode,
        CachedSound? cachedSound,
        MixingSampleProvider mixer,
        float volume,
        float balance,
        float volumeFactor = 1.25f,
        float balanceFactor = 1)
    {
        if (cachedSound is null) return;
        if (controlNode.LoopChannel == null) return;
        var slideChannel = controlNode.LoopChannel.Value;
        Remove(slideChannel, mixer);

        var audioDataLength = cachedSound.AudioData.Length * sizeof(float);
        var byteArray = ArrayPool<byte>.Shared.Rent(audioDataLength);
        Buffer.BlockCopy(cachedSound.AudioData, 0, byteArray, 0, audioDataLength);

        var memoryStream = new MemoryStream(byteArray, 0, audioDataLength);
        var waveStream = new RawSourceWaveStream(memoryStream, cachedSound.WaveFormat);
        var loopStream = new LoopStream(waveStream);
        var volumeProvider = new EnhancedVolumeSampleProvider(loopStream.ToSampleProvider())
        {
            Volume = volume * volumeFactor
        };
        var balanceProvider = new BalanceSampleProvider(volumeProvider)
        {
            Balance = balance * balanceFactor
        };

        var loopProvider = new LoopProvider(balanceProvider, volumeProvider, memoryStream, waveStream, loopStream, byteArray);
        _dictionary.Add(slideChannel, loopProvider);
        loopProvider.AddTo(mixer);
    }
}