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
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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

            _asio = DeviceCreationHelper.CreateDevice(out var description, null);
            _engine = new AudioPlaybackEngine(_asio);
            var s1 = await CachedSoundFactory.GetOrCreateCacheSound(_engine.WaveFormat,
                @"C:\Users\milkitic\Downloads\1680421 EBIMAYO - GOODTEK [no video]\soft-hitclap.wav");
            var s2 = await CachedSoundFactory.GetOrCreateCacheSound(_engine.WaveFormat,
                @"C:\Users\milkitic\Downloads\1680421 EBIMAYO - GOODTEK [no video]\soft-hitnormal2.wav");
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            _asio.Dispose();
        }
    }
}
