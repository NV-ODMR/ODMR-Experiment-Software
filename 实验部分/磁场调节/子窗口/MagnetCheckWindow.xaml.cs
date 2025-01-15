using Controls.Charts;
using ODMR_Lab.实验部分.磁场调节;
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
using System.Windows.Shapes;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// MagnetZWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MagnetCheckWindow : Window
    {
        public CWPointObject CWPoint1 { get; private set; } = null;

        public CWPointObject CWPoint2 { get; private set; } = null;

        private NumricDataSeries CWLine1 { get; set; } = new NumricDataSeries("CW", new List<double>(), new List<double>()) { LineThickness = 3, LineColor = Colors.LightBlue };
        private NumricDataSeries CWLine2 { get; set; } = new NumricDataSeries("CW", new List<double>(), new List<double>()) { LineThickness = 3, LineColor = Colors.LightGreen };

        public MagnetCheckWindow(string windowtitle)
        {
            InitializeComponent();
            Title = windowtitle;
            WindowTitle.Content = windowtitle;
            CodeHelper.WindowResizeHelper helper = new CodeHelper.WindowResizeHelper();
            helper.RegisterWindow(this, dragHeight: 30);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        public void SetPoint1(CWPointObject point)
        {
            CWPoint1 = point;
            L1_1.Content = Math.Round(point.MoverLoc, 4).ToString();
            L1_2.Content = Math.Round(point.CW1, 4).ToString();
            L1_3.Content = Math.Round(point.CW2, 4).ToString();
            L1_4.Content = Math.Round(point.Bv, 4).ToString();
            L1_5.Content = Math.Round(point.Bp, 4).ToString();
            L1_6.Content = Math.Round(point.B, 4).ToString();
        }

        public void SetTarget1(double x, double y, double z, double angle)
        {
            TL1_1.Content = Math.Round(x, 4).ToString();
            TL1_2.Content = Math.Round(y, 4).ToString();
            TL1_3.Content = Math.Round(z, 4).ToString();
            TL1_4.Content = Math.Round(angle, 4).ToString();
        }

        public void SetTarget2(double x, double y, double z, double angle)
        {
            TL2_1.Content = Math.Round(x, 4).ToString();
            TL2_2.Content = Math.Round(y, 4).ToString();
            TL2_3.Content = Math.Round(z, 4).ToString();
            TL2_4.Content = Math.Round(angle, 4).ToString();
        }

        public void SetPoint2(CWPointObject point)
        {
            CWPoint2 = point;
            L2_1.Content = Math.Round(point.MoverLoc, 4).ToString();
            L2_2.Content = Math.Round(point.CW1, 4).ToString();
            L2_3.Content = Math.Round(point.CW2, 4).ToString();
            L2_4.Content = Math.Round(point.Bv, 4).ToString();
            L2_5.Content = Math.Round(point.Bp, 4).ToString();
            L2_6.Content = Math.Round(point.B, 4).ToString();
        }


        private void ShowCW1(object sender, MouseButtonEventArgs e)
        {
            CWLine1.ClearData();
            if (CWPoint1 == null) return;
            for (int i = 0; i < CWPoint1.CW1Freqs.Count; i++)
            {
                CWLine1.AddData(CWPoint1.CW1Freqs[i], CWPoint1.CW1Values[i]);
            }
            for (int i = 0; i < CWPoint1.CW2Freqs.Count; i++)
            {
                CWLine2.AddData(CWPoint1.CW2Freqs[i], CWPoint1.CW2Values[i]);
            }
            CWChart.RefreshPlotWithAutoScale();
        }

        private void ShowCW2(object sender, MouseButtonEventArgs e)
        {
            CWChart.RefreshPlotWithAutoScale();
        }

        private void MovetoP2(object sender, RoutedEventArgs e)
        {

        }

        private void MovetoP1(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 刷新计算
        /// </summary>
        public void UpdateCalculate(MagnetScanParams P)
        {
            Dispatcher.Invoke(() =>
            {
                Calculate(P, out double x1, out double y1, out double z1, out double angle1, out double x2, out double y2, out double z2, out double angle2);
                SetTarget1(x1, y1, z1, angle1);
                SetTarget2(x2, y2, z2, angle2);
            });
        }

        public void Calculate(MagnetScanParams P, out double x1, out double y1, out double z1, out double angle1, out double x2, out double y2, out double z2, out double angle2)
        {
            #region 第一个方向
            List<double> res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, P.Theta1.Value, P.Phi1.Value, P.ZDistance.Value);
            double ang = res[0] * P.ReverseANum.Value;
            double dx = res[1] * P.ReverseXNum.Value;
            double dy = res[2] * P.ReverseYNum.Value;
            ang = P.StartAngle.Value + ang;
            while (ang > 360)
            {
                ang -= 360;
            }
            while (ang < -360)
            {
                ang += 360;
            }
            if (Math.Abs(ang) > 150)
            {
                res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, Math.PI - P.Theta1.Value, P.Phi1.Value - Math.PI, P.ZDistance.Value);
                ang = res[0] * P.ReverseANum.Value;
                dx = res[1] * P.ReverseXNum.Value;
                dy = res[2] * P.ReverseYNum.Value;
                ang = P.StartAngle.Value + ang;
                while (ang > 360)
                {
                    ang -= 360;
                }
                while (ang < -360)
                {
                    ang += 360;
                }
            }
            //根据需要移动的角度进行偏心修正
            List<double> re = MagnetAutoScanHelper.GetTargetOffset(P, ang);
            x1 = P.XLoc.Value + re[0] + dx;
            y1 = P.YLoc.Value + re[1] + dy;
            z1 = P.ZLoc.Value;
            angle1 = ang;

            #endregion

            #region 第二个方向
            res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, P.Theta2.Value, P.Phi1.Value, P.ZDistance.Value);
            ang = res[0] * P.ReverseANum.Value;
            dx = res[1] * P.ReverseXNum.Value;
            dy = res[2] * P.ReverseYNum.Value;
            ang = P.StartAngle.Value + ang;
            while (ang > 360)
            {
                ang -= 360;
            }
            while (ang < -360)
            {
                ang += 360;
            }
            if (Math.Abs(ang) > 150)
            {
                res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, Math.PI - P.Theta2.Value, P.Phi1.Value - Math.PI, P.ZDistance.Value);
                ang = res[0] * P.ReverseANum.Value;
                dx = res[1] * P.ReverseXNum.Value;
                dy = res[2] * P.ReverseYNum.Value;
                ang = P.StartAngle.Value + ang;
                while (ang > 360)
                {
                    ang -= 360;
                }
                while (ang < -360)
                {
                    ang += 360;
                }
            }
            //根据需要移动的角度进行偏心修正
            re = MagnetAutoScanHelper.GetTargetOffset(P, ang);
            x2 = P.XLoc.Value + re[0] + dx;
            y2 = P.YLoc.Value + re[1] + dy;
            z2 = P.ZLoc.Value;
            angle2 = ang;

            #endregion


        }

        private void SnapPlotCW(object sender, RoutedEventArgs e)
        {

        }
    }
}
