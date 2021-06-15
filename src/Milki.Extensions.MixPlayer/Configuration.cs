using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Milki.Extensions.MixPlayer
{
    public class Configuration
    {
        private Configuration()
        {
        }

        public static Configuration Instance { get; } = new Configuration();

        private ILoggerFactory? _factory;
        internal ILogger<T>? GetLogger<T>() => _factory?.CreateLogger<T>();
        internal ILogger? GetLogger(Type category)
        {
            var fullName = category.Namespace + "." + category.Name;
            return _factory?.CreateLogger(fullName);
        }

        public ILogger? GetCurrentClassLogger()
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

        public uint GeneralOffset { get; set; } = 0;
        public float PlaybackRate { get; set; } = 1;
        public bool KeepTune { get; set; } = false;

        public string DefaultDir { get; set; } =
            Path.Combine(Environment.CurrentDirectory, "default");
        public string CacheDir { get; set; } =
            Path.Combine(Environment.CurrentDirectory, "caching");
        public string SoundTouchDir { get; set; } =
            Path.Combine(Environment.CurrentDirectory, "libs", "SoundTouch");

        public void SetLogger(ILoggerFactory loggerFactory)
        {
            _factory = loggerFactory;
        }
    }
}
