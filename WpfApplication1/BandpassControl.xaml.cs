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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for BandpassControl.xaml
    /// </summary>
    public partial class BandpassControl : UserControl
    {
        public int LowValue;
        public int HighValue;
        public int WindowSize { get { int v; if (textBox == null || !int.TryParse(textBox.Text, out v)) v = 4096; return v; } }
        public bool ByPass { get { return (bool)Bypass.IsChecked; } }
        public bool Invert { get { return (bool)InvertCheck.IsChecked; } }
        public double Gain { get { return GainSlider.Value; } }

        public BandpassControl()
        {
            InitializeComponent();
        }

        private void Low_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LowLabel != null)
            {
                double scaled = Low.Value * Math.Log(WindowSize, 10);
                LowValue = (int)(Math.Pow(10, scaled));
                LowLabel.Content = LowValue*44100/WindowSize;
            }
        }

        private void High_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HighLabel != null)
            {
                double scaled = High.Value * Math.Log(WindowSize, 10);
                HighValue = (int)(Math.Pow(10, scaled));
                HighLabel.Content = HighValue*44100/WindowSize;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GainLabel != null)
            {
                GainLabel.Content = GainSlider.Value;
            }

        }
    }
}
