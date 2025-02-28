using CodeHelper;
using Controls.Charts;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.扫描基方法;
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

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM实验
{
    /// <summary>
    /// ScanRangeSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScanRangeSelectWindow : Window
    {
        ODMRExpObject Exp = null;
        bool IsD1 = false;
        bool IsD2 = false;
        public ScanRangeSelectWindow(ODMRExpObject exp)
        {
            Exp = exp;
            InitializeComponent();
            TitleWindow.Content = "扫描范围设置" + "(" + exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName + ")";
            Title = "扫描范围设置" + "(" + exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName + ")";
            WindowResizeHelper helper = new WindowResizeHelper();
            helper.RegisterWindow(this, MinimizeBtn, MaximizeBtn, null, 6, 30);
        }

        public void ShowD1(D1ScanRangeBase scanrange)
        {
            IsD1 = true;
            D1Panel.Visibility = Visibility.Visible;
            D2Panel.Visibility = Visibility.Collapsed;
            Show();
        }

        public void ShowD2(D2ScanRangeBase scanrange)
        {
            IsD2 = true;
            D1Panel.Visibility = Visibility.Collapsed;
            D2Panel.Visibility = Visibility.Visible;
            Show();
        }

        private void ChangePannel(object sender, RoutedEventArgs e)
        {
            D2Btn1.KeepPressed = false;
            D2Btn2.KeepPressed = false;
            D2Btn3.KeepPressed = false;
            D2Panel1.Visibility = Visibility.Collapsed;
            D2Panel2.Visibility = Visibility.Collapsed;
            D2Panel3.Visibility = Visibility.Collapsed;

            D1Btn1.KeepPressed = false;
            D1Btn2.KeepPressed = false;
            D1Panel1.Visibility = Visibility.Collapsed;
            D1Panel2.Visibility = Visibility.Collapsed;

            if (sender == D1Btn1)
            {
                D1Btn1.KeepPressed = true;
                D1Panel1.Visibility = Visibility.Visible;
            }
            if (sender == D1Btn2)
            {
                D1Btn2.KeepPressed = true;
                D1Panel2.Visibility = Visibility.Visible;
            }

            if (sender == D2Btn1)
            {
                D2Btn1.KeepPressed = true;
                D2Panel1.Visibility = Visibility.Visible;
            }
            if (sender == D2Btn2)
            {
                D2Btn2.KeepPressed = true;
                D2Panel2.Visibility = Visibility.Visible;
            }
            if (sender == D2Btn3)
            {
                D2Btn3.KeepPressed = true;
                D2Panel3.Visibility = Visibility.Visible;
            }
        }

        private void Apply(object sender, RoutedEventArgs e)
        {

        }

        #region 二维特殊形状扫描
        private void AddPoint(object sender, RoutedEventArgs e)
        {
            Point p = new Point(double.NaN, double.NaN);
            CounterPoints.AddItem(p, p.X, p.Y);
            CounterPointAssemble.Add(p);
        }

        private void CounterPoints_ItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            CounterPointAssemble.RemoveAt(arg2);
            UpdateCounterPoints();
            UpdatePointCanvas();
        }

        private void UpdateCounterPoints()
        {
            CounterPoints.ClearItems();
            foreach (var item in CounterPointAssemble)
            {
                CounterPoints.AddItem(item, item.X, item.Y);
            }
        }

        List<Point> CounterPointAssemble = new List<Point>();

        private void UpdatePointCanvas()
        {
            PlotChart.DataList.Clear();
            PlotChart.RefreshPlotWithAutoScale();
            if (CounterPointAssemble.Where(x => !double.IsNaN(x.X) && !double.IsNaN(x.Y)).Count() < 3) return;
            NumricDataSeries dd = new NumricDataSeries("");
            dd.LineThickness = 3;
            dd.MarkerSize = 0;
            dd.LineColor = Colors.Orange;
            for (int i = 0; i < CounterPointAssemble.Count; i++)
            {
                if (double.IsNaN(CounterPointAssemble[i].X) || double.IsNaN(CounterPointAssemble[i].Y))
                    continue;
                dd.X.Add(CounterPointAssemble[i].X);
                dd.Y.Add(CounterPointAssemble[i].Y);
            }
            dd.X.Add(CounterPointAssemble[0].X);
            dd.Y.Add(CounterPointAssemble[0].Y);
            PlotChart.DataList.Add(dd);
            PlotChart.RefreshPlotWithAutoScale();
        }

        private void CounterPoints_ItemValueChanged(int arg1, int arg2, object arg3)
        {
            try
            {
                if (arg2 == 0)
                {
                    CounterPointAssemble[arg1] = new Point(double.Parse(arg3 as string), CounterPointAssemble[arg1].Y);
                }
                if (arg2 == 1)
                {
                    CounterPointAssemble[arg1] = new Point(CounterPointAssemble[arg1].X, double.Parse(arg3 as string));
                }
                UpdatePointCanvas();
            }
            catch (Exception)
            {

            }
        }

        private void ResizePlot(object sender, RoutedEventArgs e)
        {
            PlotChart.RefreshPlotWithAutoScale();
        }
        #endregion

        private void Close(object sender, RoutedEventArgs e)
        {
            Exp.RangeWindow = null;
            Close();
        }
    }
}
