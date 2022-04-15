using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

/// <summary>
/// Audio file to wave stream
/// </summary>
internal static class ResampleHelper
{
    private static readonly ILogger? Logger = Configuration.Instance.GetCurrentClassLogger();
    public static async Task<MyAudioFileReader> GetResampledAudioFileReader(string path, WaveType type, WaveFormat newWaveFormat)
    {
        var stream = await Resample(path, newWaveFormat).ConfigureAwait(false);
        return stream is MyAudioFileReader afr ? afr : new MyAudioFileReader(stream, type);
    }

    private static async Task<Stream> Resample(string path, WaveFormat newWaveFormat)
    {
        return await Task.Run(() =>
        {
            MyAudioFileReader? audioFileReader = null;
            try
            {
                audioFileReader = File.Exists(path)
                    ? new MyAudioFileReader(path)
                    : new MyAudioFileReader(SharedUtils.EmptyWaveFile, WaveType.Wav);
                if (CompareWaveFormat(audioFileReader.WaveFormat, newWaveFormat))
                {
                    return (Stream)audioFileReader;
                }

                var sw = Stopwatch.StartNew();
                try
                {
                    using (audioFileReader)
                    {
                        using var resampler = new MediaFoundationResampler(audioFileReader, newWaveFormat);
                        var stream = new MemoryStream();
                        resampler.ResamplerQuality = 60; // highest
                        WaveFileWriter.WriteWavFileToStream(stream, resampler);
                        stream.Position = 0;
                        return stream;
                    }
                }
                finally
                {
                    Logger?.LogDebug($"Resampled {Path.GetFileName(path)} in {sw.Elapsed.TotalMilliseconds:N2}ms");
                }
            }
            catch (Exception ex)
            {
                audioFileReader?.Dispose();
                Console.Error.WriteLine($"Error while resampling audio file {path}: " + ex.Message);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private static bool CompareWaveFormat(WaveFormat format1, WaveFormat format2)
    {
        if (format2.Channels != format1.Channels) return false;
        if (format2.SampleRate != format1.SampleRate) return false;
        return true;
    }
}