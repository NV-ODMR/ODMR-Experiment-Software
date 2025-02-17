using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using HardWares.APD;
using HardWares.APD.Exclitas_SPCM_AQRH;
using HardWares.Windows;
using HardWares.仪器列表.电动翻转座;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.源表;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.相机_翻转镜;
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
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.设备部分.光子探测器
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "相机";

        public List<APDInfo> APDs { get; set; } = new List<APDInfo>();
        public List<FlipMotorInfo> Flips { get; set; } = new List<FlipMotorInfo>();


        public DevicePage()
        {
            InitializeComponent();
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
            ConfigParam = new APDDevConfigParams();
            ConfigParam.ReadFromPage(new FrameworkElement[] { this }, false);
        }

        private void NewAPDConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(APDBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                APDInfo apd = new APDInfo() { Device = window.ConnectedDevice as APDBase, ConnectInfo = window.ConnectInfo };
                apd.CreateDeviceInfoBehaviour();

                APDs.Add(apd);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }

        public override void RefreshPanels()
        {
            APDList.ClearItems();
            foreach (var item in APDs)
            {
                APDList.AddItem(item, item.Device.ProductName);
            }
        }

        /// <summary>
        /// 右键菜单事件
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void ContextMenuEvent(int arg1, int arg2, object arg3)
        {
            APDInfo inf = arg3 as APDInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    inf.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    APDs.Remove(inf);
                    RefreshPanels();
                }
            }
            #endregion

            #region 参数设置
            if (arg1 == 1)
            {
                ParameterWindow window = new ParameterWindow(inf.Device, Window.GetWindow(this));
                window.ShowDialog();
            }
            #endregion
        }

        #region 采样部分

        Thread SampleThread = null;
        bool IsSampleEnd = false;
        APDBase CurrentAPD = null;

        APDDevConfigParams ConfigParam = null;

        public Queue<double> APDSampleData = new Queue<double>();
        public NumricDataSeries APDDisplayData { get; set; } = new NumricDataSeries("光子计数率", new List<double>(), new List<double>());

        private void StartAPDSample(object sender, RoutedEventArgs e)
        {
            SetStartState();
            try
            {
                CurrentAPD = APDDevice.SelectedItem.Tag as APDBase;
                CurrentAPD.BeginContinusSample(ConfigParam.SampleFreq.Value);
            }
            catch (Exception)
            {
                SetStopState();
                return;
            }
            SampleThread = new Thread(() =>
            {
                while (!IsSampleEnd)
                {
                    double value = CurrentAPD.GetContinusCountRatio();
                    APDSampleData.Enqueue(value);
                    while (APDSampleData.Count > ConfigParam.MaxSavePoint.Value)
                    {
                        APDSampleData.Dequeue();
                    }
                    int displaycount = ConfigParam.MaxDisplayPoint.Value;
                    if (displaycount > APDSampleData.Count) displaycount = APDSampleData.Count;
                    APDDisplayData.X = Enumerable.Range(APDSampleData.Count - displaycount, displaycount).Select(x => (double)x).ToList();
                    APDDisplayData.Y = APDSampleData.ToList().GetRange(APDSampleData.Count - displaycount, displaycount);
                    Dispatcher.Invoke(() =>
                    {
                        Chart.RefreshPlotWithAutoScale();
                    });
                }
            });
            SampleThread.Start();
        }

        private void SetStartState()
        {
            Dispatcher.Invoke(() =>
            {
                APDDevice.IsEnabled = false;
                BeginBtn.IsEnabled = false;
                EndBtn.IsEnabled = true;
            });
        }

        private void SetStopState()
        {
            Dispatcher.Invoke(() =>
            {
                APDDevice.IsEnabled = true;
                BeginBtn.IsEnabled = true;
                EndBtn.IsEnabled = false;
            });
        }

        private void EndAPDSample(object sender, RoutedEventArgs e)
        {
            IsSampleEnd = true;
            APDDevice.IsEnabled = false;
            while (SampleThread.ThreadState != ThreadState.Stopped)
            {
                Thread.Sleep(30);
            }

        }

        private void UpdateDeviceList(object sender, RoutedEventArgs e)
        {
            APDDevice.Items.Clear();
            APDDevice.TemplateButton = APDDevice;
            foreach (var item in APDs)
            {
                APDDevice.Items.Add(new DecoratedButton() { Text = item.Device.ProductName });
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
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("参数格式错误", Window.GetWindow(this));
            }
        }
        #endregion
    }
}
