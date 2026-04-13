using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口.数据拟合;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.设备部分.温控;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;
using ODMR_Lab.实验部分.设备参数监控;
using Controls.Charts;
using HardWares.Windows;
using ODMR_Lab.实验部分.设备参数监测;
using HardWares.端口基类部分;

namespace ODMR_Lab.设备参数监测
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public override string PageName { get; set; } = "设备参数监测";

        /// <summary>
        /// 线程锁
        /// </summary>
        private static readonly object lockobj = new object();


        public DeviceListenerDispatcher ListenDispatcher = null;

        public static MainWindow WindowHandle = null;

        public DisplayPage()
        {
            InitializeComponent();
        }

        public override void InnerInit()
        {
        }

        List<ChannelInfo> SelectedInfos = new List<ChannelInfo>();

        public override void CloseBehaviour()
        {
            ListenDispatcher?.StopListen();
        }



        public override void UpdateParam()
        {
        }

        #region 图表操作
        private void ResizePlot(object sender, RoutedEventArgs e)
        {
            if (!IsFixedTime.IsSelected)
                Chart.RefreshPlotWithAutoScale();
            else
            {
                UpdatePlotWithFixedTimeLength();
            }
        }

        public void UpdatePlotWithFixedTimeLength()
        {
            try
            {
                double timeend = (DateTime.Now).ToOADate();
                double timestart = (DateTime.Now - FixedTimeLength).ToOADate();
                //获取数据中的时间最大值
                var dats = Chart.DataList.Where(x => x.IsVisible);
                if (dats.Count() == 0) return;
                var maxs = dats.Select(x => (x as TimeDataSeries).X.Last());
                var mins = dats.Select(x => (x as TimeDataSeries).X.First());
                if (maxs.Count() != 0 && mins.Count() != 0)
                {
                    TimeSpan det = maxs.Max() - mins.Min();
                    timeend = (maxs.Max()).ToOADate();
                    timestart = (maxs.Max() - FixedTimeLength).ToOADate();
                    if (det > FixedTimeLength) Chart.RefreshPlotWithCustomScale(timestart, timeend);
                    else
                        Chart.RefreshPlotWithAutoScale();
                    return;
                }
                Chart.RefreshPlotWithCustomScale(timestart, timeend);
            }
            catch (Exception)
            {
            }
        }

        private void Snap(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(Chart));
            TimeWindow window = new TimeWindow();
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        /// <summary>
        /// 保存为文本文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsExternal(object sender, RoutedEventArgs e)
        {
            string name = (sender as DecoratedButton).Name;
            string save = "";
            int maxcount = -1;

            string namestr = "";
            foreach (var item in ListenDispatcher.DeviceParameters)
            {
                int count = item.DeviceDisplayData.GetXCount();
                if (maxcount < count) maxcount = count;

                namestr += item.DeviceDisplayData.Name + " time" + "\t" + item.DeviceDisplayData.Name + "\t";
            }

            if (namestr != "")
            {
                namestr = namestr.Remove(namestr.Length - 1, 1);
            }
            namestr += "\n";
            save += namestr;

            string datastr = "";
            for (int i = 0; i < maxcount; i++)
            {
                string temp = "";
                foreach (var item in ListenDispatcher.DeviceParameters)
                {
                    item.GetData(out List<DateTime> data, out List<double> ddata);
                    if (i > data.Count - 1)
                    {
                        temp += "" + "\t";
                    }
                    else
                    {
                        temp += data[i].ToString("yyyy/MM/dd HH:mm:ss:fff") + "\t";
                    }
                    if (i > ddata.Count - 1)
                    {
                        temp += "" + "\t";
                    }
                    else
                    {
                        temp += ddata[i].ToString() + "\t";
                    }
                }
                if (temp != "")
                {
                    temp = temp.Remove(temp.Length - 1, 1);
                }
                datastr += temp + "\n";
            }

            if (datastr != null)
            {
                datastr = datastr.Remove(datastr.Length - 1, 1);
            }

            save += datastr;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "文本文件 (*.txt)|*.txt";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter wr = new StreamWriter(new FileStream(dlg.FileName, FileMode.Create)))
                    {
                        wr.Write(save);
                    }
                    TimeWindow win = new TimeWindow();
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("文件未成功保存，原因：" + ex.Message, Window.GetWindow(this));
                }
            }

        }
        #endregion


        /// <summary>
        /// 显示点数
        /// </summary>
        public uint DisplayPointCount = 1000;

        /// <summary>
        /// 显示点数
        /// </summary>
        public long StoredPointCount = 1000;

        /// <summary>
        /// 显示点数
        /// </summary>
        public long SampleGap = 1000;

        /// <summary>
        /// 固定显示时间长度
        /// </summary>
        public TimeSpan FixedTimeLength = TimeSpan.FromMinutes(2);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetApplied(object sender, RoutedEventArgs e)
        {
            SetSampleConfigs();
            TimeWindow win = new TimeWindow();
            win.ShowWindow("设置成功");
        }

        public void SetSampleConfigs()
        {
            try
            {
                DisplayPointCount = uint.Parse(DisplayedPoints.Text);
                Chart.SampleThreshold = DisplayPointCount;
                StoredPointCount = long.Parse(StoredPoints.Text);
                SampleGap = (int)(double.Parse(SampleTimeText.Text) * 1000);
                ListenDispatcher.ValidateGap = (int)SampleGap;
                if (TimeUnit.Text == "s")
                    FixedTimeLength = TimeSpan.FromSeconds(double.Parse(FixedDisplayMode.Text));
                if (TimeUnit.Text == "min")
                    FixedTimeLength = TimeSpan.FromMinutes(double.Parse(FixedDisplayMode.Text));
                if (TimeUnit.Text == "h")
                    FixedTimeLength = TimeSpan.FromHours(double.Parse(FixedDisplayMode.Text));
                if (TimeUnit.Text == "d")
                    FixedTimeLength = TimeSpan.FromDays(double.Parse(FixedDisplayMode.Text));
            }
            catch (Exception)
            {
            }

            DisplayedPoints.Text = DisplayPointCount.ToString();
            StoredPoints.Text = StoredPointCount.ToString();
            SampleTimeText.Text = (SampleGap / 1000.0).ToString();
        }

        private void AddParam(object sender, RoutedEventArgs e)
        {
            //选择设备
            DeviceFindWindow w = new DeviceFindWindow();
            w.Owner = Window.GetWindow(this);
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var devresult = w.ShowDialog();
            if (devresult.Key == null && devresult.Value == null) return;
            ParamSelectWindow win = new ParamSelectWindow();
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Parameter target = null;
            if (devresult.Value == null)
            {
                target = win.ShowDialog(devresult.Key);
            }
            else
            {
                target = win.ShowDialog(devresult.Value);
            }
            if (target == null) return;
            //新建参数
            DeviceListenInfo info = new DeviceListenInfo(this, devresult.Key, devresult.Value, target);
            info.ValidateDev(devresult.Key, devresult.Value, target);
            AppendInfoCommands(info);
            ListenDispatcher.AddListenInfo(info);
            ParamPanel.Children.Add(info.DeviceParamBar);
            NumricPanel.Children.Add(info.DeviceNumberBar);
            Chart.DataList.Add(info.DeviceDisplayData);
            Chart.RefreshPlotWithAutoScaleY();
        }

        public void AppendInfoCommands(DeviceListenInfo info)
        {
            info.DeviceParamBar.ParamDeleteEvent += new Action<ParamBar>((ele) =>
            {
                if (MessageWindow.ShowMessageBox("删除", "确定要取消对此参数的监控吗?", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    //删除
                    ListenDispatcher.RemoveListenInfo(info);
                    ParamPanel.Children.Remove(info.DeviceParamBar);
                    NumricPanel.Children.Remove(info.DeviceNumberBar);
                    Chart.DataList.Remove(info.DeviceDisplayData);
                    Chart.RefreshPlotWithAutoScaleY();
                }
            });
            info.DeviceParamBar.ColorSelectEvent += new Action<ParamBar>((ele) =>
            {
                ColorSelectPop.PlacementTarget = ele; ColorSelectPop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                ColorSelectPop.IsOpen = true; ColorSelectPop.Tag = info; ColorSelect.CurrentColor = info.DisplayColor;
            });
            info.DeviceParamBar.SelectedEvent += new Action<ParamBar>((p) =>
            {
                foreach (var item in ParamPanel.Children)
                {
                    (item as ParamBar).Background = new SolidColorBrush(Colors.Transparent);
                }
                p.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F85CD"));
            });
        }

        public void ValidateParamBars()
        {
            ParamPanel.Children.Clear();
            NumricPanel.Children.Clear();
            Chart.DataList.Clear();
            foreach (var item in ListenDispatcher.DeviceParameters)
            {
                ParamPanel.Children.Add(item.DeviceParamBar);
                NumricPanel.Children.Add(item.DeviceNumberBar);
                Chart.DataList.Add(item.DeviceDisplayData);
            }
        }

        public void ValidatePanelBar()
        {
            NumricPanel.Children.Clear();
            foreach (var item in ListenDispatcher.DeviceParameters)
            {
                NumricPanel.Children.Add(item.DeviceNumberBar);
            }
        }

        private void ColorSelectPop_Closed(object sender, EventArgs e)
        {
            (ColorSelectPop.Tag as DeviceListenInfo).DisplayColor = ColorSelect.CurrentColor;
        }

        private void ShowNumric(object sender, RoutedEventArgs e)
        {
            NumBtn.KeepPressed = true;
            PlotBtn.KeepPressed = false;
            NumricView.Visibility = Visibility.Visible;
            PlotView.Visibility = Visibility.Hidden;
        }

        private void ShowPlot(object sender, RoutedEventArgs e)
        {
            NumBtn.KeepPressed = false;
            PlotBtn.KeepPressed = true;
            NumricView.Visibility = Visibility.Hidden;
            PlotView.Visibility = Visibility.Visible;
        }

        #region 图表样式设置
        public Chart1DStyleParams StyleParam = (Chart1DStyleParams)Chart1DStyleParams.GlobalChartPlotParam.Copy();

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
                Chart1DStyleParams.GlobalChartPlotParam = (Chart1DStyleParams)StyleParam.Copy();
                ApplyChartStyle(StyleParam);
            }
        }

        private void ApplyChartStyle(Chart1DStyleParams para)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var item in Chart.DataList)
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
                    Chart.RefreshPlotWithAutoScaleY();
                }
            });
        }
        #endregion

        #region 光标
        private void AddCursor(object sender, RoutedEventArgs e)
        {
            Chart.AddCursorCenter();
            CursorCount.Content = Chart.GetCursorCount();
        }

        private void BringCursorToCenter(object sender, RoutedEventArgs e)
        {
            Chart.BringAllCursorToCenter();
            CursorCount.Content = Chart.GetCursorCount();
        }
        #endregion
    }
}
