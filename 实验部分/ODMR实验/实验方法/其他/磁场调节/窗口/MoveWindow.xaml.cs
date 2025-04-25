using CodeHelper;
using Controls.Windows;
using MathLib.NormalMath.Decimal;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Vector = System.Windows.Vector;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// OffsetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MoveWindow : Window
    {
        private MagnetLoc exp = null;

        public MoveWindow(MagnetLoc parentexp)
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
        private void Move(object sender, RoutedEventArgs e)
        {
            try
            {
                double x = double.Parse(X.Text);
                double y = double.Parse(Y.Text);
                double z = double.Parse(Z.Text);
                double angle = double.Parse(Angle.Text);
                exp.GetDevices();
                var mx = exp.GetDeviceByName("MagnetX") as NanoStageInfo;
                var my = exp.GetDeviceByName("MagnetY") as NanoStageInfo;
                var mz = exp.GetDeviceByName("MagnetZ") as NanoStageInfo;
                var ma = exp.GetDeviceByName("MagnetAngle") as NanoStageInfo;
                Thread t = new Thread(() =>
                  {
                      MessageWindow win = null;
                      App.Current.Dispatcher.Invoke(() =>
                      {
                          win = new MessageWindow("提示", "正在移动...", MessageBoxButton.OK, false, false);
                          win.Owner = this;
                          win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                          win.Show();
                      });
                      DeviceDispatcher.UseDevices(mx, my, mz, ma);
                      mx.Device.MoveToAndWait(x, 10000);
                      my.Device.MoveToAndWait(y, 10000);
                      mz.Device.MoveToAndWait(z, 10000);
                      ma.Device.MoveToAndWait(angle, 60000);
                      DeviceDispatcher.EndUseDevices(mx, my, mz, ma);
                      App.Current.Dispatcher.Invoke(() =>
                      {
                          win.Close();
                      });
                  });
                t.Start();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("移动未完成", this);
            }
            ;
        }

        // 计算绕任意轴旋转指定角度的旋转矩阵
        private RealMatrix CalculateRotationMatrix(Vector3D axis, double angleInRadians)
        {
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            double oneMinusCos = 1 - cosTheta;

            double x = axis.X;
            double y = axis.Y;
            double z = axis.Z;

            double xSq = x * x;
            double ySq = y * y;
            double zSq = z * z;

            double xy = x * y;
            double yz = y * z;
            double zx = z * x;

            double xSin = x * sinTheta;
            double ySin = y * sinTheta;
            double zSin = z * sinTheta;

            double m11 = cosTheta + xSq * oneMinusCos;
            double m12 = xy * oneMinusCos - zSin;
            double m13 = zx * oneMinusCos + ySin;

            double m21 = xy * oneMinusCos + zSin;
            double m22 = cosTheta + ySq * oneMinusCos;
            double m23 = yz * oneMinusCos - xSin;

            double m31 = zx * oneMinusCos - ySin;
            double m32 = yz * oneMinusCos + xSin;
            double m33 = cosTheta + zSq * oneMinusCos;

            var mat = new RealMatrix(3, 3);
            mat.Content = new List<List<double>> { new List<double> { m11, m12, m13 },
                    new List<double>() { m21, m22, m23 },
                    new List<double>() { m31, m32, m33 } };

            return mat;
        }

        private void MoveAngle(object sender, RoutedEventArgs e)
        {
            try
            {
                double offx = exp.GetInputParamValueByName("OffsetX");
                double offy = exp.GetInputParamValueByName("OffsetY");
                double angle = double.Parse(RotAngle.Text);
                var xy = MagnetScanTool.GetTargetOffset(angle, offx, offy, exp.GetInputParamValueByName("ReverseX"),
                     exp.GetInputParamValueByName("ReverseY"), exp.GetInputParamValueByName("ReverseA"), exp.GetInputParamValueByName("AngleStart"));
                double centerx = double.Parse(RotX.Text);
                double centery = double.Parse(RotY.Text);
                exp.GetDevices();
                var mx = exp.GetDeviceByName("MagnetX") as NanoStageInfo;
                var my = exp.GetDeviceByName("MagnetY") as NanoStageInfo;
                var mz = exp.GetDeviceByName("MagnetZ") as NanoStageInfo;
                var ma = exp.GetDeviceByName("MagnetAngle") as NanoStageInfo;
                Thread t = new Thread(() =>
                {
                    MessageWindow win = null;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        win = new MessageWindow("提示", "正在移动...", MessageBoxButton.OK, false, false);
                        win.Owner = this;
                        win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        win.Show();
                    });
                    DeviceDispatcher.UseDevices(mx, my, mz, ma);
                    mx.Device.MoveToAndWait(centerx + xy[0], 10000);
                    my.Device.MoveToAndWait(centery + xy[1], 10000);
                    ma.Device.MoveToAndWait(angle, 10000);
                    DeviceDispatcher.EndUseDevices(mx, my, mz, ma);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        win.Close();
                    });
                });
                t.Start();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("移动未完成", this);
            }
        }
    }
}
