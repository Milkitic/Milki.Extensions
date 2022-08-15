using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

/// <summary>
/// Audio file to wave stream
/// </summary>
public static class ResampleHelper
{
    private static readonly ILogger? Logger = Configuration.Instance.GetCurrentClassLogger();
    private static readonly ConcurrentDictionary<string, Assembly?> AssemblyCache = new();

    public static async Task<SmartWaveReader> GetResampledSmartWaveReader(string path, WaveFormat newWaveFormat, bool useWdlResampler = false)
    {
        var smartWaveReader = await ResampleAsync(path, newWaveFormat, useWdlResampler).ConfigureAwait(false);
        return smartWaveReader;
    }

    private static async Task<SmartWaveReader> ResampleAsync(string path, WaveFormat newWaveFormat, bool useWdlResampler)
    {
        return await Task.Run(() => Resample(path, newWaveFormat, useWdlResampler)).ConfigureAwait(false);
    }

    private static SmartWaveReader Resample(string path, WaveFormat newWaveFormat, bool useWdlResampler)
    {
        SmartWaveReader? smartWaveReader = null;
        try
        {
            if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            {
                var span = path.AsSpan(6);
                var firstSplit = span.IndexOf('/');
                var assemblyName = span.Slice(0, firstSplit).ToString();
                var resourcePath = span.Slice(firstSplit + 1).ToString();

                var assembly = AssemblyCache.GetOrAdd(assemblyName, name =>
                    AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(k => k.GetName().Name
                                ?.Equals(name, StringComparison.OrdinalIgnoreCase) == true
                        )
                );

                if (assembly == null)
                {
                    smartWaveReader = new SmartWaveReader(SharedUtils.EmptyWaveFile);
                }
                else
                {
                    var stream = assembly.GetManifestResourceStream(resourcePath);
                    if (stream == null)
                    {
                        smartWaveReader = new SmartWaveReader(SharedUtils.EmptyWaveFile);
                    }
                    else
                    {
                        smartWaveReader = new SmartWaveReader(stream);
                    }
                }
            }
            else if (File.Exists(path))
            {
                smartWaveReader = new SmartWaveReader(path);
            }
            else
            {
                smartWaveReader = new SmartWaveReader(SharedUtils.EmptyWaveFile);
            }

            if (useWdlResampler)
            {
                return ResampleByWdl(path, newWaveFormat, smartWaveReader);
            }
            else
            {
                return ResampleByMf(path, newWaveFormat, smartWaveReader);
            }
        }
        catch (Exception ex)
        {
            smartWaveReader?.Dispose();
            Console.Error.WriteLine($"Error while resampling audio file {path}: " + ex.Message);
            throw;
        }
    }

    private static SmartWaveReader ResampleByMf(string path, WaveFormat newWaveFormat, SmartWaveReader smartWaveReader)
    {
        if (CompareWaveFormat(smartWaveReader.WaveFormat, newWaveFormat))
        {
            return smartWaveReader;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using (smartWaveReader)
            {
                using var resampler = new MediaFoundationResampler(smartWaveReader, newWaveFormat);
                var stream = new MemoryStream();
                resampler.ResamplerQuality = 60; // Highest
                WaveFileWriter.WriteWavFileToStream(stream, resampler);
                return new SmartWaveReader(stream);
            }
        }
        finally
        {
            var message = $"Resampled (MF) {Path.GetFileName(path)} in {sw.Elapsed.TotalMilliseconds:N2}ms";
            Logger?.LogDebug(message);
            if (Debugger.IsAttached)
            {
                Console.WriteLine(message);
            }
        }
    }

    private static SmartWaveReader ResampleByWdl(string path, WaveFormat newWaveFormat, SmartWaveReader smartWaveReader)
    {
        ISampleProvider iSampleProvider;
        if (smartWaveReader.WaveFormat.Channels == 1 && newWaveFormat.Channels == 2)
        {
            iSampleProvider = new MonoToStereoSampleProvider(smartWaveReader);
        }
        else if (smartWaveReader.WaveFormat.Channels == 2 && newWaveFormat.Channels == 1)
        {
            iSampleProvider = new StereoToMonoSampleProvider(smartWaveReader);
        }
        else
        {
            iSampleProvider = smartWaveReader;
        }

        var sw = Stopwatch.StartNew();

        if (smartWaveReader.WaveFormat.SampleRate == newWaveFormat.SampleRate &&
            iSampleProvider == smartWaveReader)
        {
            return smartWaveReader;
        }
        else
        {
            var stream = new MemoryStream();
            var length = Math.Min((int)smartWaveReader.Length, 16 * 1024);
            var array = ArrayPool<float>.Shared.Rent(length);
            var wdlResampler = new WdlResamplingSampleProvider(iSampleProvider, newWaveFormat.SampleRate);

            try
            {
                // Should not dispose, cuz disposing will only dispose the stream
                var writer = new WaveFileWriter(stream, newWaveFormat);
                while (wdlResampler.Read(array, 0, length) > 0)
                {
                    writer.WriteSamples(array, 0, length);
                }

                writer.Flush();
                return new SmartWaveReader(stream);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(array);
                var message = $"Resampled (WDL) {Path.GetFileName(path)} in {sw.Elapsed.TotalMilliseconds:N2}ms";
                Logger?.LogDebug(message);
                if (Debugger.IsAttached)
                {
                    Console.WriteLine(message);
                }
            }
        }
    }

    private static bool CompareWaveFormat(WaveFormat format1, WaveFormat format2)
    {
        if (format2.Channels != format1.Channels) return false;
        if (format2.SampleRate != format1.SampleRate) return false;
        return true;
    }
}