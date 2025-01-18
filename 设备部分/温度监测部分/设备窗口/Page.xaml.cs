using CodeHelper;
using Controls;
using DataBaseLib;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using 温度监控程序.Windows;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.温度监测部分
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : PageBase
    {
        /// <summary>
        /// 温控列表
        /// </summary>
        public List<TemperatureControllerInfo> TemperatureControllers { get; set; } = new List<TemperatureControllerInfo>();

        /// <summary>
        /// 当选中数据改变时触发的事件
        /// </summary>
        public event Action<List<ChannelInfo>> DataChangedEvent = null;

        /// <summary>
        /// 当需要刷新图表时触发的事件
        /// </summary>
        public event Action ChartUpdateEvent = null;


        public DevicePage()
        {
            InitializeComponent();
        }

        public override void Init()
        {
            CreateSampleThread();
        }

        public override void CloseBehaviour()
        {
            SampleThread?.Abort();
        }

        public override void UpdateDataBaseToUI()
        {
        }

        /// <summary>
        /// 列出所有需要向数据库取回的数据
        /// </summary>
        public override void ListDataBaseData()
        {
        }


        private ContextMenu CreateBtnMenu(ChannelInfo data)
        {
            ContextMenu menu = new ContextMenu()
            {
                PanelMinWidth = 200,
                ItemHeight = 30,
            };
            DecoratedButton btn = new DecoratedButton();
            btn.Text = "设置通道参数";
            MenuTemplateBtn.CloneStyleTo(btn);
            btn.Tag = data;
            menu.Items.Add(btn);
            btn.Click += OpenChannelWindow;
            return menu;
        }

        /// <summary>
        /// 打开参数设置窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OpenChannelWindow(object sender, RoutedEventArgs e)
        {
            ChannelInfo data = ((sender as DecoratedButton).Tag as ChannelInfo);
            List<Parameter> param = new List<Parameter>();
            if (data is OutputChannelInfo)
            {
                param = (data as OutputChannelInfo).Channel.GetAvailableParams();
            }
            if (data is SensorChannelInfo)
            {
                param = (data as SensorChannelInfo).Channel.GetAvailableParams();
            }
            ParameterWindow window = new ParameterWindow(param);
            window.ShowDialog();
        }

        private void NewConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(TemperatureControllerBase), MainWindow.Handle);
            bool res = window.ShowDialog(out PortObject dev);
            if (res == true)
            {
                TemperatureControllerInfo info = (TemperatureControllerInfo)new TemperatureControllerInfo().CreateDeviceInfo(dev as TemperatureControllerBase, window.ConnectInfo);
                TemperatureControllers.Add(info);
            }
            else
            {
                return;
            }
            RefreshConnectedControllers();
            RefreshChannelBtns();
        }

        private List<ChannelInfo> GetSelectedChannels()
        {
            List<ChannelInfo> result = new List<ChannelInfo>();
            foreach (var item in TemperatureControllers)
            {
                foreach (var channel in item.SensorsInfo)
                {
                    if (channel.IsSelected)
                    {
                        result.Add(channel);
                    }
                }
                foreach (var channel in item.OutputsInfo)
                {
                    if (channel.IsSelected)
                    {
                        result.Add(channel);
                    }
                }
            }
            return result;
        }

        private void ChangeChannelPanel(object sender, RoutedEventArgs e)
        {
            DecoratedButton btn = sender as DecoratedButton;
            ChannelInfo info = (btn.Tag as ChannelInfo);
            info.IsSelected = !info.IsSelected;
            DataChangedEvent?.Invoke(GetSelectedChannels());
            RefreshChannelBtns();
        }

        public void RefreshChannelBtns()
        {
            Channels.Children.Clear();
            SelectedChannel.Children.Clear();
            foreach (var item in TemperatureControllers)
            {
                foreach (var channel in item.SensorsInfo)
                {
                    DecoratedButton btn = channel.DisplayBtn;
                    if (btn.Tag == null)
                    {
                        SensorButtonTemplate.CloneStyleTo(btn);
                        btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                        btn.Height = 50;
                        btn.Tag = channel;
                        CreateBtnMenu(btn.Tag as SensorChannelInfo).ApplyToControl(btn);
                        btn.Click += ChangeChannelPanel;
                    }

                    if (channel.IsSelected)
                    {
                        SelectedChannel.Children.Add(channel.DisplayBtn);
                    }
                    else
                    {
                        Channels.Children.Add(channel.DisplayBtn);
                    }
                }
                foreach (var channel in item.OutputsInfo)
                {
                    DecoratedButton btn = channel.DisplayBtn;
                    if (btn.Tag == null)
                    {
                        OutputbtnTemplate.CloneStyleTo(btn);
                        btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                        btn.Height = 50;
                        btn.Tag = channel;
                        CreateBtnMenu(btn.Tag as OutputChannelInfo).ApplyToControl(btn);
                        btn.Click += ChangeChannelPanel;
                    }

                    if (channel.IsSelected)
                    {
                        SelectedChannel.Children.Add(channel.DisplayBtn);
                    }
                    else
                    {
                        Channels.Children.Add(channel.DisplayBtn);
                    }
                }
            }
        }

        public void RefreshConnectedControllers()
        {
            ConnectedDevices.Children.Clear();
            DevicesStates.Children.Clear();
            foreach (var item in TemperatureControllers)
            {
                DecoratedButton btn = new DecoratedButton();
                ConnectedDeviceTemplateBtn.CloneStyleTo(btn);
                btn.Height = 50;
                btn.Text = item.Device.ProductName;
                btn.Click += SelectControllerBtn;
                Chooser choo = new Chooser();
                choo.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                choo.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                choo.Height = 20;
                choo.Width = 40;
                choo.Margin = new Thickness(0, 15, 0, 15);
                choo.IsSelected = item.Device.OutputEnable;
                choo.Selected += OpenController;
                choo.UnSelected += CloseController;
                Grid g = new Grid();
                g.Children.Add(choo);
                g.Height = 50;
                ConnectedDevices.Children.Add(btn);
                DevicesStates.Children.Add(g);
            }
        }

        private void CloseController(object sender, RoutedEventArgs e)
        {
            Chooser choo = sender as Chooser;
            TemperatureControllerInfo info = DeviceDispatcher.TryGetTemperatureDevice(DevicesStates.Children.IndexOf(choo.Parent as Grid), OperationMode.Write, true, true);
            info.Device.OutputEnable = false;
        }

        private void OpenController(object sender, RoutedEventArgs e)
        {
            Chooser choo = sender as Chooser;
            TemperatureControllerInfo info = DeviceDispatcher.TryGetTemperatureDevice(DevicesStates.Children.IndexOf(choo.Parent as Grid), OperationMode.Write, true, true);
            info.Device.OutputEnable = true;
        }

        private void SelectControllerBtn(object sender, RoutedEventArgs e)
        {
            DecoratedButton btn = sender as DecoratedButton;
            foreach (var item in ConnectedDevices.Children)
            {
                (item as DecoratedButton).KeepPressed = false;
            }
            btn.KeepPressed = true;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseConnect(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: MainWindow.Handle) == MessageBoxResult.Yes)
            {
                foreach (var item in ConnectedDevices.Children)
                {
                    if ((item as DecoratedButton).KeepPressed)
                    {
                        TemperatureControllerInfo controller = TemperatureControllers[ConnectedDevices.Children.IndexOf(item as DecoratedButton)];
                        controller.CloseDeviceInfoAndSaveParams(out bool result);
                        if (result == false) return;
                        TemperatureControllers.Remove(controller);
                        RefreshConnectedControllers();
                        RefreshChannelBtns();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearConnect(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭所有设备吗？", MessageBoxButton.YesNo, owner: MainWindow.Handle) == MessageBoxResult.Yes)
            {
                foreach (var item in ConnectedDevices.Children)
                {
                    TemperatureControllers[ConnectedDevices.Children.IndexOf(item as DecoratedButton)].CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) continue;
                    TemperatureControllers.RemoveAt(ConnectedDevices.Children.IndexOf(item as DecoratedButton));
                }
                RefreshChannelBtns();
                RefreshConnectedControllers();
            }
        }

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
                    for (int i = 0; i < TemperatureControllers.Count; ++i)
                    {
                        for (int j = 0; j < TemperatureControllers[i].SensorsInfo.Count; j++)
                        {
                            if (TemperatureControllers[i].SensorsInfo[j].IsSelected)
                            {
                                double value = TemperatureControllers[i].SensorsInfo[j].Channel.Temperature;
                                if (double.IsNaN(value)) continue;
                                TemperatureControllers[i].SensorsInfo[j].PlotLine.AddData(DateTime.Now, value);
                            }
                        }

                        for (int j = 0; j < TemperatureControllers[i].OutputsInfo.Count; j++)
                        {
                            if (TemperatureControllers[i].OutputsInfo[j].IsSelected)
                            {
                                double value = TemperatureControllers[i].OutputsInfo[j].Channel.Power;
                                if (double.IsNaN(value)) continue;
                                TemperatureControllers[i].OutputsInfo[j].PlotLine.AddData(DateTime.Now, value);
                            }
                        }
                    }
                    ChartUpdateEvent?.Invoke();
                    Thread.Sleep((int)(10 + MainWindow.Exp_TemPeraPage.SampleTime * 1000));
                }
            });

            SampleThread.Start();
        }
        #endregion
    }
}
