using CodeHelper;
using Controls.Windows;
using MathNet.Numerics.Distributions;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.磁场调节;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// OffsetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MIntensityWindow : Window
    {

        public double OffsetX { get; set; } = double.NaN;

        public double OffsetY { get; set; } = double.NaN;

        private MagnetLoc exp = null;

        public MIntensityWindow(MagnetLoc parentexp)
        {
            exp = parentexp;
            InitializeComponent();
            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterWindow(this, null, null, null, 0, 30);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// 计算偏移量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calc(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsHitTestVisible = false;
                    });
                    exp.GetDevices();
                    var x = exp.GetDeviceByName("MagnetX") as NanoStageInfo;
                    var y = exp.GetDeviceByName("MagnetY") as NanoStageInfo;
                    var z = exp.GetDeviceByName("MagnetZ") as NanoStageInfo;
                    var a = exp.GetDeviceByName("MagnetAngle") as NanoStageInfo;
                    double zloc = double.Parse(exp.GetOutputParamValueByName("ZLoc"));
                    double zdis = double.Parse(exp.GetOutputParamValueByName("ZDistance"));
                    DeviceDispatcher.UseDevices(x, y, z, a);
                    x.Device.MoveToAndWait(exp.GetOutputParamValueByName("XLoc"), 10000);
                    y.Device.MoveToAndWait(exp.GetOutputParamValueByName("YLoc"), 10000);
                    z.Device.MoveToAndWait(zloc, 10000);
                    exp.TotalCWPeaks2OrException(out var peaks, out var freqs, out var contracts);
                    exp.CalculateB(peaks[0], peaks[1], out var bp, out var bv, out var b);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        MeasureB.Text = b.ToString();
                        MIntensity.Text = (b /
                        new Magnet(exp.GetInputParamValueByName("MRadius"),
                        exp.GetInputParamValueByName("MLength"), 1).GetIntensityBField(zdis, 0, 0)).ToString();
                    });
                    DeviceDispatcher.EndUseDevices(x, y, z, a);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsHitTestVisible = true;
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsHitTestVisible = true;
                    });
                    MessageWindow.ShowTipWindow("测量失败，请检查位移台连接及参数设置情况", this);
                }
            });
            t.Start();
        }

    }
}
