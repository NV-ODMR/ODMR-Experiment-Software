using CodeHelper;
using CodeHelper.ResizeElements;
using Controls.Charts;
using MathNet.Numerics;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.Collections.Generic;
using System.Data;
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
using Window = System.Windows.Window;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// MagnetXYZWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MagnetXYAngleWindow : Window
    {
        private List<CWPointObject> CWPoints = new List<CWPointObject>();

        private NumricDataSeries LineBp = new NumricDataSeries("Bp", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, LineThickness = 3, Smooth = false, MarkerSize = 0 };

        private NumricDataSeries LineBv = new NumricDataSeries("Bv", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, LineThickness = 3, Smooth = false, MarkerSize = 0 };

        private NumricDataSeries LineB = new NumricDataSeries("B", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, LineThickness = 3, Smooth = false, MarkerSize = 0 };

        private NumricDataSeries LineCW1 = new NumricDataSeries("CW", new List<double>(), new List<double>()) { LineColor = Colors.LightBlue, LineThickness = 3, Smooth = false, MarkerSize = 0 };
        private NumricDataSeries LineCW2 = new NumricDataSeries("CW", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, LineThickness = 3, Smooth = false, MarkerSize = 0 };

        private CWPointObject CurrentPoint = null;

        private bool IsAngleWindow = false;

        public MagnetXYAngleWindow(string windowtitle, bool isAngleWindow)
        {
            InitializeComponent();
            IsAngleWindow = isAngleWindow;
            Title = windowtitle;
            WindowTitle.Content = windowtitle;
            CodeHelper.WindowResizeHelper helper = new CodeHelper.WindowResizeHelper();
            helper.RegisterWindow(this, dragHeight: 30);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshCheck();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void UpdateData()
        {

        }

        private StackPanel CreateInformationBar(CWPointObject data)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            List<double> datas = new List<double>() { data.MoverLoc, data.CW1, data.CW2, data.Bp, data.Bv, data.B };
            foreach (var item in datas)
            {
                Label l = new Label();
                l.Foreground = Brushes.White;
                l.Width = 100;
                l.Height = 45;
                l.FontSize = 10;
                l.FontWeight = FontWeights.Thin;
                l.Content = Math.Round(item, 4).ToString();
                l.ToolTip = Math.Round(item, 4).ToString();
                panel.Children.Add(l);
            }
            panel.MouseLeftButtonDown += SelectPoint;
            MouseColorHelper helper = new MouseColorHelper(Background, BtnTemplate.MoveInColor, BtnTemplate.PressedColor);
            panel.Tag = data;
            helper.RegistateTarget(panel);
            return panel;
        }

        private void SelectPoint(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in DataViewer.Children)
            {
                (item as StackPanel).IsEnabled = true;
            }
            (sender as StackPanel).IsEnabled = false;
            (sender as StackPanel).Background = BtnTemplate.PressedColor;
            LineCW1.ClearData();
            LineCW2.ClearData();
            CWPointObject point = (sender as StackPanel).Tag as CWPointObject;
            for (int i = 0; i < point.CW1Freqs.Count; i++)
            {
                LineCW1.AddData(point.CW1Freqs[i], point.CW1Values[i]);
            }
            for (int i = 0; i < point.CW2Freqs.Count; i++)
            {
                LineCW2.AddData(point.CW2Freqs[i], point.CW2Values[i]);
            }
            UpdateCWPlot();
        }

        public void ClearData()
        {
            LineBp.ClearData();
            LineBv.ClearData();
            LineB.ClearData();
            CWPoints.Clear();
            LineCW1.ClearData();
            LineCW2.ClearData();
            DataViewer.Children.Clear();
            Chart.RefreshPlotWithAutoScale();
            CWChart.RefreshPlotWithAutoScale();
        }

        public void AddData(CWPointObject point)
        {
            CWPoints.Add(point);
            DataViewer.Children.Add(CreateInformationBar(point));
            LineBp.AddData(point.MoverLoc, point.Bp);
            LineBv.AddData(point.MoverLoc, point.Bv);
            LineB.AddData(point.MoverLoc, point.B);
            Chart.RefreshPlotWithAutoScale();
        }

        public void AddDatas(List<CWPointObject> points)
        {
            foreach (var item in points)
            {
                CWPoints.Add(item);
                DataViewer.Children.Add(CreateInformationBar(item));
                LineBp.AddData(item.MoverLoc, item.Bp);
                LineBv.AddData(item.MoverLoc, item.Bv);
                LineB.AddData(item.MoverLoc, item.B);
            }
            Chart.RefreshPlotWithAutoScale();
        }

        public void AddPoint(CWPointObject obj)
        {
            DataViewer.Children.Add(CreateInformationBar(obj));
            LineBp.AddData(obj.MoverLoc, obj.Bp);
            LineBv.AddData(obj.MoverLoc, obj.Bv);
            LineB.AddData(obj.MoverLoc, obj.B);
            Chart.RefreshPlotWithAutoScale();
        }

        private void ChangeLineCheck(object sender, RoutedEventArgs e)
        {
            if (Chart == null) return;
            RefreshCheck();
        }

        private void RefreshCheck()
        {
            Chart.DataList.Clear();
            if (BpCheck.IsChecked == true)
            {
                Chart.DataList.Add(LineBp);
            }
            if (BvCheck.IsChecked == true)
            {
                Chart.DataList.Add(LineBv);
            }
            if (BCheck.IsChecked == true)
            {
                Chart.DataList.Add(LineB);
            }
        }

        private void SnapPlot(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(Chart));
            TimeWindow window = new TimeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        private void SnapPlotCW(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(CWChart));
            TimeWindow window = new TimeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        private void ChangeCWPlot(object sender, RoutedEventArgs e)
        {
            UpdateCWPlot();
        }

        private void UpdateCWPlot()
        {
            if (CWChart == null) return;
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

        private void FitWithCurve(object sender, RoutedEventArgs e)
        {
            if (IsAngleWindow)
            {
            }
            else
            {

            }
        }

        /// <summary>
        /// 将绝对值数据转换成三角函数
        /// </summary>
        public List<double> ConvertAbsDataToSin(List<double> y)
        {
            List<double> ny = new List<double>();
            if (y.Count < 3)
            {
                return y;
            }

            List<int> revs = new List<int>();

            for (int i = 1; i < y.Count - 1; i++)
            {
                //找拐点
                if (y[i] <= y[i + 1] && y[i] <= y[i - 1])
                {
                    revs.Add(i);
                }
            }

            revs.Sort();

            for (int i = 0; i < y.Count; i++)
            {
                int sgn = 1;
                foreach (var item in revs)
                {
                    if (i > item) sgn *= -1;
                }
                ny.Add(sgn * y[i]);
            }

            return ny;
        }

    }
}
