using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Milki.Extensions.MixPlayer
{
    public static class Information
    {
        public const string WavExtension = ".wav";
        public const string OggExtension = ".ogg";
        public const string Mp3Extension = ".mp3";

        public static ICollection<string> SupportExtensions { get; } =
            new ReadOnlyCollection<string>(new[] { WavExtension, Mp3Extension, OggExtension });
    }
}
