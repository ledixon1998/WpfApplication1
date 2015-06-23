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
            /// Initialises a device enumerator to count/ find multimedia devices
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();

            /// Creates list of recording multimedia devices, then playback devices
            recordDevices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            playDevices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

            /// Puts recording devices into recording combo box and sets default selection to the first device in the box
            ComboDevicesRecording.ItemsSource = recordDevices;
            if( recordDevices.Count > 0 )
            ComboDevicesRecording.SelectedIndex = 0;
            ComboDevicesPlayback.ItemsSource = playDevices;
            ComboDevicesPlayback.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles event Button_Click when OK button on window is clicked
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CPUOutput.Content = "Hello " + UserInput.Text;

            /// Assigns pointer 'temp' to selected recording device in combo box
            MMDevice temp = (MMDevice)ComboDevicesRecording.SelectedItem;
            /// elliesPhatBeats instatiated as Wasapi capture class with selected recording device
            elliesPhatBeats = new WasapiCapture(temp);
            /// calls OnDataAvailable event handler when data is available in elliesPhatBeats from the recording device
            elliesPhatBeats.DataAvailable += OnDataAvailable;

            ///elliesPhatPlayer instatiated as Wasapi playback class with selected playback device, exclusive playback from soundcard and latency of 50ms
            elliesPhatPlayer = new WasapiOut(
                (MMDevice)ComboDevicesPlayback.SelectedItem,
                AudioClientShareMode.Exclusive,
                false, 50);
            /// creates a BufferedWaveProvider class (essentially a data array for storing recorded sound data) called bwp with waveformat of the recording input
            bwp = new BufferedWaveProvider(elliesPhatBeats.WaveFormat);
            /// initialises the playback to use the data from the buffered wave provider
            elliesPhatPlayer.Init(bwp);

            /// tells player to play and recorder to record
            elliesPhatPlayer.Play();
            elliesPhatBeats.StartRecording();
        }

        /// <summary>
        /// handles event in which recorder has available data
        /// </summary>

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread (you need to be on the same thread
            {
                Dispatcher.Invoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
                return;
            }
            /// Writes in window the number of bytes in the buffer
            CPUOutput.Content = "Been given " + e.BytesRecorded + " Bytes";

            /// Gives samples to bwp from recording device
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);

            int[] Convertedsamples = ConvertSamples(e.Buffer, e.BytesRecorded);
            DrawWaveform(Convertedsamples, e.BytesRecorded / 4);
        }

        int[] ConvertSamples(byte[] input, int arraysize)
        {
            int[] Dataarray = new int[arraysize/4];

            for (int i = 0; i < arraysize; i += 4)
            {
#if false
                int temp = (int)input[i+3];
                temp += (int)(input[i+2] * 256);
                temp += (int)(input[i + 1] * 256 * 256);
                temp += (int)(input[i + 0] * 256 * 256 * 256);
                Dataarray[i / 4] = temp;
#else
                Dataarray[i / 4] = ((((int)input[i + 3]) << 24) |
                                    (((int)input[i + 2]) << 16)|
                                    (((int)input[i + 1]) << 8) |
                                    (((int)input[i + 0]) ));
#endif
            }

            return Dataarray;
        }

        void DrawWaveform(int[] convertedsamples, int convertedarraysize)
        {
            WriteableBitmap Wavemap = new WriteableBitmap((int)Waveform.Width, (int)Waveform.Height, 96, 96, PixelFormats.Bgr32, null);

            byte[] colorData = { 200, 100, 50, 255 };

            for (int x=0; x < (int)Waveform.Width; x++)
            {
                int y = (int)convertedsamples[x]/147483647 + ((int)Waveform.Height/2);
                Wavemap.WritePixels(new Int32Rect(x, y, 1, 1), colorData, 4, 0); 
            }

            Waveform.Source = Wavemap;
        }
    }
}
