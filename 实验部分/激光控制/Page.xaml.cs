using Controls;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.其他设备;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Clipboard = System.Windows.Clipboard;

namespace ODMR_Lab.激光控制
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public DisplayPage()
        {
            InitializeComponent();
            Chart.DataList.Add(APDDisplayData);
        }

        public override string PageName { get; set; } = "Trace窗口";

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
            ConfigParam.ReadFromPage(new FrameworkElement[] { this }, false);
        }

        #region 采样部分

        Thread SampleThread = null;
        Thread PlotThread = null;
        bool IsSampleEnd = false;
        APDInfo CurrentAPD = null;
        PulseBlasterInfo CurrentPB = null;

        TraceConfigParams ConfigParam = new TraceConfigParams();

        public Queue<double> APDSampleData = new Queue<double>();
        public NumricDataSeries APDDisplayData { get; set; } = new NumricDataSeries("光子计数率", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, MarkerSize = 4, LineThickness = 1 };

        private void StartAPDSample(object sender, RoutedEventArgs e)
        {
            if (APDDevice.SelectedItem == null || PBDevice.SelectedItem == null) return;
            CurrentAPD = APDDevice.SelectedItem.Tag as APDInfo;
            CurrentPB = PBDevice.SelectedItem.Tag as PulseBlasterInfo;
            try
            {
                CurrentAPD.BeginUse();
                CurrentPB.BeginUse();
                MainWindow.Dev_APDPage.UpdateSourceState();
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("设备正在使用,无法开始计数", Window.GetWindow(this));
                return;
            }

            SetStartState();
            try
            {
                var dev = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster, CurrentAPD.TraceSourceName) as PulseBlasterInfo;
                try
                {
                    dev.Device?.End();
                }
                catch (Exception)
                {
                }
                //打开激光
                LaserOn lon = new LaserOn();
                double light = 0;
                try
                {
                    light = double.Parse(Ligntness.Text);
                }
                catch (Exception) { }
                if (light < 0) light = 0;
                if (light > 100) light = 100;
                lon.CoreMethod(new List<object>() { light / 100 }, CurrentPB);
                //打开APDTrace源
                dev.Device.PulseFrequency = ConfigParam.SampleFreq.Value;
                dev.Device.Start();
                CurrentAPD.StartContinusSample();
            }
            catch (Exception ex)
            {
                SetStopState();
                try
                {
                    //关闭激光
                    LaserOff loff = new LaserOff();
                    loff.CoreMethod(new List<object>(), CurrentPB);
                }
                catch (Exception)
                {
                }
                CurrentAPD.EndUse();
                CurrentPB.EndUse();
                MainWindow.Dev_APDPage.UpdateSourceState();
                return;
            }
            // ★ 轮询治理说明（2026-10-05）：本采样循环属于【按需触发】，不是自动轮询 ——
            //   线程仅在用户点击"开始采样"后才创建，IsSampleEnd=true（点击"结束采样"）即退出；
            //   页面切走不会中断已开始的采集（数据采集 ≠ 界面刷新，故此处刻意不加可见性门控）。
            //   其次，其节拍由 DAQ 硬件采样时钟决定：CurrentAPD.GetContinusSampleRatio() 每次阻塞读取
            //   2 个采样点（HardWares\仪器列表\apd\Exclitas SPCM-AQRH\APD.cs:103/126/190-195，
            //   采样时钟 = 脉冲输出频率，见本页 line100: dev.Device.PulseFrequency = ConfigParam.SampleFreq.Value）。
            //   ⇒ 这里【不得】加入毫秒级固定 Sleep：消费端一旦慢于生产端，DAQ 缓冲（仅 2 点）立即溢出，
            //     采集会以 -200279 报错中断。需要降低采集速率时，正确做法是调小界面"采样频率"
            //     （ConfigParam.SampleFreq，默认 100Hz），时间轴 i/freq 仍自洽。
            IsSampleEnd = false;

            SampleThread = new Thread(() =>
            {
                while (!IsSampleEnd)
                {
                    try
                    {
                        double value = CurrentAPD.GetContinusSampleRatio();
                        APDSampleData.Enqueue(value);
                        while (APDSampleData.Count > ConfigParam.MaxSavePoint.Value)
                        {
                            APDSampleData.Dequeue();
                        }
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(30);
                    }
                }
            });
            SampleThread.IsBackground = true;
            SampleThread.Start();
            PlotThread = new Thread(() =>
            {
                while (!IsSampleEnd)
                {
                    try
                    {
                        List<double> buffer = new List<double>();
                        lock (APDSampleData)
                        {
                            buffer = APDSampleData.ToArray().ToList();
                        }
                        int count = buffer.Count;
                        int displaycount = ConfigParam.MaxDisplayPoint.Value;
                        if (displaycount > count) displaycount = count;
                        APDDisplayData.X = Enumerable.Range(count - displaycount, count).Select(x => (double)x).ToList();
                        APDDisplayData.Y = buffer.GetRange(count - displaycount, displaycount);
                        Dispatcher.Invoke(() =>
                        {
                            Chart.RefreshPlotWithAutoScale();
                            if (buffer.Count == 0)
                                CountRate.Text = "0";
                            else
                                CountRate.Text = buffer.Last().ToString();
                        });
                    }
                    catch (Exception) { }
                    Thread.Sleep(150);
                }
            });
            PlotThread.IsBackground = true;
            PlotThread.Start();
        }

        private void SetStartState()
        {
            Dispatcher.Invoke(() =>
            {
                APDDevice.IsEnabled = false;
                BeginTraceBtn.IsEnabled = false;
                EndTraceBtn.IsEnabled = true;
            });
        }

        private void SetStopState()
        {
            Dispatcher.Invoke(() =>
            {
                APDDevice.IsEnabled = true;
                BeginTraceBtn.IsEnabled = true;
                EndTraceBtn.IsEnabled = false;
            });
        }

        private void EndAPDSample(object sender, RoutedEventArgs e)
        {
            IsSampleEnd = true;
            APDDevice.IsEnabled = false;
            while (SampleThread.ThreadState != ThreadState.Stopped && PlotThread.ThreadState != ThreadState.Stopped)
            {
                Thread.Sleep(30);
            }
            var dev = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster, CurrentAPD.TraceSourceName);
            if (dev == null) return;
            (dev as PulseBlasterInfo).Device?.End();
            try
            {
                //关闭激光
                LaserOff loff = new LaserOff();
                loff.CoreMethod(new List<object>(), CurrentPB);
            }
            catch (Exception)
            {
            }
            //结束APD计数
            CurrentAPD.EndContinusSample();
            SetStopState();
            CurrentPB.EndUse();
            CurrentAPD.EndUse();
            MainWindow.Dev_APDPage.UpdateSourceState();
        }

        private void UpdateAPDDeviceList(object sender, RoutedEventArgs e)
        {
            APDDevice.Items.Clear();
            APDDevice.TemplateButton = UIUpdater.ButtonTemplate;
            var APDs = DeviceDispatcher.GetDevice(DeviceTypes.光子计数器);
            foreach (var item in APDs)
            {
                APDDevice.Items.Add(new DecoratedButton() { Text = (item as APDInfo).GetDeviceDescription(), Tag = item });
            }
        }

        private void UpdatePBDeviceList(object sender, RoutedEventArgs e)
        {
            PBDevice.Items.Clear();
            PBDevice.TemplateButton = UIUpdater.ButtonTemplate;
            var pbs = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster);
            foreach (var item in pbs)
            {
                PBDevice.Items.Add(new DecoratedButton() { Text = (item as PulseBlasterInfo).GetDeviceDescription(), Tag = item });
            }
        }

        /// <summary>
        /// 应用数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Apply(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigParam.ReadFromPage(new FrameworkElement[] { this }, true);
                if (ConfigParam.MaxDisplayPoint.Value > ConfigParam.MaxSavePoint.Value)
                {
                    ConfigParam.MaxDisplayPoint.Value = ConfigParam.MaxSavePoint.Value;
                }
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("参数格式错误", Window.GetWindow(this));
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

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            string save = "时间(s)\t" + "计数(cps)\n";
            List<double> res = APDSampleData.ToArray().ToList();
            double freq = ConfigParam.SampleFreq.Value;
            for (int i = 0; i < res.Count; i++)
            {
                save += (i / freq).ToString() + "\t" + res[i].ToString() + "\n";
            }

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
        /// 清空数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearData(object sender, RoutedEventArgs e)
        {
            lock (APDSampleData) lock (APDDisplayData)
                {
                    APDSampleData.Clear();
                    APDDisplayData.X.Clear();
                    APDDisplayData.Y.Clear();
                }
            Chart.RefreshPlotWithAutoScale();
        }
    }
}
