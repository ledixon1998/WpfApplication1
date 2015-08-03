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
using Fourier;

namespace WpfApplication1
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveIn elliesPhatBeats;
        private WaveOut elliesPhatPlayer;
        private BufferedWaveProvider bwp;
        private int SampleRate = 44100;
        private AudioAnalyser analyser;

        public MainWindow()
        {
            InitializeComponent();
            /// Initialises a device enumerator to count/ find multimedia devices
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            
            /// Creates list of recording multimedia devices, then playback devices
            try
            {
                int waveInDevices = WaveIn.DeviceCount;
                for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
                {
                    WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                    string device = string.Format("Device {0}: {1}, {2} channels",
                        waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
                    ComboDevicesRecording.Items.Add(device);
                }

                int waveOutDevices = WaveOut.DeviceCount;
                for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
                {
                    WaveOutCapabilities deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
                    string device = string.Format("Device {0}: {1}, {2} channels",
                        waveOutDevice, deviceInfo.ProductName, deviceInfo.Channels);
                    ComboDevicesPlayback.Items.Add(device);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("There was an error getting audio devices: " + ex.Message);
            }

            ComboDevicesRecording.SelectedIndex = 0;
            ComboDevicesPlayback.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles event Button_Click when OK button on window is clicked
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (elliesPhatBeats != null)
            {
                elliesPhatBeats.StopRecording();
                if (elliesPhatPlayer != null)
                {
                    elliesPhatPlayer.Stop();
                }
                elliesPhatBeats = null;
                elliesPhatPlayer = null;

            }
            else
            {
                // elliesPhatBeats instatiated as WaveIn class with selected recording device
                elliesPhatBeats = new WaveIn();
                elliesPhatBeats.DeviceNumber = ComboDevicesRecording.SelectedIndex;
                elliesPhatBeats.WaveFormat = new WaveFormat(SampleRate, 1 /* 1 channel = mono */);
                // calls OnDataAvailable event handler when data is available in elliesPhatBeats from the recording device
                elliesPhatBeats.DataAvailable += OnDataAvailable;
                elliesPhatBeats.BufferMilliseconds = 25;
                elliesPhatBeats.RecordingStopped += AutomaticStop;

                //elliesPhatPlayer instatiated as WaveOut class with selected playback device
                elliesPhatPlayer = new WaveOut();
                elliesPhatPlayer.DeviceNumber = ComboDevicesPlayback.SelectedIndex;
                elliesPhatPlayer.DesiredLatency = 100;
                elliesPhatPlayer.PlaybackStopped += AutomaticStop;

                // We want to now set up the following chain of audio:
                // elliesPhatBeats -> BufferedWaveProvider -> AudioAnalyser -> elliesPhatPlayer

                // creates a BufferedWaveProvider class (essentially a data array for storing recorded sound data) called bwp with waveformat of the recording input
                bwp = new BufferedWaveProvider(elliesPhatBeats.WaveFormat);
                // initialises the playback to use the data from the buffered wave provider
                analyser = new AudioAnalyser(bwp, FFT, Playback);
                analyser.bpc = Bandpass;
                elliesPhatPlayer.Init(analyser);

                // tells player to play and recorder to record
                elliesPhatPlayer.Play();
                elliesPhatBeats.StartRecording();
            }
        }

        void AutomaticStop(object sender, StoppedEventArgs e)
        {
            if (elliesPhatBeats != null)
            {
                elliesPhatBeats.StopRecording();
                elliesPhatBeats = null;
            }
            if (elliesPhatPlayer != null)
            {
                elliesPhatPlayer.Stop();
                elliesPhatPlayer = null;
            }
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
            WaveBuffer wb = new WaveBuffer(e.Buffer);

            //DrawWaveform(Waveform, wb.ShortBuffer, e.BytesRecorded/2);
            ffnumber.Content = analyser.Fundamental;
        }

        private void CrossBufferSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (analyser != null)
            {
                analyser.CrossfadeSampleSize = (int)CrossBufferSizeSlider.Value;
            }
        }

        private void GraphScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (analyser != null)
            {
                analyser.GraphScale = GraphScaleSlider.Value;
            }
        }
    }
}
