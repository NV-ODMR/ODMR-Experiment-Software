using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using System.Windows.Forms;

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// FitCurveParamSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class D1AnalyseWindow : Window
    {
        NumricDataSeries PlotXData = new NumricDataSeries("") { LineColor = Colors.GreenYellow };
        NumricDataSeries PlotYData = new NumricDataSeries("") { LineColor = Colors.SkyBlue };

        ChartViewer2D ChartParent = null;

        public D1AnalyseWindow(ChartViewer2D parent)
        {
            InitializeComponent();
            ChartParent = parent;

            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterHideWindow(this, MinBtn, null, CloseBtn, PinBtn, 4, 40);

            chartX.DataList.Add(PlotXData);
            chartY.DataList.Add(PlotYData);

            ApplyChartStyle(StyleParam);
        }

        /// <summary>
        /// 返回和输入值最接近的扫描列表的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int GetNearestIndex(List<double> list, double value)
        {
            try
            {
                var l = list.Select(x => Math.Abs(x - value)).ToList();
                return l.IndexOf(l.Min());
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private bool IsInRange(double lo, double hi, double value)
        {
            if (value < Math.Min(lo, hi) || value > Math.Max(lo, hi)) return false;

            return true;
        }

        private void UpdatePlot(object sender, RoutedEventArgs e)
        {
            Point p = ChartParent.ChartObject.GetSelectedCursorPoint();
            if (double.IsNaN(p.X) || double.IsNaN(p.Y))
            {
                return;
            }
            if (ChartParent.GetSelectedData() == null) return;
            var data = ChartParent.GetSelectedData().Data;
            if (!IsInRange(data.XLo, data.XHi, p.X) || !IsInRange(data.YLo, data.YHi, p.Y))
            {
                return;
            }
            //获取数据
            var xdata = data.GetFormattedXData();
            var ydata = data.GetFormattedYData();

            int xind = GetNearestIndex(xdata, p.X);
            int yind = GetNearestIndex(ydata, p.Y);

            var ypoints = ydata.Select(x => new Point(x, data.GetValue(xind, GetNearestIndex(ydata, x))));
            var xpoints = xdata.Select(x => new Point(x, data.GetValue(GetNearestIndex(xdata, x), yind)));

            PlotYData.X = ypoints.Select(x => x.X).ToList();
            PlotYData.Y = ypoints.Select(x => x.Y).ToList();

            PlotXData.X = xpoints.Select(x => x.X).ToList();
            PlotXData.Y = xpoints.Select(x => x.Y).ToList();

            PlotXData.Name = ChartParent.GetSelectedData().Data.ZName + "Y=" + Math.Round(p.Y, 4).ToString();
            PlotYData.Name = ChartParent.GetSelectedData().Data.ZName + "X=" + Math.Round(p.X, 4).ToString();

            chartX.RefreshPlotWithAutoScale();
            chartY.RefreshPlotWithAutoScale();
        }

        #region 图表样式设置
        Chart1DStyleParams StyleParam = (Chart1DStyleParams)Chart1DStyleParams.GlobalChartPlotParam.Copy();

        private void ApplyLineStyle(object sender, RoutedEventArgs e)
        {
            StyleParam.ReadFromPage(new FrameworkElement[] { this });
            ApplyChartStyle(StyleParam);
        }


        private void ApplyLineStyleKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StyleParam.ReadFromPage(new FrameworkElement[] { this });
                ApplyChartStyle(StyleParam);
            }
        }

        private void ApplyChartStyle(Chart1DStyleParams para)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var item in chartX.DataList)
                {
                    item.Smooth = para.IsSmooth.Value;
                    try
                    {
                        item.LineThickness = para.LineWidth.Value;
                    }
                    catch (Exception) { }

                    string style = para.PlotStyle.Value;
                    if (style == "点状图")
                    {
                        item.MarkerSize = para.PointSize.Value;
                        item.LineThickness = 0;
                    }
                    if (style == "线状图")
                    {
                        item.LineThickness = para.LineWidth.Value;
                        item.MarkerSize = 0;
                    }
                    if (style == "点线图")
                    {
                        item.LineThickness = para.LineWidth.Value;
                        item.MarkerSize = para.PointSize.Value;
                    }
                    chartX.RefreshPlotWithAutoScaleY();
                }
                foreach (var item in chartY.DataList)
                {
                    item.Smooth = para.IsSmooth.Value;
                    try
                    {
                        item.LineThickness = para.LineWidth.Value;
                    }
                    catch (Exception) { }

                    string style = para.PlotStyle.Value;
                    if (style == "点状图")
                    {
                        item.MarkerSize = para.PointSize.Value;
                        item.LineThickness = 0;
                    }
                    if (style == "线状图")
                    {
                        item.LineThickness = para.LineWidth.Value;
                        item.MarkerSize = 0;
                    }
                    if (style == "点线图")
                    {
                        item.LineThickness = para.LineWidth.Value;
                        item.MarkerSize = para.PointSize.Value;
                    }
                    chartY.RefreshPlotWithAutoScaleY();
                }
            });
        }
        #endregion

        #region 光标
        private void AddCursor(object sender, RoutedEventArgs e)
        {
            chartX.AddCursorCenter();
            chartY.AddCursorCenter();
            CursorCount.Content = chartX.GetCursorCount();
        }

        private void BringCursorToCenter(object sender, RoutedEventArgs e)
        {
            chartX.BringAllCursorToCenter();
            chartY.BringAllCursorToCenter();
            CursorCount.Content = chartX.GetCursorCount();
        }
        #endregion

        private void SnapY(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(chartY));
                TimeWindow window = new TimeWindow();
                window.Owner = Window.GetWindow(this);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowWindow("截图已复制到剪切板");
            }
            catch (Exception ex) { }
        }

        private void SnapX(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(chartX));
                TimeWindow window = new TimeWindow();
                window.Owner = Window.GetWindow(this);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowWindow("截图已复制到剪切板");
            }
            catch (Exception ex) { }
        }

        /// <summary>
        /// 复制X数据到剪切板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveX(object sender, RoutedEventArgs e)
        {
            try
            {
                string save = "";
                int maxcount = PlotXData.GetXCount();

                string namestr = "";
                namestr = PlotXData.Name + " X坐标" + "\t" + PlotXData.Name + " Y坐标";

                namestr += "\n";

                save += namestr;

                string datastr = "";
                for (int i = 0; i < maxcount; i++)
                {
                    datastr += PlotXData.X[i].ToString() + "\t" + PlotXData.Y[i].ToString() + "\n";
                }

                if (datastr != "")
                {
                    datastr = datastr.Remove(datastr.Length - 1, 1);
                }
                save += datastr;
                //复制到剪切板
                System.Windows.Forms.Clipboard.SetText(save);
                TimeWindow win = new TimeWindow();
                win.Owner = this;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.ShowWindow("复制成功");
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 复制Y数据到剪切板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveY(object sender, RoutedEventArgs e)
        {
            try
            {
                string save = "";
                int maxcount = PlotYData.GetXCount();

                string namestr = "";
                namestr = PlotYData.Name + " X坐标" + "\t" + PlotYData.Name + " Y坐标";

                namestr += "\n";

                save += namestr;

                string datastr = "";
                for (int i = 0; i < maxcount; i++)
                {
                    datastr += PlotYData.X[i].ToString() + "\t" + PlotYData.Y[i].ToString() + "\n";
                }

                if (datastr != "")
                {
                    datastr = datastr.Remove(datastr.Length - 1, 1);
                }
                save += datastr;
                //复制到剪切板
                System.Windows.Forms.Clipboard.Clear();
                System.Windows.Forms.Clipboard.SetText(save);
                TimeWindow win = new TimeWindow();
                win.Owner = this;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.ShowWindow("复制成功");
            }
            catch (Exception)
            {
            }
        }
    }
}
