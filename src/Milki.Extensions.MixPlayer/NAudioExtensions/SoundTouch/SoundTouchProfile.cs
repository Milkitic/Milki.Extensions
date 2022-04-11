namespace Milki.Extensions.MixPlayer.NAudioExtensions.SoundTouch
{
    internal class SoundTouchProfile
    {
        public bool KeepTune { get; set; }
        public bool UseAntiAliasing { get; set; }
        public bool UseQuickSeek { get; set; } = true;

        public SoundTouchProfile(bool keepTune, bool useAntiAliasing)
        {
            KeepTune = keepTune;
            UseAntiAliasing = useAntiAliasing;
        }
    }
}