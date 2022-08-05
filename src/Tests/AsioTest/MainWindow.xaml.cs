using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NVorbis;

namespace AsioTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IWavePlayer _asio;
        private MixingSampleProvider _sampleProvider;
        private AudioPlaybackEngine _engine;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var name = AsioOut.GetDriverNames().FirstOrDefault();

            var tsubakiNew = (NLayerMp3FileReader)new SmartWaveReader(@"F:\Test\tsubaki_new.mp3").ReaderStream!;
            var tsubakiOld = (NLayerMp3FileReader)new SmartWaveReader(@"F:\Test\tsubaki_old.mp3").ReaderStream!;
            var tsubakiOld160 = (NLayerMp3FileReader)new SmartWaveReader(@"F:\Test\tsubaki_old_160.mp3").ReaderStream!;
            var goodtek = (NLayerMp3FileReader)new SmartWaveReader(@"F:\Test\goodtek.mp3").ReaderStream!;
            var tsubakiOldou = (NLayerMp3FileReader)new SmartWaveReader(@"F:\Test\320.mp3").ReaderStream!;
            //var mp3Acm = new Mp3FileReaderBase(path3, (format) => new AcmMp3FrameDecompressor(format));
            var tsubakiNewWf = tsubakiNew.Mp3WaveFormat;
            var tsubakiOldWf = tsubakiOld.Mp3WaveFormat;
            var tsubakiOld160Wf = tsubakiOld160.Mp3WaveFormat;
            var goodtekWf = goodtek.Mp3WaveFormat;

            _asio = DeviceCreationHelper.CreateDevice(out var description, null);
            _engine = new AudioPlaybackEngine(_asio);
            //var s1 = await CachedSoundFactory.GetOrCreateCacheSound(_engine.WaveFormat,
            //    @"C:\Users\milkitic\Downloads\1680421 EBIMAYO - GOODTEK [no video]\soft-hitclap.wav");
            //var s2 = await CachedSoundFactory.GetOrCreateCacheSound(_engine.WaveFormat,
            //    @"C:\Users\milkitic\Downloads\1680421 EBIMAYO - GOODTEK [no video]\soft-hitnormal2.wav");
            var s3 = await CachedSoundFactory.GetOrCreateCacheSound(_engine.WaveFormat,
                @"D:\GitHub\Osu-Player\OsuPlayer.Wpf\bin\Debug\Songs\1602552 Tian Yi Ming - Re Ai 105C De Ni\audio.mp3");
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            _asio.Dispose();
        }
    }
}
