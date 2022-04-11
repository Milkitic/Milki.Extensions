using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.Utilities;
using NAudio.Wave;

namespace Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

[Fody.ConfigureAwait(false)]
public static class CachedSoundFactory
{
    private static readonly ILogger? Logger = Configuration.Instance.GetCurrentClassLogger();
    public static WaveType WavType => WaveType.Wav;
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CachedSound?>>
        IdentifiersDictionary = new();
    public static int GetCount(string? identifier = null)
    {
        if (IdentifiersDictionary.TryGetValue(identifier ?? "default", out var dict))
        {
            return dict.Count;
        }

        return 0;
    }
    public static bool ContainsCache(string? path)
    {
        if (path == null) return false;
        foreach (var dictionary in IdentifiersDictionary.Values)
        {
            if (dictionary.ContainsKey(path)) return true;
        }

        return false;
    }

    public static async Task<CachedSound?> GetOrCreateCacheSound(WaveFormat waveFormat, string? path, string? identifier = null)
    {
        if (path == null) return null;
        var dict = IdentifiersDictionary.GetOrAdd(identifier ?? "default",
            _ => new ConcurrentDictionary<string, CachedSound?>());

        if (dict.TryGetValue(path, out var value))
        {
            return value;
        }

        if (!File.Exists(path))
        {
            dict.TryAdd(path, null);
            return null;
        }

        CachedSound cachedSound;
        try
        {
            cachedSound = await CreateCacheFromFile(waveFormat, path);
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Error while creating cached sound: {path}" + ex.Message);
            dict.TryAdd(path, null);
            return null;
        }

        // Cache each file once before play.
        var sound = dict.GetOrAdd(path, cachedSound);

        Logger?.LogDebug("Total size of cache usage: {0}", SharedUtils.SizeSuffix(
            IdentifiersDictionary
                .SelectMany(k => k.Value)
                .Sum(k => k.Value?.AudioData.Length * sizeof(float) ?? 0))
        );

        return sound;
    }

    public static void ClearCacheSounds(string? identifier = null)
    {
        if (IdentifiersDictionary.TryGetValue(identifier ?? "default", out var dict))
        {
            dict.Clear();
        }
    }

    private static async Task<CachedSound> CreateCacheFromFile(WaveFormat waveFormat, string filePath)
    {
        using var audioFileReader = await ResampleHelper.GetResampledAudioFileReader(filePath, WavType, waveFormat);
        var sw = Stopwatch.StartNew();
        var wholeData = new float[(int)(audioFileReader.Length / 4)];
        var actualWaveFormat = audioFileReader.WaveFormat;

        var length = actualWaveFormat.SampleRate * actualWaveFormat.Channels;
        var readBuffer = ArrayPool<float>.Shared.Rent(length);
        try
        {
            int samplesRead;
            int offset = 0;
            while ((samplesRead = audioFileReader.Read(readBuffer, 0, length)) > 0)
            {
                var read = offset + samplesRead;
                if (wholeData.Length < read)
                {
                    var realloc = new float[read];
                    wholeData.CopyTo(realloc, 0);
                    wholeData = realloc;
                }

                readBuffer.AsSpan(0, samplesRead).CopyTo(wholeData.AsSpan(offset, samplesRead));
                offset += samplesRead;
            }

            return new CachedSound(filePath, wholeData, audioFileReader.TotalTime, actualWaveFormat);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(readBuffer);
            Logger?.LogDebug($"Cached {Path.GetFileName(filePath)} in {sw.Elapsed.TotalMilliseconds:N2}ms");
        }
    }
}