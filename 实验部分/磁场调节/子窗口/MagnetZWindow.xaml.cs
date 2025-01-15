using Controls.Charts;
using ODMR_Lab.Windows;
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
    public partial class MagnetZWindow : Window
    {
        private CWPointObject CWPoint1 { get; set; } = null;

        private CWPointObject CWPoint2 { get; set; } = null;

        private NumricDataSeries LineCW1 = new NumricDataSeries("CW", new List<double>(), new List<double>()) { LineColor = Colors.LightBlue, LineThickness = 1, Smooth = false, MarkerSize = 0 };
        private NumricDataSeries LineCW2 = new NumricDataSeries("CW", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, LineThickness = 1, Smooth = false, MarkerSize = 0 };

        public MagnetZWindow(string windowtitle)
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

        public void ClearPoints()
        {
            CWChart.DataList.Clear();

            CWPoint1 = null;
            CWPoint2 = null;

            L1_1.Content = "";
            L1_2.Content = "";
            L1_3.Content = "";
            L1_4.Content = "";
            L1_5.Content = "";
            L1_6.Content = "";

            L2_1.Content = "";
            L2_2.Content = "";
            L2_3.Content = "";
            L2_4.Content = "";
            L2_5.Content = "";
            L2_6.Content = "";
        }

        private void CWCheckChanged(object sender, RoutedEventArgs e)
        {
            UpdateCW();
        }

        private void UpdateCW()
        {
            CWChart.DataList.Clear();
            if (CW1.IsChecked == true)
            {
                CWChart.DataList.Add(LineCW1);
            }
            if (CW2.IsChecked == true)
            {
                CWChart.DataList.Add(LineCW2);
            }
        }

        private void ShowPoint1CW(object sender, MouseButtonEventArgs e)
        {
            if (CWPoint1 == null) return;
            CWChart.DataList.Clear();
            LineCW1.ClearData();
            for (int i = 0; i < CWPoint1.CW1Freqs.Count; i++)
            {
                LineCW1.AddData(CWPoint1.CW1Freqs[i], CWPoint1.CW1Values[i]);
            }
            CWChart.RefreshPlotWithAutoScale();
        }

        private void ShowPoint2CW(object sender, MouseButtonEventArgs e)
        {
            if (CWPoint2 == null) return;
            CWChart.DataList.Clear();
            LineCW2.ClearData();
            for (int i = 0; i < CWPoint2.CW2Freqs.Count; i++)
            {
                LineCW1.AddData(CWPoint2.CW2Freqs[i], CWPoint2.CW2Values[i]);
            }
            CWChart.RefreshPlotWithAutoScale();
        }

        private void SnapPlotCW(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(CWChart));
            TimeWindow window = new TimeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }
    }
}
