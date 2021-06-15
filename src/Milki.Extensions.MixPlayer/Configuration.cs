using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Milki.Extensions.MixPlayer
{
    public static class Configuration
    {
        private static ILoggerFactory? _factory;
        internal static ILogger<T>? GetLogger<T>() => _factory?.CreateLogger<T>();
        internal static ILogger? GetLogger(Type category)
        {
            var fullName = category.Namespace + "." + category.Name;
            return _factory?.CreateLogger(fullName);
        }

        public static ILogger? GetCurrentClassLogger()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame[]? stackFrames = stackTrace.GetFrames();
            if (stackFrames != null && stackFrames.Length > 1)
            {
                var frame1 = stackFrames[1];
                var type = frame1.GetMethod().ReflectedType;
                return GetLogger(type);
            }

            return null;
        }

        public static uint GeneralOffset { get; set; } = 0;
        public static float PlaybackRate { get; set; } = 1;
        public static bool KeepTune { get; set; } = false;

        public static string DefaultDir { get; set; } =
            Path.Combine(Environment.CurrentDirectory, "default");
        public static string CacheDir { get; set; } =
            Path.Combine(Environment.CurrentDirectory, "caching");
        public static string SoundTouchDir { get; set; } =
            Path.Combine(Environment.CurrentDirectory, "libs", "SoundTouch");

        public static void SetLogger(ILoggerFactory loggerFactory)
        {
            _factory = loggerFactory;
        }
    }
}
