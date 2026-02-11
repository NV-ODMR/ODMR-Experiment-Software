using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using OpenCvSharp;
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
using Point = System.Windows.Point;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM实验
{
    /// <summary>
    /// ScanRangeSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScanRangeSelectWindow : System.Windows.Window
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
            helper.RegisterCloseWindow(this, MinimizeBtn, MaximizeBtn, CloseBtn, 6, 30);
            helper.BeforeClose += BeforeClose;
        }

        public void ShowD1(D1PointsScanRangeBase scanrange)
        {
            IsD1 = true;
            D1Panel.Visibility = Visibility.Visible;
            D2Panel.Visibility = Visibility.Collapsed;
            //设置内容
            SetD1Panel(scanrange);
            Show();
        }

        public void ShowD2(D2ScanRangeBase scanrange)
        {
            IsD2 = true;
            D1Panel.Visibility = Visibility.Collapsed;
            D2Panel.Visibility = Visibility.Visible;

            //设置内容
            SetD2Panel(scanrange);
            Show();
        }

        private void SetD1Panel(D1PointsScanRangeBase scanRangeBase)
        {
            if (scanRangeBase is D1PointsLinearScanRange)
            {
                ChangePannel(D1Btn1, new RoutedEventArgs());
                D1LineStartPointX.Text = scanRangeBase.StartPoint.X.ToString();
                D1LineStartPointY.Text = scanRangeBase.StartPoint.Y.ToString();
                D1LineEndPointX.Text = scanRangeBase.EndPoint.X.ToString();
                D1LineEndPointY.Text = scanRangeBase.EndPoint.Y.ToString();
                D1LineScanPoints.Text = scanRangeBase.Counts.ToString();
                D1LineScanReverse.IsSelected = scanRangeBase.Reverse;
                return;
            }
            if (scanRangeBase is D1PointsListScanRange)
            {
                ChangePannel(D1Btn2, new RoutedEventArgs());
                var points = (scanRangeBase as D1PointsListScanRange).ScanPoints;
                D1ScanPointAssemble = points.Select(x => new Point(x.X, x.Y)).ToList();
                UpdateScanPointsList();
                return;
            }
        }

        private void SetD2Panel(D2ScanRangeBase scanRangeBase)
        {
            if (scanRangeBase == null)
            {
                D2Panel1.Visibility = Visibility.Visible;
                return;
            }
            if (scanRangeBase is D2LinearScanRangeBase)
            {
                ChangePannel(D2Btn1, new RoutedEventArgs());
                D2RectScanXCount.Text = scanRangeBase.XCount.ToString();
                D2RectScanYCount.Text = scanRangeBase.YCount.ToString();
                D2RectScanXLo.Text = scanRangeBase.XLo.ToString();
                D2RectScanXHi.Text = scanRangeBase.XHi.ToString();
                D2RectScanYLo.Text = scanRangeBase.YLo.ToString();
                D2RectScanYHi.Text = scanRangeBase.YHi.ToString();
                D2RectScanXReverse.IsSelected = scanRangeBase.ReverseX;
                D2RectScanYReverse.IsSelected = scanRangeBase.ReverseY;
                D2RectScanXFast.IsSelected = scanRangeBase.IsXFastAxis;
                if (scanRangeBase is D2LinearScanRange)
                {
                    D2RectScanDirectionReverse.IsSelected = false;
                }
                if (scanRangeBase is D2LinearReverseScanRange)
                {
                    D2RectScanDirectionReverse.IsSelected = true;
                }
                return;
            }
            if (scanRangeBase is D2ShapeScanRangeBase)
            {
                ChangePannel(D2Btn2, new RoutedEventArgs());
                D2ShapeScanXCounts.Text = scanRangeBase.XCount.ToString();
                D2ShapeScanYCounts.Text = scanRangeBase.YCount.ToString();
                D2ShapeScanXReverse.IsSelected = scanRangeBase.ReverseX;
                D2ShapeScanYreverse.IsSelected = scanRangeBase.ReverseY;
                D2ShapeScanXFast.IsSelected = scanRangeBase.IsXFastAxis;
                var points = (scanRangeBase as D2ShapeScanRangeBase).Poly.Coordinates;
                CounterPointAssemble = points.Select(x => new Point(x.X, x.Y)).ToList();
                if (CounterPointAssemble.Count != 0)
                    CounterPointAssemble.Remove(CounterPointAssemble.Last());
                UpdateCounterPoints();
                UpdatePointCanvas();
                if (scanRangeBase is D2ShapeScanRangeBase)
                {
                    D2ShapeScanDirectionReverse.IsSelected = false;
                }
                if (scanRangeBase is D2ShapeReverseScanRange)
                {
                    D2ShapeScanDirectionReverse.IsSelected = true;
                }
                return;
            }
        }



        private void ChangePannel(object sender, RoutedEventArgs e)
        {
            D2Btn1.KeepPressed = false;
            D2Btn2.KeepPressed = false;
            D2Panel1.Visibility = Visibility.Collapsed;
            D2Panel2.Visibility = Visibility.Collapsed;

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
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            try
            {
                if (D2Panel.Visibility == Visibility.Visible)
                {
                    D2ScanRangeBase resrange = null;
                    #region 二维矩形扫描
                    if (D2Panel1.Visibility == Visibility.Visible)
                    {
                        if (D2RectScanDirectionReverse.IsSelected)
                        {
                            resrange = new D2LinearReverseScanRange(double.Parse(D2RectScanXLo.Text), double.Parse(D2RectScanXHi.Text), int.Parse(D2RectScanXCount.Text),
                                double.Parse(D2RectScanYLo.Text), double.Parse(D2RectScanYHi.Text), int.Parse(D2RectScanYCount.Text), D2RectScanXReverse.IsSelected, D2RectScanYReverse.IsSelected,
                                D2RectScanXFast.IsSelected);
                        }
                        else
                        {
                            resrange = new D2LinearScanRange(double.Parse(D2RectScanXLo.Text), double.Parse(D2RectScanXHi.Text), int.Parse(D2RectScanXCount.Text),
                            double.Parse(D2RectScanYLo.Text), double.Parse(D2RectScanYHi.Text), int.Parse(D2RectScanYCount.Text), D2RectScanXReverse.IsSelected, D2RectScanYReverse.IsSelected,
                            D2RectScanXFast.IsSelected);
                        }
                    }
                    #endregion
                    #region 二维任意形状扫描
                    if (D2Panel2.Visibility == Visibility.Visible)
                    {
                        if (CounterPointAssemble.Count < 3) throw new Exception("形状轮廓必须包含至少三个点");
                        List<Point> ps = CounterPointAssemble.ToArray().ToList();
                        ps.Add(ps.First());
                        if (D2ShapeScanDirectionReverse.IsSelected)
                        {
                            resrange = new D2ShapeReverseScanRange(int.Parse(D2ShapeScanXCounts.Text), int.Parse(D2ShapeScanYCounts.Text),
                                D2ShapeScanXReverse.IsSelected, D2ShapeScanYreverse.IsSelected, D2ShapeScanXFast.IsSelected, ps);
                        }
                        else
                        {
                            resrange = new D2ShapeScanRange(int.Parse(D2ShapeScanXCounts.Text), int.Parse(D2ShapeScanYCounts.Text),
                                D2ShapeScanXReverse.IsSelected, D2ShapeScanYreverse.IsSelected, D2ShapeScanXFast.IsSelected, ps);
                        }
                    }
                    #endregion
                    Exp.D2ScanRange = resrange;
                    if (Exp.ParentPage != null)
                        Exp.ParentPage.ScanRangeName.Text = resrange.ScanName;
                    Close();
                    Exp.RangeWindow = null;
                    return;
                }
                if (D1Panel.Visibility == Visibility.Visible)
                {
                    D1PointsScanRangeBase resrange = null;
                    #region 直线扫描
                    if (D1Panel1.Visibility == Visibility.Visible)
                    {
                        resrange = new D1PointsLinearScanRange(new Point(double.Parse(D1LineStartPointX.Text), double.Parse(D1LineStartPointY.Text)),
                            new Point(double.Parse(D1LineEndPointX.Text), double.Parse(D1LineEndPointY.Text)), int.Parse(D1LineScanPoints.Text),
                            D1LineScanReverse.IsSelected);
                    }
                    #endregion
                    #region 点列表扫描
                    if (D1Panel2.Visibility == Visibility.Visible)
                    {
                        if (D1ScanPointAssemble.Count == 0)
                            throw new Exception("扫描点不能为空");
                        resrange = new D1PointsListScanRange(D1ScanPointAssemble);
                    }
                    #endregion
                    Exp.D1ScanRange = resrange;
                    if (Exp.ParentPage != null)
                        Exp.ParentPage.ScanRangeName.Text = resrange.ScanName;
                    Close();
                    Exp.RangeWindow = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("参数格式错误:" + ex.Message, this);
            }
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

        private void BeforeClose(object sender, RoutedEventArgs e)
        {
            Exp.RangeWindow = null;
        }

        #region 一维点列表扫描
        List<Point> D1ScanPointAssemble = new List<Point>();
        private void AddScanPoint(object sender, RoutedEventArgs e)
        {
            Point p = new Point(double.NaN, double.NaN);
            D1ScanPointAssemble.Add(p);

        }
        private void UpdateScanPointsList()
        {
            D1PointList.ClearItems();
            foreach (var item in D1ScanPointAssemble)
            {
                D1PointList.AddItem(item, Math.Round(item.X, 5).ToString(), Math.Round(item.Y, 5).ToString());
            }
        }

        private void D1ScanPoints_ItemValueChanged(int arg1, int arg2, object arg3)
        {
            try
            {
                if (arg2 == 0)
                {
                    D1ScanPointAssemble[arg1] = new Point(double.Parse(arg3 as string), D1ScanPointAssemble[arg1].Y);
                }
                if (arg2 == 1)
                {
                    D1ScanPointAssemble[arg1] = new Point(D1ScanPointAssemble[arg1].X, double.Parse(arg3 as string));
                }
                UpdateScanPointsList();
            }
            catch (Exception)
            {

            }
        }

        private void D1ScanPoints_ItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            D1ScanPointAssemble.RemoveAt(arg2);
            UpdateScanPointsList();
        }
        #endregion
    }
}
