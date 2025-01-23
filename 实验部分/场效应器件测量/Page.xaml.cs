using CodeHelper;
using Controls;
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
        }

        public override void Init()
        {
            CreateCurrentLimitListener();
        }

        public override void CloseBehaviour()
        {
            CurrentLimitListener.Abort();
            while (CurrentLimitListener.ThreadState == ThreadState.Running) Thread.Sleep(10);
        }

        #region IV测量部分
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


        bool ToStop = false;

        /// <summary>
        /// IV测量结果
        /// </summary>
        IVMeasureFileObject IVMeasureObj = new IVMeasureFileObject();
        /// <summary>
        /// 开始测量IV
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeginIV(object sender, RoutedEventArgs e)
        {
            IVMeasureProgress.Value = 0.5;
            Thread.Sleep(1);
            IVMeasureProgress.Value = 1;

            if (IVDevice.SelectedItem == null)
            {
                MessageWindow.ShowTipWindow("未选择进行IV测量的源表", MainWindow.Handle);
                return;
            }

            IVBeginBtn.IsEnabled = false;

            string scanstrs = "0 V\n";
            List<double> scanpoints = new List<double>() { 0 };
            foreach (var item in ScanPointPanel.Children)
            {
                try
                {
                    double value = double.Parse((item as TextBox).Text);
                    scanpoints.Add(value);
                    scanstrs += value.ToString() + " V\n";
                }
                catch (Exception)
                {
                    continue;
                }
            }

            IVMeasureObj.Config.ScanPoints = scanpoints;

            try
            {
                //获取测量参数
                IVMeasureObj.Param.ReadFromPage(new FrameworkElement[] { this });
            }
            catch (Exception exc)
            {
                MessageWindow.ShowTipWindow("存在格式错误的参数，请检查参数格式。", MainWindow.Handle);
                return;
            }

            if (MessageWindow.ShowMessageBox("确认IV测量参数", "测量路径点：\n" + scanstrs + "继续执行将会在电源上设置非零伏电压,同时修改电流限制值，是否继续？", MessageBoxButton.YesNo, owner: MainWindow.Handle) == MessageBoxResult.Yes)
            {
                Thread t = new Thread(() =>
                {
                    SetBeginState();
                    ToStop = false;

                    IVResultWindow.SetTitle("IV测量结果，开始时间：" + DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss"));

                    IVMeasureObj.IVVData.Clear();
                    IVMeasureObj.IVTimes.Clear();
                    IVMeasureObj.IVIData.Clear();
                    IVMeasureObj.IVTatgetData.Clear();

                    PowerMeterInfo dev = null;
                    Dispatcher.Invoke(() =>
                    {
                        //获取设备
                        dev = DeviceDispatcher.TryGetPowerMeterDevice(IVDevice.SelectedItem.Tag as PowerMeterInfo, OperationMode.ReadWrite, true, true);
                    });
                    try
                    {
                        if (dev == null)
                        {
                            SetStopState();
                            dev.AllowAutoMeasure = true;
                            dev.EndUse();
                            return;
                        }
                        //测量
                        dev.BeginUse();

                        dev.Device.VoltageRampGap = IVMeasureObj.Config.IVRampGap.Value;
                        dev.Device.VoltageRampStep = IVMeasureObj.Config.IVRampStep.Value;
                        dev.Device.CurrentLimit = IVMeasureObj.Config.IVCurrentLimit.Value;

                        //停止自动采样
                        dev.AllowAutoMeasure = false;
                        while (dev.IsMeasuring)
                        {
                            Thread.Sleep(10);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            IVMeasureProgress.Value = 0;
                        });


                        double begin = 0;
                        double end = 0;

                        //预计总点数
                        int count = 0;
                        for (int i = 0; i < scanpoints.Count - 1; i++)
                        {
                            begin = scanpoints[i];
                            end = scanpoints[i + 1];
                            int sgn = (end - begin) > 0 ? 1 : -1;

                            double temp = begin + IVMeasureObj.Config.IVScanStep.Value * sgn;
                            while ((temp - end) * sgn < 0)
                            {
                                count += 1;
                                temp += IVMeasureObj.Config.IVScanStep.Value * sgn;
                            }
                        }

                        int ind = 0;
                        MeasureGroup g;
                        for (int i = 0; i < scanpoints.Count - 1; i++)
                        {
                            begin = scanpoints[i];
                            end = scanpoints[i + 1];
                            int sgn = (end - begin) > 0 ? 1 : -1;
                            double temp = begin;
                            while ((temp - end) * sgn < 0)
                            {
                                if (ToStop == true)
                                {
                                    SetBeginState();
                                    return;
                                }
                                dev.Device.TargetVoltage = temp;
                                g = dev.Device.Measure();
                                IVMeasureObj.IVTimes.Add(g.TimeStamp);
                                IVMeasureObj.IVIData.Add(g.Current);
                                IVMeasureObj.IVTatgetData.Add(temp);
                                IVMeasureObj.IVVData.Add(g.Voltage);
                                temp += IVMeasureObj.Config.IVScanStep.Value * sgn;
                                //设置进度条
                                Dispatcher.Invoke(() =>
                                {
                                    IVMeasureProgress.Value = ind * 90.0 / count;
                                });
                                //更新结果
                                UpdateResult();
                                ind += 1;
                            }
                        }

                        dev.Device.TargetVoltage = end;
                        g = dev.Device.Measure();
                        IVMeasureObj.IVTimes.Add(g.TimeStamp);
                        IVMeasureObj.IVIData.Add(g.Current);
                        IVMeasureObj.IVTatgetData.Add(end);
                        IVMeasureObj.IVVData.Add(g.Voltage);

                        //测量完成后返回0V
                        dev.Device.TargetVoltage = 0;
                        Dispatcher.Invoke(() =>
                        {
                            IVMeasureProgress.Value = 100;
                            IVResultBtn.IsEnabled = true;
                        });

                        dev.EndUse();
                    }
                    catch (Exception exc)
                    {
                        MessageWindow.ShowTipWindow("IV测量过程中遇到错误," + exc.Message, MainWindow.Handle);
                    }
                    finally
                    {
                        //开始自动采样
                        dev.EndUse();
                        dev.AllowAutoMeasure = true;
                        Dispatcher.Invoke(() =>
                        {
                            SetStopState();
                        });
                    }
                });
                t.Start();
            }
            else
            {
                SetStopState();
            }
        }


        private void IVStop(object sender, RoutedEventArgs e)
        {
            ToStop = true;
        }

        public void SetStopState()
        {
            Dispatcher.Invoke(() =>
            {
                IVStopBtn.IsEnabled = false;
                IVBeginBtn.IsEnabled = true;
                IVMeasureObj.Param.SetEndTime(DateTime.Now);
                IVEndTime.Content = IVMeasureObj.Param.ExpEndTime.Value;
                IVEndTime.ToolTip = IVMeasureObj.Param.ExpEndTime.Value;
            });
        }

        public void SetBeginState()
        {
            Dispatcher.Invoke(() =>
            {
                IVStopBtn.IsEnabled = true;
                IVBeginBtn.IsEnabled = false;
                IVMeasureObj.Param.SetStartTime(DateTime.Now);
                IVStartTime.Content = IVMeasureObj.Param.ExpStartTime.Value;
                IVStartTime.ToolTip = IVMeasureObj.Param.ExpStartTime.Value;
            });
        }

        ChartViewerWindow IVResultWindow = new ChartViewerWindow(true);
        /// <summary>
        /// IV测量结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IVResult(object sender, RoutedEventArgs e)
        {
            IVResultWindow.ShowAs1D(new List<ChartData1D>()
            {
                new NumricChartData1D()
                {
                    Data=IVMeasureObj.IVVData,
                    Name="IV测量-电压测量值(V)"
                },
                  new NumricChartData1D()
                {
                    Data=IVMeasureObj.IVIData,
                    Name="IV测量-电流测量值(A)"
                },
                    new NumricChartData1D()
                {
                    Data=IVMeasureObj.IVTatgetData,
                    Name="IV测量-电压设置值(V)"
                },
                      new TimeChartData1D()
                {
                    Data=IVMeasureObj.IVTimes,
                    Name="IV测量-采样时间点"
                },
            });
        }

        private void UpdateResult()
        {
            Dispatcher.Invoke(() =>
            {
                IVResultWindow.UpdateChart(true);
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
                    win.Owner = MainWindow.Handle;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("文件保存未完成:\n" + ex.Message, MainWindow.Handle);
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
            MessageWindow.ShowTipWindow("当指示灯为绿色时表示未限流，红色表示限流", MainWindow.Handle);
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
                //停止采样
                meter = DeviceDispatcher.TryGetPowerMeterDevice(meter, OperationMode.Read, true, true);
                if (meter == null)
                {
                    return;
                }
                meter.BeginUse();

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
                MessageWindow.ShowTipWindow("参数格式错误", MainWindow.Handle);
            }

            Thread t = new Thread(() =>
            {
                PowerMeterInfo meter = null;
                Dispatcher.Invoke(() =>
                {
                    meter = VoltageSetDevice.SelectedItem.Tag as PowerMeterInfo;
                });
                //停止采样
                meter = DeviceDispatcher.TryGetPowerMeterDevice(meter, OperationMode.Write, true, true);
                if (meter == null)
                {
                    return;
                }
                meter.BeginUse();

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
