using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.源表;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.位移台部分;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using ODMR_Lab.实验部分.场效应器件测量;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using 温度监控程序.Windows;
using ContextMenu = Controls.ContextMenu;
using Path = System.IO.Path;

namespace ODMR_Lab.场效应器件测量
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : PageBase
    {

        public List<PowerMeterInfo> PowerMeterList { get; set; } = new List<PowerMeterInfo>();

        /// <summary>
        /// 当前选中的源表
        /// </summary>
        public PowerMeterInfo CurrentSelectedMeter { get; set; } = null;

        public DisplayPage()
        {
            InitializeComponent();
            InitIVThread();
        }

        public override void Init()
        {
            CreateCurrentLimitListener();
        }

        public override void CloseBehaviour()
        {
            CurrentLimitListener.Abort();
            while (CurrentLimitListener.ThreadState == ThreadState.Running) Thread.Sleep(10);
            IVMeasureObj?.Dispose();
        }

        #region IV测量部分
        public void InitIVThread()
        {
            IVMeasureObj.ExpPage = this;
            IVMeasureObj.StartButton = IVBeginBtn;
            IVMeasureObj.StopButton = IVStopBtn;
            IVMeasureObj.ExpStartTimeLabel = IVStartTime;
            IVMeasureObj.ExpEndTimeLabel = IVEndTime;
        }

        /// <summary>
        /// 添加扫描路径点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddScanPoint(object sender, RoutedEventArgs e)
        {
            ScanPointPanel.Children.Add(CreatePointBar());
        }

        private TextBox CreatePointBar()
        {
            TextBox g = new TextBox();
            g.Height = 30;
            g.BorderThickness = new Thickness(0);
            g.HorizontalContentAlignment = HorizontalAlignment.Center;
            g.VerticalContentAlignment = VerticalAlignment.Center;
            g.Foreground = Brushes.White;
            MouseColorHelper help = new MouseColorHelper(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")));
            help.RegistateTarget(g);
            g.CaretBrush = Brushes.White;
            ContextMenu menu = new ContextMenu();
            DecoratedButton bt = new DecoratedButton();
            IVBeginBtn.CloneStyleTo(bt);
            bt.Text = "删除";
            bt.Tag = g;
            bt.Click += DeletePoint;
            menu.Items.Add(bt);
            menu.ApplyToControl(g);
            return g;
        }

        private void DeletePoint(object sender, RoutedEventArgs e)
        {
            ScanPointPanel.Children.Remove((sender as DecoratedButton).Tag as TextBox);
        }


        /// <summary>
        /// IV测量结果
        /// </summary>
        IVMeasureExpObject IVMeasureObj = new IVMeasureExpObject();

        public ChartViewerWindow IVResultWindow = new ChartViewerWindow(true);
        /// <summary>
        /// IV测量结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IVResult(object sender, RoutedEventArgs e)
        {
            IVResultWindow.ShowAs1D(new List<ChartData1D>()
            {
                new NumricChartData1D("电压测量值(V)","IV测试数据")
                {
                    Data=IVMeasureObj.IVVData,
                },
                  new NumricChartData1D("电流测量值(A)","IV测试数据")
                {
                    Data=IVMeasureObj.IVIData,
                },
                    new NumricChartData1D("电压设置值(V)","IV测试数据")
                {
                    Data=IVMeasureObj.IVTatgetData,
                },
                      new TimeChartData1D("采样时间点","IV测试数据")
                {
                    Data=IVMeasureObj.IVTimes,
                },
            });
        }

        public void UpdateResult()
        {
            Dispatcher.Invoke(() =>
            {
                IVResultWindow.UpdateChartAndDataFlow(true);
            });
        }

        /// <summary>
        /// 保存测量结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IVSave(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IVStartTime.Content.ToString() == "" || IVEndTime.Content.ToString() == "")
                {
                    return;
                }
                string FileName = IVStartTime.Content.ToString();
                FileName = FileName.Replace(":", "_");
                FileName = "IVMeasure " + FileName;
                FileName += ".userdat";
                bool result = IVMeasureObj.WriteFromExplorer(FileName);
                if (result)
                {
                    TimeWindow win = new TimeWindow();
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("文件保存未完成:\n" + ex.Message, Window.GetWindow(this));
            }
        }
        #endregion


        /// <summary>
        /// 更新设备列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDeviceList(object sender, RoutedEventArgs e)
        {
            IVDevice.Items.Clear();
            VoltageSetDevice.Items.Clear();
            foreach (PowerMeterInfo meter in MainWindow.Dev_PowerMeterPage.PowerMeterList)
            {
                DecoratedButton btn = new DecoratedButton();
                btn.Text = meter.Device.ProductName;
                IVBeginBtn.CloneStyleTo(btn);
                IVDevice.Items.Add(btn);
                btn.Tag = meter;

                btn = new DecoratedButton();
                btn.Text = meter.Device.ProductName;
                IVBeginBtn.CloneStyleTo(btn);
                VoltageSetDevice.Items.Add(btn);
                btn.Tag = meter;
            }
        }

        #region 栅压设置部分

        private void ShowLimitInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("当指示灯为绿色时表示未限流，红色表示限流", Window.GetWindow(this));
        }


        /// <summary>
        /// 读取当前电压
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeasureVoltage(object sender, RoutedEventArgs e)
        {
            if (VoltageSetDevice.SelectedItem == null) return;
            Thread t = new Thread(() =>
            {
                PowerMeterInfo meter = null;
                Dispatcher.Invoke(() =>
                {
                    meter = VoltageSetDevice.SelectedItem.Tag as PowerMeterInfo;
                });

                try
                {
                    meter.BeginUse();
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("设备正在使用", Window.GetWindow(this));
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    (sender as DecoratedButton).IsEnabled = false;
                });
                while (meter.IsMeasuring)
                {
                    Thread.Sleep(20);
                }
                double result = meter.Device.Measure().Voltage;
                Dispatcher.Invoke(() =>
                {
                    VoltageReadValue.Content = result.ToString();
                    (sender as DecoratedButton).IsEnabled = true;
                });

                meter.EndUse();
            });
            t.Start();
        }

        /// <summary>
        /// 设置电压
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetMeterVoltage(object sender, RoutedEventArgs e)
        {
            if (VoltageSetDevice.SelectedItem == null) return;

            double volt = 0;
            double currlim = 0;
            double step = 0;
            int gap = 0;
            try
            {
                volt = double.Parse(SetVoltage.Text);
                currlim = double.Parse(SetCurrentLimit.Text);
                step = double.Parse(SetRampStep.Text);
                gap = int.Parse(SetRampGap.Text);
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("参数格式错误", Window.GetWindow(this));
            }

            Thread t = new Thread(() =>
            {
                PowerMeterInfo meter = null;
                Dispatcher.Invoke(() =>
                {
                    meter = VoltageSetDevice.SelectedItem.Tag as PowerMeterInfo;
                });
                //停止采样
                try
                {
                    meter.BeginUse();
                }
                catch (Exception)
                {
                    MessageWindow.ShowTipWindow("设备正在使用", Window.GetWindow(this));
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    (sender as DecoratedButton).IsEnabled = false;
                    VoltageSetState.Content = "正在调整电压...";
                });
                meter.AllowAutoMeasure = false;
                while (meter.IsMeasuring)
                {
                    Thread.Sleep(20);
                }
                meter.Device.CurrentLimit = currlim;
                meter.Device.VoltageRampGap = gap;
                meter.Device.VoltageRampStep = step;

                meter.Device.TargetVoltage = volt;
                meter.AllowAutoMeasure = true;
                Dispatcher.Invoke(() =>
                {
                    (sender as DecoratedButton).IsEnabled = true;
                    VoltageSetState.Content = "";
                });

                meter.EndUse();
            });
            t.Start();
        }


        Thread CurrentLimitListener = null;

        private void CreateCurrentLimitListener()
        {
            CurrentLimitListener = new Thread(() =>
            {
                while (true)
                {
                    PowerMeterInfo dev = null;
                    Dispatcher.Invoke(() =>
                    {
                        if (VoltageSetDevice.SelectedItem == null)
                        {
                            LimitState.Background = Brushes.Green;
                        }
                        else
                        {
                            dev = VoltageSetDevice.SelectedItem.Tag as PowerMeterInfo;
                        }
                    });
                    if (dev == null) continue;
                    Dispatcher.Invoke(() =>
                    {
                        if (dev.Device.IsCurrentLimited())
                        {
                            LimitState.Background = Brushes.Red;
                        }
                        else
                        {
                            LimitState.Background = Brushes.Green;
                        }
                    });
                    Thread.Sleep(50);
                }
            });
            CurrentLimitListener.Start();
        }
        #endregion
    }
}
