using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer.Annotations;

namespace Milki.Extensions.MixPlayer
{
    public class Configuration : INotifyPropertyChanged
    {
        private Configuration()
        {
        }

        public static Configuration Instance { get; } = new Configuration();

        private ILoggerFactory? _factory;
        private uint _generalOffset = 0;
        private float _playbackRate = 1;
        private bool _keepTune = false;
        private string _defaultDir = Path.Combine(Environment.CurrentDirectory, "default");
        private string _cacheDir = Path.Combine(Environment.CurrentDirectory, "caching");
        private string _soundTouchDir = Path.Combine(Environment.CurrentDirectory, "libs", "SoundTouch");

        internal ILogger<T>? GetLogger<T>() => _factory?.CreateLogger<T>();
        internal ILogger? GetLogger(Type category)
        {
            var fullName = category.Namespace + "." + category.Name;
            return _factory?.CreateLogger(fullName);
        }

        internal ILogger? GetCurrentClassLogger()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame[]? stackFrames = stackTrace.GetFrames();
            if (stackFrames is { Length: > 1 })
            {
                var frame1 = stackFrames[1];
                var type = frame1.GetMethod().ReflectedType;
                return GetLogger(type);
            }

            return null;
        }

        public uint GeneralOffset
        {
            get => _generalOffset;
            set
            {
                if (value == _generalOffset) return;
                _generalOffset = value;
                OnPropertyChanged();
            }
        }

        public float PlaybackRate
        {
            get => _playbackRate;
            set
            {
                if (value.Equals(_playbackRate)) return;
                _playbackRate = value;
                OnPropertyChanged();
            }
        }

        public bool KeepTune
        {
            get => _keepTune;
            set
            {
                if (value == _keepTune) return;
                _keepTune = value;
                OnPropertyChanged();
            }
        }

        public string DefaultDir
        {
            get => _defaultDir;
            set
            {
                if (value == _defaultDir) return;
                _defaultDir = value;
                OnPropertyChanged();
            }
        }

        public string CacheDir
        {
            get => _cacheDir;
            set
            {
                if (value == _cacheDir) return;
                _cacheDir = value;
                OnPropertyChanged();
            }
        }

        public string SoundTouchDir
        {
            get => _soundTouchDir;
            set
            {
                if (value == _soundTouchDir) return;
                _soundTouchDir = value;
                OnPropertyChanged();
            }
        }

        public void SetLogger(ILoggerFactory loggerFactory)
        {
            _factory = loggerFactory;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
