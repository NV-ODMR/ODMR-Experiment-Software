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
    public partial class TemperaturePage : ExpPageBase
    {
        public override string PageName { get; set; } = "温度监测";

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

        public override void InnerInit()
        {
            MainWindow.Dev_TemPeraPage.DataChangedEvent += RefreshChooserLegends;

            //创建自动保存线程
            CreateAutoSaveThread();

            TemperatureExpObj.Config.ReadFromPage(new FrameworkElement[] { this });

            CreateSampleThread();

        }

        List<ChannelInfo> SelectedInfos = new List<ChannelInfo>();

        public override void CloseBehaviour()
        {
            AutoSaveThread?.Abort();

            SampleThread?.Abort();
        }

        public override void UpdateParam()
        {
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

        #endregion

        #region 数据采集

        /// <summary>
        /// 设置采样时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSampleTime(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SampleTimeText.Text, out double value))
            {
                TemperatureExpObj.Config.SampleTimeText.Value = value;
                TimeWindow window = new TimeWindow();
                window.Owner = null;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowWindow("设置成功");
            }
            else
            {
                MessageWindow.ShowTipWindow("采样时间间隔值必须是数字", Window.GetWindow(this));
            }
        }

        #endregion

        #region 图表绘制
        /// <summary>
        /// 刷新图例列表
        /// </summary>
        private void RefreshChooserLegends(List<ChannelInfo> datas)
        {
            SelectedInfos = datas;
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
            }

            List<KeyValuePair<TimeChartData1D, NumricChartData1D>> newsource = new List<KeyValuePair<TimeChartData1D, NumricChartData1D>>();

            foreach (var item in datas)
            {
                int ind = Chart.DataSource.FindIndex(x => x.Value.Name == item.Name);
                if (ind != -1)
                {
                    newsource.Add(Chart.DataSource[ind]);
                }
                else
                {
                    newsource.Add(new KeyValuePair<TimeChartData1D, NumricChartData1D>(item.Time, item.Value));
                }
            }
            Chart.DataSource.Clear(false);
            Chart.DataSource.AddRange(newsource);

            Chart.UpdateChartAndDataFlow(false);
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
                                    foreach (var item in Chart.DataSource)
                                    {
                                        item.Key.Data.Clear();
                                        item.Value.Data.Clear();
                                    }
                                    Dispatcher.Invoke(() =>
                                    {
                                        Chart.UpdateChartAndDataFlow(true);
                                    });
                                }
                            }
                        }
                    }
                    Thread.Sleep(1 * 60 * 1000);
                }
            });
            AutoSaveThread.Start();
        }


        TemperatureExpObject TemperatureExpObj = new TemperatureExpObject();
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Save(string SavePath, string Filename = "")
        {
            TemperatureExpObj = new TemperatureExpObject();
            if (Chart.DataSource.Count == 0)
            {
                throw new Exception("没有需要保存的数据");
            }
            double mintime = double.MaxValue;
            double maxtime = double.MinValue;
            #region 获取最早和最晚日期,设备名称
            List<string> DeviceNames = new List<string>();
            foreach (var item in SelectedInfos)
            {
                if (!DeviceNames.Contains(item.ParentInfo.Device.ProductName))
                {
                    DeviceNames.Add(item.ParentInfo.Device.ProductName);
                }
                var time = item.Time.GetDataCopyAsDouble().ToList();
                if (time.ElementAt(0) < mintime)
                {
                    mintime = time.ElementAt(0);
                }
                if (time.ElementAt(time.Count() - 1) > maxtime)
                {
                    maxtime = time.ElementAt(time.Count() - 1);
                }
            }

            TemperatureExpObj.Param.DeviceName.Value = "";
            foreach (var device in DeviceNames)
            {
                TemperatureExpObj.Param.DeviceName.Value += device + ",";
            }
            if (TemperatureExpObj.Param.DeviceName.Value != "") TemperatureExpObj.Param.DeviceName.Value = TemperatureExpObj.Param.DeviceName.Value.Remove(TemperatureExpObj.Param.DeviceName.Value.Length - 1, 1);

            TemperatureExpObj.Param.SetStartTime(DateTime.FromOADate(mintime));
            TemperatureExpObj.Param.SetEndTime(DateTime.FromOADate(maxtime));
            #endregion

            foreach (var item in Chart.DataSource)
            {
                TemperatureExpObj.SelectedChannelsData.Add(new NumricChartData1D(item.Value.Name, item.Value.GroupName) { Data = new List<double>(item.Value.Data.ToArray()) });
                TemperatureExpObj.SelectedChannelsData.Add(new TimeChartData1D(item.Value.Name + "—时间", item.Value.GroupName) { Data = new List<DateTime>(item.Key.Data.ToArray()) });
            }

            string saveFileName = Filename;

            if (saveFileName == "")
            {
                saveFileName = "TemperatureData";

                saveFileName += " " + mintime.ToString("yyyy-MM-dd HH-mm-ss-FFF");
            }

            TemperatureExpObj.WriteToFile(SavePath, saveFileName);

            TimeWindow window = new TimeWindow();
            window.Owner = Window.GetWindow(this);
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

        #region 温度采样部分

        /// <summary>
        /// 采样线程
        /// </summary>
        public Thread SampleThread = null;

        /// <summary>
        /// 创建采样线程
        /// </summary>
        public void CreateSampleThread()
        {
            SampleThread = new Thread(() =>
            {
                while (true)
                {
                    for (int i = 0; i < SelectedInfos.Count; ++i)
                    {
                        double value = double.NaN;
                        if (SelectedInfos[i] is SensorChannelInfo)
                        {
                            value = (SelectedInfos[i] as SensorChannelInfo).Channel.Temperature;
                        }
                        if (SelectedInfos[i] is OutputChannelInfo)
                        {
                            value = (SelectedInfos[i] as OutputChannelInfo).Channel.Power;
                        }

                        SelectedInfos[i].Time.Data.Add(DateTime.Now);
                        SelectedInfos[i].Value.Data.Add(value);

                        Dispatcher.Invoke(() =>
                        {
                            foreach (TemperaturePannel item in NumricPanel.Children)
                            {
                                if ((item.Tag as ChannelInfo).Value.Data.Count == 0) continue;
                                double v = (item.Tag as ChannelInfo).Value.Data.Last();
                                if (item.Tag is SensorChannelInfo)
                                {
                                    item.Value.Text = Math.Round(v, 7).ToString() + (item.Tag as SensorChannelInfo).Channel.Unit;
                                }
                                if (item.Tag is OutputChannelInfo)
                                {
                                    item.Value.Text = Math.Round(v, 7).ToString() + (item.Tag as OutputChannelInfo).Channel.Unit;
                                }
                            }
                            Chart.UpdateChartAndDataFlow(false);
                        });
                    }
                    Thread.Sleep((int)(10 + TemperatureExpObj.Config.SampleTimeText.Value * 1000));
                }
            });

            SampleThread.Start();
        }
        #endregion

    }
}
