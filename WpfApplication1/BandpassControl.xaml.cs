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
        public int WindowSize { get { int v; if (!int.TryParse(textBox.Text, out v)) v = 4096; return v; } }
        public bool ByPass {  get { return (bool)Bypass.IsChecked; } }
        public double Gain = 4.0;

        public BandpassControl()
        {
            InitializeComponent();
        }

        private void Low_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LowLabel != null)
            {
                LowValue = (int)(Math.Pow(Low.Value, 2) * (32768 / 2));
                LowLabel.Content = LowValue;
            }
        }

        private void High_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HighLabel != null)
            {
                HighValue = (int)(Math.Pow(High.Value, 2) * (32768 / 2));
                HighLabel.Content = HighValue;
            }
        }
    }
}
