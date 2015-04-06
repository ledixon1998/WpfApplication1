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
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IWaveIn elliesPhatBeats;
        private IWavePlayer elliesPhatPlayer;
        public List<MMDevice> recordDevices { get; private set; }
        public List<MMDevice> playDevices { get; private set; }
        private BufferedWaveProvider bwp;

        public MainWindow()
        {
            InitializeComponent();
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            recordDevices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            playDevices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

            ComboDevicesRecording.ItemsSource = recordDevices;
            ComboDevicesRecording.SelectedIndex = 0;
            ComboDevicesPlayback.ItemsSource = playDevices;
            ComboDevicesPlayback.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CPUOutput.Content = "Hello " + UserInput.Text;

            MMDevice temp = (MMDevice)ComboDevicesRecording.SelectedItem;
            elliesPhatBeats = new WasapiCapture(temp);
            elliesPhatBeats.DataAvailable += OnDataAvailable;

            elliesPhatPlayer = new WasapiOut(
                (MMDevice)ComboDevicesPlayback.SelectedItem,
                AudioClientShareMode.Exclusive,
                false, 50);

            elliesPhatBeats.StartRecording();
            elliesPhatPlayer.Init(bwp = new BufferedWaveProvider(elliesPhatBeats.WaveFormat));
            elliesPhatPlayer.Play();
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
                return;
            } 
            CPUOutput.Content = "Been given " + e.BytesRecorded + " Bytes";
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }
    }
}
