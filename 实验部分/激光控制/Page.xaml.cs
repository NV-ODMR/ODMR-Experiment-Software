using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using HardWares.APD.Exclitas_SPCM_AQRH;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = Controls.ContextMenu;
using Path = System.IO.Path;

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
        }

        #region 采样部分

        Thread SampleThread = null;
        Thread PlotThread = null;
        bool IsSampleEnd = false;
        APDInfo CurrentAPD = null;

        TraceConfigParams ConfigParam = null;

        public Queue<double> APDSampleData = new Queue<double>();
        public NumricDataSeries APDDisplayData { get; set; } = new NumricDataSeries("光子计数率", new List<double>(), new List<double>()) { LineColor = Colors.LightGreen, MarkerSize = 4, LineThickness = 1 };

        private void StartAPDSample(object sender, RoutedEventArgs e)
        {
            if (APDDevice.SelectedItem == null) return;
            CurrentAPD = APDDevice.SelectedItem.Tag as APDInfo;
            try
            {
                CurrentAPD.BeginUse();
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("设备正在使用,无法开始计数", Window.GetWindow(this));
                return;
            }

            SetStartState();
            try
            {
                CurrentAPD.TraceSource.Device?.End();
                CurrentAPD.TraceSource.Device.PulseFrequency = ConfigParam.SampleFreq.Value;
                CurrentAPD.TraceSource.Device.Start();
                CurrentAPD.StartContinusSample();
            }
            catch (Exception ex)
            {
                SetStopState();
                return;
            }
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
            SampleThread.Start();
            PlotThread = new Thread(() =>
            {
                while (!IsSampleEnd)
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
                    Thread.Sleep(30);
                }
            });
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
            CurrentAPD.TraceSource.Device.End();
            CurrentAPD.EndContinusSample();
            SetStopState();
            CurrentAPD.EndUse();
        }

        private void UpdateDeviceList(object sender, RoutedEventArgs e)
        {
            APDDevice.Items.Clear();
            APDDevice.TemplateButton = APDDevice;
            var APDs = DeviceDispatcher.GetDevice(DeviceTypes.光子计数器);
            foreach (var item in APDs)
            {
                APDDevice.Items.Add(new DecoratedButton() { Text = (item as APDInfo).Device.ProductName, Tag = item });
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
            lock (APDSampleData)
                APDSampleData.Clear();
        }
    }
}
