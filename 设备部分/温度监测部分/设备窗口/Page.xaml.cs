using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
using ODMR_Lab.设备部分;
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
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.设备部分.温控
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "温度控制器";
        /// <summary>
        /// 温控列表
        /// </summary>
        public List<TemperatureControllerInfo> TemperatureControllers { get; set; } = new List<TemperatureControllerInfo>();

        /// <summary>
        /// 当选中数据改变时触发的事件
        /// </summary>
        public event Action<List<ChannelInfo>> DataChangedEvent = null;


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
                ParameterWindow window = new ParameterWindow((data as OutputChannelInfo).Channel, Window.GetWindow(this));
                window.ShowDialog();
            }
            if (data is SensorChannelInfo)
            {
                ParameterWindow window = new ParameterWindow((data as SensorChannelInfo).Channel, Window.GetWindow(this));
                window.ShowDialog();
            }
        }

        private void NewConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(TemperatureControllerBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                TemperatureControllerInfo info = new TemperatureControllerInfo() { Device = (TemperatureControllerBase)window.ConnectedDevice, ConnectInfo = window.ConnectInfo };
                info.CreateDeviceInfoBehaviour();
                TemperatureControllers.Add(info);
            }
            else
            {
                return;
            }
            RefreshPanels();
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

        private void RefreshChannelBtns()
        {
            Channels.Children.Clear();
            SelectedChannel.Children.Clear();
            foreach (var item in TemperatureControllers)
            {
                foreach (var channel in item.SensorsInfo)
                {
                    DecoratedButton btn = new DecoratedButton();
                    btn.Text = channel.ParentInfo.Device.ProductName + ":" + channel.Name;
                    SensorButtonTemplate.CloneStyleTo(btn);
                    btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    btn.Height = 50;
                    btn.Tag = channel;
                    CreateBtnMenu(channel).ApplyToControl(btn);
                    btn.Click += ChangeChannelPanel;

                    if (channel.IsSelected)
                    {
                        SelectedChannel.Children.Add(btn);
                    }
                    else
                    {
                        Channels.Children.Add(btn);
                    }
                }
                foreach (var channel in item.OutputsInfo)
                {
                    DecoratedButton btn = new DecoratedButton();
                    btn.Text = channel.ParentInfo.Device.ProductName + ":" + channel.Name;
                    OutputbtnTemplate.CloneStyleTo(btn);
                    btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    btn.Height = 50;
                    btn.Tag = channel;
                    CreateBtnMenu(channel).ApplyToControl(btn);
                    btn.Click += ChangeChannelPanel;

                    if (channel.IsSelected)
                    {
                        SelectedChannel.Children.Add(btn);
                    }
                    else
                    {
                        Channels.Children.Add(btn);
                    }
                }
            }
        }

        public override void RefreshPanels()
        {
            DeviceList.ClearItems();
            foreach (var item in TemperatureControllers)
            {
                DeviceList.AddItem(item, item.Device.ProductName, item.Device.OutputEnable);
            }
            RefreshChannelBtns();
        }

        private void ChangeControllerOutput(int arg1, int arg2, object arg3)
        {
            TemperatureControllerInfo inf = DeviceList.GetTag(arg1) as TemperatureControllerInfo;
            try
            {
                inf.BeginUse();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("设备正在使用", MainWindow.Handle);
                return;
            }
            inf.Device.OutputEnable = (bool)arg3;
            inf.EndUse();
        }

        private void CloseDevice(int arg1, int arg2, object arg3)
        {
            if (arg3 == null) return;
            TemperatureControllerInfo dev = arg3 as TemperatureControllerInfo;
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
            {
                dev.CloseDeviceInfoAndSaveParams(out bool result);
                if (result == false) return;
                TemperatureControllers.Remove(dev);
                RefreshPanels();
                RefreshChannelBtns();
                return;
            }
        }

        /// <summary>
        /// 清除所有连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearConnect(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭所有设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
            {
                for (int i = 0; i < TemperatureControllers.Count; i++)
                {
                    TemperatureControllers[i].CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) continue;
                    TemperatureControllers.Remove(TemperatureControllers[i]);
                    i--;
                }
                RefreshChannelBtns();
                RefreshPanels();
            }
        }

        public override void UpdateParam()
        {
        }
    }
}
