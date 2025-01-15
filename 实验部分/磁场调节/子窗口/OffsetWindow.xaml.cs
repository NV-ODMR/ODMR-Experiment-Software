using MathNet.Numerics.Distributions;
using ODMR_Lab.Windows;
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
    public partial class OffsetWindow : Window
    {
        double AngleStart { get; set; }

        public double OffsetX { get; set; } = double.NaN;

        public double OffsetY { get; set; } = double.NaN;


        public OffsetWindow(double AngleStart)
        {
            this.AngleStart = AngleStart;
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 计算偏移量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calc(object sender, RoutedEventArgs e)
        {
            try
            {
                double x1 = double.Parse(X1.Text);
                double y1 = double.Parse(Y1.Text);
                double x2 = double.Parse(X2.Text);
                double y2 = double.Parse(Y2.Text);

                double angle1 = double.Parse(Angle1.Text);

                bool reverseX = DeviceDispatcher.TryGetMoverDevice(MoverTypes.X, OperationMode.Read, PartTypes.Magnnet, showmessagebox: true).IsReverse;

                bool reverseY = DeviceDispatcher.TryGetMoverDevice(MoverTypes.Y, OperationMode.Read, PartTypes.Magnnet, showmessagebox: true).IsReverse;

                bool reverseAngle = DeviceDispatcher.TryGetMoverDevice(MoverTypes.AngleZ, OperationMode.Read, PartTypes.Magnnet, showmessagebox: true).IsReverse;


                MagnetAutoScanHelper.GetZeroOffset(angle1, x1, y1, angle1 - 180, x2, y2, reverseX, reverseY, reverseAngle, AngleStart, out double offx, out double offy);
                OffsetX = offx;
                OffsetY = offy;

                Close();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("计算偏移量失败，请检查位移台连接及参数设置情况", this);
            }
        }

    }
}
