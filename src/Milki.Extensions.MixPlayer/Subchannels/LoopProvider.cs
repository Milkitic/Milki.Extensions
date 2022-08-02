using System;
using System.IO;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.Subchannels;

internal sealed class LoopProvider : IDisposable
{
    private readonly BalanceSampleProvider _balanceProvider;
    private readonly EnhancedVolumeSampleProvider _volumeProvider;
    private readonly MemoryStream _loopStream;

    public LoopProvider(BalanceSampleProvider balanceProvider,
        EnhancedVolumeSampleProvider volumeProvider,
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