using MathNet.Numerics.Distributions;
using ODMR_Lab.位移台部分;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// OffsetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScanInitWindow : Window
    {
        public double ZPlane { get; set; } = double.NaN;

        public double XLo { get; set; } = double.NaN;

        public double XHi { get; set; } = double.NaN;

        public double YLo { get; set; } = double.NaN;

        public double YHi { get; set; } = double.NaN;

        public double D { get; set; } = double.NaN;

        private bool result = false;


        public ScanInitWindow(Window owner = null)
        {
            InitializeComponent();
            if (owner != null)
            {
                Owner = owner;
                owner.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        public new bool ShowDialog()
        {
            base.ShowDialog();
            return result;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            try
            {
                XLo = double.Parse(XLoText.Text);
                XHi = double.Parse(XHiText.Text);
                YLo = double.Parse(YLoText.Text);
                YHi = double.Parse(YHiText.Text);
                ZPlane = double.Parse(ZPlaneText.Text);
                D = double.Parse(DText.Text);
                result = true;
                Close();
            }
            catch (Exception ex) { return; }
        }
    }
}
