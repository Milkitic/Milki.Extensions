using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private static readonly Dictionary<string, Assembly?> _assemblyCache = new();

    public static async Task<SmartWaveReader> GetResampledAudioFileReader(string path, WaveFormat newWaveFormat)
    {
        var stream = await ResampleAsync(path, newWaveFormat).ConfigureAwait(false);
        return stream is SmartWaveReader afr ? afr : new SmartWaveReader(stream);
    }

    private static async Task<Stream> ResampleAsync(string path, WaveFormat newWaveFormat)
    {
        return await Task.Run(() => Resample(path, newWaveFormat)).ConfigureAwait(false);
    }

    private static Stream Resample(string path, WaveFormat newWaveFormat)
    {
        SmartWaveReader? audioFileReader = null;
        try
        {
            if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            {
                var span = path.AsSpan(6);
                var firstSplit = span.IndexOf('/');
                var assemblyName = span.Slice(0, firstSplit).ToString();
                var resourcePath = span.Slice(firstSplit + 1).ToString();

                if (!_assemblyCache.TryGetValue(assemblyName, out var assembly))
                {
                    assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(k =>
                       k.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

                    _assemblyCache.Add(assemblyName, assembly);
                }

                if (assembly == null)
                {
                    audioFileReader = new SmartWaveReader(SharedUtils.EmptyWaveFile);
                }
                else
                {
                    var stream = assembly.GetManifestResourceStream(resourcePath);
                    if (stream == null)
                    {
                        audioFileReader = new SmartWaveReader(SharedUtils.EmptyWaveFile);
                    }
                    else
                    {
                        audioFileReader = new SmartWaveReader(stream);
                    }
                }
            }
            else if (File.Exists(path))
            {
                audioFileReader = new SmartWaveReader(path);
            }
            else
            {
                audioFileReader = new SmartWaveReader(SharedUtils.EmptyWaveFile);
            }

            if (CompareWaveFormat(audioFileReader.WaveFormat, newWaveFormat))
            {
                return audioFileReader;
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
    }

    private static bool CompareWaveFormat(WaveFormat format1, WaveFormat format2)
    {
        if (format2.Channels != format1.Channels) return false;
        if (format2.SampleRate != format1.SampleRate) return false;
        return true;
    }
}