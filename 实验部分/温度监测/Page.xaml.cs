using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.磁场调节;
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

namespace ODMR_Lab.温度监测部分
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class TemperaturePage : PageBase
    {
        /// <summary>
        /// 线程锁
        /// </summary>
        private static readonly object lockobj = new object();


        public static MainWindow WindowHandle = null;

        public TemperaturePage()
        {
            InitializeComponent();
            SetWindow = new SaveWindow(this);
        }

        public override void Init()
        {
            MainWindow.Dev_TemPeraPage.DataChangedEvent += RefreshChooserLegends;

            MainWindow.Dev_TemPeraPage.ChartUpdateEvent += RefreshChart;

            //创建自动保存线程
            CreateAutoSaveThread();
        }

        public override void CloseBehaviour()
        {
            AutoSaveThread?.Abort();
        }

        private void RefreshChart()
        {
            //刷新界面UI
            Dispatcher.Invoke(() =>
            {
                foreach (TemperaturePannel item in NumricPanel.Children)
                {
                    (item.Tag as ChannelInfo).PlotLine.GetData(out IEnumerable<DateTime> dx, out IEnumerable<double> dy);
                    if (dx.Count() == 0) continue;
                    if (item.Tag is SensorChannelInfo)
                    {
                        item.Value.Text = Math.Round(dy.Last(), 7).ToString() + (item.Tag as SensorChannelInfo).Channel.Unit;
                    }
                    if (item.Tag is OutputChannelInfo)
                    {
                        item.Value.Text = Math.Round(dy.Last(), 7).ToString() + (item.Tag as OutputChannelInfo).Channel.Unit;
                    }
                }
            });
            PlotChart.RefreshPlotWithAutoScaleY();
        }

        private void PlotChart_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            PlotChart.RefreshPlotWithAutoScaleY();
        }

        private void PlotChart_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                PlotChart.RefreshPlotWithAutoScaleY();
            }
        }


        #region 按键功能
        /// <summary>
        /// 显示数值图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowNumric(object sender, RoutedEventArgs e)
        {
            NumricView.Visibility = Visibility.Visible;
            PlotView.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 显示图表视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPlot(object sender, RoutedEventArgs e)
        {
            NumricView.Visibility = Visibility.Hidden;
            PlotView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save(object sender, RoutedEventArgs e)
        {
            SaveWindow window = new SaveWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        /// <summary>
        /// 重新设置图表范围
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResizePlot(object sender, RoutedEventArgs e)
        {
            PlotChart.RefreshPlotWithAutoScale();
        }

        /// <summary>
        /// 截图到剪切板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SnapPlot(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(PlotChart));
            TimeWindow window = new TimeWindow();
            window.ShowWindow("截图已复制到剪切板");
        }

        #endregion

        #region 数据采集
        /// <summary>
        /// 采样时间间隔
        /// </summary>
        public double SampleTime { get; set; } = 1;


        /// <summary>
        /// 设置采样时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSampleTime(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SampleTimeText.Text, out double value))
            {
                SampleTime = value;
                TimeWindow window = new TimeWindow();
                window.Owner = null;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowWindow("设置成功");
            }
            else
            {
                MessageWindow.ShowTipWindow("采样时间间隔值必须是数字", MainWindow.Handle);
            }
        }

        #endregion

        #region 图表绘制
        /// <summary>
        /// 刷新图例列表
        /// </summary>
        private void RefreshChooserLegends(List<ChannelInfo> datas)
        {
            LegendContent.Children.Clear();
            NumricPanel.Children.Clear();
            foreach (var item in datas)
            {
                //添加数值显示面板
                TemperaturePannel pannel = null;
                if (item is OutputChannelInfo)
                    pannel = new TemperaturePannel(TemperaturePannel.PanelType.Output, item.Name) { Margin = new Thickness(10) };
                if (item is SensorChannelInfo)
                    pannel = new TemperaturePannel(TemperaturePannel.PanelType.Temperature, item.Name) { Margin = new Thickness(10) };
                pannel.Tag = item;
                NumricPanel.Children.Add(pannel);


                StackPanel panel = new StackPanel();
                panel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                panel.Margin = new Thickness(5);

                TextBlock block = new TextBlock();
                block.Text = item.Name;
                block.FontSize = 18;
                if (item is SensorChannelInfo)
                {
                    block.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4FC058"));
                }
                if (item is OutputChannelInfo)
                {
                    block.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC0A916"));
                }
                block.Width = 50;
                block.FontWeight = FontWeights.Bold;
                block.TextWrapping = TextWrapping.Wrap;
                block.VerticalAlignment = VerticalAlignment.Center;

                panel.Children.Add(block);

                Chooser choose = new Chooser();
                if (item is SensorChannelInfo)
                {
                    choose.ChooseBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4FC058"));
                    choose.UnChooseBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2E2E2E"));
                }
                if (item is OutputChannelInfo)
                {
                    choose.ChooseBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC0A916"));
                    choose.UnChooseBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2E2E2E"));
                }
                choose.Height = 30;
                choose.Width = 50;
                choose.Margin = new Thickness(5);
                choose.Tag = item;
                choose.IsSelected = PlotChart.DataList.Contains(item.PlotLine);
                choose.Selected += ChangeLine;
                choose.UnSelected += ChangeLine;

                panel.Children.Add(choose);

                LegendContent.Children.Add(panel);
                panel.Tag = item;
            }

            for (int i = 0; i < PlotChart.DataList.Count; i++)
            {
                bool isinData = false;
                foreach (var item in datas)
                {
                    if (PlotChart.DataList[i] == item.PlotLine)
                    {
                        isinData = true;
                    }
                }
                if (!isinData)
                {
                    PlotChart.DataList.RemoveAt(i);
                    --i;
                }
            }
        }

        /// <summary>
        /// 修改显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ChangeLine(object sender, RoutedEventArgs e)
        {
            Chooser chooser = sender as Chooser;
            if (chooser.IsSelected)
            {
                AddLine(chooser.Tag as ChannelInfo);
            }
            else
            {
                DeleteLine(chooser.Tag as ChannelInfo);
            }
        }

        /// <summary>
        /// 添加直线
        /// </summary>
        /// <param name="data"></param>
        public void AddLine(ChannelInfo data)
        {
            if (data is SensorChannelInfo)
            {
                if (PlotChart.DataList.Contains((data as SensorChannelInfo).PlotLine))
                    return;
                PlotChart.DataList.Add((data as SensorChannelInfo).PlotLine);
            }
            if (data is OutputChannelInfo)
            {
                if (PlotChart.DataList.Contains((data as OutputChannelInfo).PlotLine))
                    return;
                PlotChart.DataList.Add((data as OutputChannelInfo).PlotLine);
            }
            PlotChart.RefreshPlotWithAutoScale();
            return;
        }

        /// <summary>
        /// 删除直线
        /// </summary>
        /// <param name="data"></param>
        public void DeleteLine(ChannelInfo data)
        {
            if (data is SensorChannelInfo)
            {
                PlotChart.DataList.Remove((data as SensorChannelInfo).PlotLine);
            }
            if (data is OutputChannelInfo)
            {
                PlotChart.DataList.Remove((data as OutputChannelInfo).PlotLine);
            }
            PlotChart.RefreshPlotWithAutoScale();
            return;
        }
        #endregion

        #region 实时数据显示
        #endregion

        #region 文件保存

        public SaveWindow SetWindow;

        /// <summary>
        /// 历史保存时间
        /// </summary>
        public DateTime HistorySaveTime { get; set; } = DateTime.Now;

        Thread AutoSaveThread = null;

        /// <summary>
        /// 自动保存线程
        /// </summary>
        public void CreateAutoSaveThread()
        {
            AutoSaveThread = new Thread(() =>
            {
                while (true)
                {
                    double AutoSaveGap = 0;
                    string AutoFolderPath = "";
                    bool IsAutoSave = false;

                    bool isparamread = true;

                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            AutoSaveGap = double.Parse(SetWindow.AutoSaveGap.Text);
                            AutoFolderPath = SetWindow.AutoPath.Content.ToString();
                            IsAutoSave = SetWindow.AutoChooser.IsSelected;
                        }
                        catch (Exception) { isparamread = false; }
                    });
                    if (isparamread == false)
                    {
                        Thread.Sleep(1 * 60 * 1000);
                        continue;
                    }

                    if (IsAutoSave == false)
                    {
                        Thread.Sleep(1 * 60 * 1000);
                        continue;
                    }

                    if ((DateTime.Now - HistorySaveTime).TotalDays > AutoSaveGap)
                    {
                        HistorySaveTime = DateTime.Now;
                        //保存
                        if (Directory.Exists(AutoFolderPath))
                        {
                            try
                            {
                                Save(AutoFolderPath);
                            }
                            catch { }
                            finally
                            {
                                lock (lockobj)
                                {
                                    foreach (var item in LegendContent.Children)
                                    {
                                        ((item as StackPanel).Tag as ChannelInfo).PlotLine.ClearData();
                                    }
                                    Dispatcher.Invoke(() =>
                                    {
                                        PlotChart.RefreshPlotWithAutoScale();
                                    });
                                }
                                Dispatcher.Invoke(() =>
                                {
                                    ResizePlot(null, new RoutedEventArgs());
                                });
                            }
                        }
                    }
                    Thread.Sleep(1 * 60 * 1000);
                }
            });
            AutoSaveThread.Start();
        }


        TemperatureExpObject saveObject = new TemperatureExpObject();
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Save(string SavePath, string Filename = "")
        {
            saveObject = new TemperatureExpObject();
            if (LegendContent.Children.Count == 0)
            {
                throw new Exception("没有需要保存的数据");
            }
            DateTime mintime = DateTime.MaxValue;
            DateTime maxtime = DateTime.MinValue;
            #region 获取最早和最晚日期,设备名称
            List<string> DeviceNames = new List<string>();
            foreach (var item in LegendContent.Children)
            {
                ChannelInfo info = (item as StackPanel).Tag as ChannelInfo;
                if (!DeviceNames.Contains(info.ParentInfo.Device.ProductName))
                {
                    DeviceNames.Add(info.ParentInfo.Device.ProductName);
                }
                info.PlotLine.GetData(out IEnumerable<DateTime> x, out IEnumerable<double> y);
                if (x.ElementAt(0) < mintime)
                {
                    mintime = x.ElementAt(0);
                }
                if (x.ElementAt(x.Count() - 1) > maxtime)
                {
                    maxtime = x.ElementAt(x.Count() - 1);
                }
            }

            saveObject.Param.DeviceName.Value = "";
            foreach (var device in DeviceNames)
            {
                saveObject.Param.DeviceName.Value += device + ",";
            }
            if (saveObject.Param.DeviceName.Value != "") saveObject.Param.DeviceName.Value = saveObject.Param.DeviceName.Value.Remove(saveObject.Param.DeviceName.Value.Length - 1, 1);

            saveObject.Param.SetStartTime(mintime);
            saveObject.Param.SetEndTime(maxtime);
            #endregion

            //温度通道
            foreach (var item in LegendContent.Children)
            {
                ChannelInfo data = (item as StackPanel).Tag as ChannelInfo;

                data.PlotLine.GetData(out IEnumerable<DateTime> x, out IEnumerable<double> y);

                if (data is SensorChannelInfo)
                {
                    saveObject.SelectedChannelsData.Add(new NumricChartData1D(data.Name + "—温度(" + (data as SensorChannelInfo).Channel.Unit + ")", "温度监测数据") { Data = new List<double>(y.ToArray()) });
                    saveObject.SelectedChannelsData.Add(new TimeChartData1D(data.Name + "—时间", "温度监测数据") { Data = new List<DateTime>(x.ToArray()) });
                }
                if (data is OutputChannelInfo)
                {
                    saveObject.SelectedChannelsData.Add(new NumricChartData1D(data.Name + "—功率(" + (data as OutputChannelInfo).Channel.Unit + ")", "温度监测数据") { Data = new List<double>(y.ToArray()) });
                    saveObject.SelectedChannelsData.Add(new TimeChartData1D(data.Name + "—时间", "温度监测数据") { Data = new List<DateTime>(x.ToArray()) });
                }
            }

            string saveFileName = Filename;

            if (saveFileName == "")
            {
                saveFileName = "TemperatureData";

                saveFileName += " " + mintime.ToString("yyyy-MM-dd HH-mm-ss-FFF");
            }

            saveObject.WriteToFile(SavePath, saveFileName);

            TimeWindow window = new TimeWindow();
            window.Owner = MainWindow.Handle;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("文件保存成功");
        }

        /// <summary>
        /// 导出选项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportClick(object sender, RoutedEventArgs e)
        {
            SetWindow.ShowDialog();
        }
        #endregion

    }
}
