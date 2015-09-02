using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using Fourier;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Drawing;
using NAudio.Dsp;


namespace WpfApplication1
{
    class AudioAnalyser: IWaveProvider
    {
        private IWaveProvider source;
        private WaveBuffer waveBuffer;
        public FFT fft;
        private Image FFTImage, WaveImage;
        public BandpassControl bpc { get; set; }
        public int Fundamental {  get { return fft.fundamentalFrequency;  } }
        WriteableBitmap Wavemap;
        WriteableBitmap FFTmap;
        public int CrossfadeSampleSize;
        public double GraphScale = 1.0;
        byte[] Clear;


        public AudioAnalyser(IWaveProvider source, Image image, Image image2)
        {
            this.source = source;
            fft = new FFT();
            FFTImage = image;
            WaveImage = image2;
            CrossfadeSampleSize = 20;
            FFTmap = new WriteableBitmap((int)image.Width, (int)image.Height, 96, 96, PixelFormats.Bgr32, null);
            Clear = new byte[(int)image.Width * (int)image.Height * 4];
            FFTmap.CopyPixels(Clear, (int)image.Width * 4, 0);
        }
        public WaveFormat WaveFormat
        {
            get
            {
                return source.WaveFormat;
            }
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            if (waveBuffer == null || waveBuffer.MaxSize < count)
            {
                waveBuffer = new WaveBuffer(count);
            }

            int bytesRead = source.Read(waveBuffer.ByteBuffer, 0, count);
 
            // the last bit sometimes needs to be rounded up:
            if (bytesRead > 0) bytesRead = count;

            int samples = bytesRead / sizeof(short);
            
            Wavemap = new WriteableBitmap((int)WaveImage.Width, (int)WaveImage.Height, 96, 96, PixelFormats.Bgr32, null);
            WaveImage.Source = Wavemap;
            FFTmap.WritePixels(new Int32Rect(0, 0, (int)FFTmap.Width, (int)FFTmap.Height), Clear, (int)FFTmap.Width * 4, 0);
            //DrawWaveform(WaveImage, waveBuffer.ShortBuffer, samples,200,100,50);
            fft.Convert1DData(waveBuffer.ShortBuffer, samples, bpc.WindowSize);
            DrawWaveform(WaveImage, fft.vector, samples, 200, 100, 50);

            if (!bpc.ByPass)
            {
                fft.ComplexFFT(1);

                //DrawFFT(FFTImage, fft, 50, 100, 200);
                fft.vector[0] = fft.vector[1] = 0;
                for (int i = fft.vector.Length / 2; i < fft.vector.Length; i++)
                {
                    fft.vector[i] = 0;
                }
#if false
                for (int i = 1; i < fft.vector.Length / 4; i++)
                {
                    int width = 40;
                    double scale = FastFourierTransform.HannWindow(width/2 + i-bpc.LowValue, width);
                    if (i < bpc.LowValue - width/2) scale = 0;
                    if (i > bpc.LowValue + width/2) scale = 0;
                    fft.vector[i * 2] *= scale;
                    fft.vector[i * 2 + 1] *= scale;
                }
#else
                for (int i = 1; i < bpc.LowValue; i++)
                {
                    fft.vector[i * 2] = 0;
                    fft.vector[i * 2 + 1] = 0;
                }

                for (int i = fft.vector.Length / 4; i > bpc.HighValue; i--)
                {
                    fft.vector[i * 2] = 0;
                    fft.vector[i * 2 + 1] = 0;
                }
                //DrawFFT(FFTImage, fft, 200, 100, 50);
#endif
                fft.ComplexFFT(-1);
            }
            WaveBuffer outBuffer = new WaveBuffer(buffer);
            // 
            for (int i = 0; i < CrossfadeSampleSize; i++)
            {
                double target = fft.vector[(i % bpc.WindowSize) * 2];
                double prop = ((double)i) / CrossfadeSampleSize;
                short val = (short)(target*prop * 32768f);
                outBuffer.ShortBuffer[i] = (short)(val * bpc.Gain);
            }
            // 
            for (int i = CrossfadeSampleSize; i < samples - CrossfadeSampleSize; i++)
            {
                double dv = fft.vector[(i % bpc.WindowSize) * 2];
                short val = (short)(dv*32768f);
                outBuffer.ShortBuffer[i] = (short)(val * bpc.Gain);
            }
            // 
            for (int i = samples - CrossfadeSampleSize; i < samples; i++)
            {
                double target = fft.vector[(i % bpc.WindowSize) * 2];
                double prop = 1.0 - ((double)i - (samples-CrossfadeSampleSize)) / CrossfadeSampleSize;
                short val = (short)(target * prop * 32768f);
                outBuffer.ShortBuffer[i] = (short)(val * bpc.Gain);
            }
            //DrawWaveform(WaveImage, fft.vector, samples, 50, 100, 200);
            DrawWaveform(WaveImage, outBuffer.ShortBuffer, samples,50,200,100);
            return count;
        }

        void DrawFFT(Image target, FFT fft, byte r, byte g, byte b)
        {
           

            byte[] colorData = new byte[(int)target.Height * 4];
            for(int i = 0 ; i< (int)target.Height ; i++)
            {
                colorData[i * 4 + 0] = b;
                colorData[i * 4 + 1] = g;
                colorData[i * 4 + 2] = r;
                colorData[i * 4 + 3] = 255;
            }

            for (int x = 1; x < (int)target.Width; x++)
            {
                int index = x * bpc.WindowSize / (int)target.Width / 16;
                double sample = (Math.Pow(fft.vector[index], 2) + Math.Pow(fft.vector[index + 1], 2)) / fft.fundamentalPower;
                int y = Math.Min((int)target.Height-1, (int)(sample * target.Height));
                FFTmap.WritePixels(new Int32Rect(x, (int)target.Height-y, 1, y), colorData, 4, 0);
            }
            target.Source = FFTmap;
        }
        void DrawWaveform(Image target, short[] convertedsamples, int convertedarraysize, byte b, byte g, byte r)
        {
            byte[] colorData = { b, g, r, 255 };

            for (int x = 0; x < (int)target.Width; x++)
            {
                float sample = convertedsamples[Math.Min(convertedarraysize,(int)(x * GraphScale))] / 32768f;
                int y = Math.Min((int)target.Height - 1, (int)(sample * target.Height / 2 + (target.Height / 2)));
                Wavemap.WritePixels(new Int32Rect(x, y, 1, 1), colorData, 4, 0);
            }
        }
        void DrawWaveform(Image target, double[] convertedsamples, int convertedarraysize, byte b, byte g, byte r)
        {
            byte[] colorData = { b, g, r, 255 };

            for (int x = 0; x < (int)target.Width; x++)
            {
                float sample = (float)convertedsamples[Math.Min(convertedarraysize,(int)(x * GraphScale))*2];
                int y = Math.Min((int)target.Height - 1, (int)(sample * target.Height / 2 + (target.Height / 2)));
                Wavemap.WritePixels(new Int32Rect(x, y, 1, 1), colorData, 4, 0);
            }
        }
    }
}
