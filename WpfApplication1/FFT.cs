using System;
using System.Collections.Generic;
using NAudio.Dsp;

namespace Fourier
{
    class Frequency
    {
        public int freq;
        public double pow;
    }
    class FFT
    {

        public double[] vector;
        public int fundamentalFrequency;
        public double fundamentalPower;
        public double maxSignal;

        public FFT()
        {
        }

        private void Swap(double[] array, int i, int j)
        {
            double t=  array[i];
            array[i] = array[j];
            array[j] = t;
        }


        public void Convert1DData(short[] data, int samples, int sample_rate)
        {
            double DCOffset = 0;
            vector = new double[2 * sample_rate];
            for (int n = 0; n < sample_rate; n++)
            {
                if (n < samples)
                {
                    vector[2 * n] = data[n] / 32768f;// *FastFourierTransform.HannWindow(n, samples);
                    DCOffset += vector[2 * n];
                }
                else
                {
                    vector[2 * n] = 0;
                }
                vector[2 * n + 1] = 0;
            }
            DCOffset /= Math.Min(samples, sample_rate);
            for (int n = 0; n < sample_rate; n++)
            {
                if (n < samples)
                {
                    vector[2 * n] -= DCOffset;
                }
            }
        }

        public void Convert1DData(float[] data, int sample_rate)
        {
            vector = new double[2 * sample_rate];
            for (int n = 0; n < sample_rate; n++)
            {
                if (n < data.Length)
                {
                    vector[2 * n] = data[n];
                }
                else
                {
                    vector[2 * n] = 0;
                }
                vector[2 * n + 1] = 0;
            }
        }

        public void Convert1DData(double[] data, int sample_rate)
        {
            vector = new double[2 * sample_rate];
            for (int n = 0; n < sample_rate; n++)
            {
                if (n < data.Length)
                {
                    vector[2 * n] = data[n];
                }
                else
                {
                   vector[2 * n] = 0;
                }
                vector[2 * n + 1] = 0;
            }
        }

        // FFT 1D
        public void ComplexFFT(int sign)
        {

	        //variables for the fft
	        int n,mmax,m,j,istep,i;
	        double wtemp,wr,wpr,wpi,wi,theta,tempr,tempi;

	        //binary inversion (note that the indexes 
            //start from 0 witch means that the
            //real part of the complex is on the even-indexes 
            //and the complex part is on the odd-indexes)
	        n=vector.Length;
	        j=0;
	        for (i=0;i<n/2;i+=2) 
            {
		        if (j > i) 
                {
			        Swap(vector,j,i);
			        Swap(vector,j+1,i+1);
			        if((j/2)<(n/4))
                    {
				        Swap(vector,(n-(i+2)),(n-(j+2)));
				        Swap(vector,(n-(i+2))+1,(n-(j+2))+1);
			        }
		        }
		        m=n >> 1;
		        while (m >= 2 && j >= m)
                {
			        j -= m;
			        m >>= 1;
		        }
		        j += m;
	        }
	        //end of the bit-reversed order algorithm

	        //Danielson-Lanzcos routine
	        mmax=2;
	        while (n > mmax) 
            {
		        istep=mmax << 1;
		        theta=sign*(2*Math.PI/mmax);
		        wtemp=Math.Sin(0.5*theta);
		        wpr = -2.0*wtemp*wtemp;
		        wpi=Math.Sin(theta);
		        wr=1.0;
		        wi=0.0;
		        for (m=1;m<mmax;m+=2) 
                {
			        for (i=m;i<=n;i+=istep) 
                    {
				        j=i+mmax;
				        tempr=wr*vector[j-1]-wi*vector[j];
				        tempi=wr*vector[j]+wi*vector[j-1];
				        vector[j-1]=vector[i-1]-tempr;
				        vector[j]=vector[i]-tempi;
				        vector[i-1] += tempr;
				        vector[i] += tempi;
			        }
			        wr=(wtemp=wr)*wpr-wi*wpi+wr;
			        wi=wi*wpr+wtemp*wpi+wi;
		        }
		        mmax=istep;
	        }
	        //end of the algorithm

            if (sign == -1)
            {
                double scale = 1.0 / (double)vector.Length;
                for (i = 0; i < vector.Length; i++)
                {
                    vector[i] *= scale;
                }
            }
            if (sign == 1)
            {
                //determine the fundamental frequency
                //look for the maximum absolute value in the complex array
                fundamentalFrequency = 0;
                fundamentalPower = 0;
                for (i = 2; i < vector.Length / 2; i += 2)
                {
                    double power = (Math.Pow(vector[i], 2) + Math.Pow(vector[i + 1], 2));
                    if (power > fundamentalPower)
                    {
                        fundamentalFrequency = i;
                        fundamentalPower = power;
                    }
                }

                //since the array of complex has the format [real][complex]=>[absolute value]
                //the maximum absolute value must be adjusted to half
                fundamentalFrequency = (int)Math.Floor((double)fundamentalFrequency / 2);
            }
            else
            {
                maxSignal = 0;
                for (i = 0; i < vector.Length / 2; i += 2)
                {
                    if (vector[i] > maxSignal)
                    {
                        maxSignal = vector[i];
                    }
                }
            }
        }

        public void FindThresholdFrequencies(List<Frequency> frequencies, double threshold, int minpeakwidth)
        {
            int i;
            Frequency fstart = new Frequency();
            fstart.freq=0;
            fstart.pow=0;
            frequencies.Add(fstart);
            for (i = 2; i < vector.Length/2; i += 2)
            {
                double power = (Math.Pow(vector[i], 2) + Math.Pow(vector[i + 1], 2));
                if ( power > threshold && power > frequencies[frequencies.Count-1].pow )
                {
                    if( i-frequencies[frequencies.Count-1].freq < minpeakwidth )
                    {
                        frequencies[frequencies.Count - 1].freq = i;
                        frequencies[frequencies.Count - 1].pow = power;
                    }
                    else 
                    {
                        Frequency newf = new Frequency();
                        newf.freq = i;
                        newf.pow = power;
                        frequencies.Add(newf);
                    }
                }
            }
        }


    }
}
