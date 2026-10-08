using Controls;
using Controls.Windows;
using HardWares;
using HardWares.Lock_In;
using HardWares.Windows;
using HardWares.仪器列表.电动翻转座;
using HardWares.射频源;
using HardWares.板卡;
using HardWares.温度控制器;
using HardWares.源表;
using HardWares.电源;
using HardWares.端口基类;
using HardWares.继电器模块;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ODMR_Lab.设备部分.其他设备
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "其他设备";

        /// <summary>
        /// 设备类型列表
        /// </summary>
        public List<KeyValuePair<DeviceTypes, object>> DeviceTypeList { get; set; } = new List<KeyValuePair<DeviceTypes, object>>()
        {
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.温控 , new List<TemperatureControllerInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.翻转镜 , new List<FlipMotorInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.开关 , new List<SwitchInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.锁相放大器 , new List<LockinInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.电源 , new List<PowerInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.信号发生器通道 , new List<SignalGeneratorInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.PulseBlaster , new List<PulseBlasterInfo>()),
            new KeyValuePair<DeviceTypes, object>(DeviceTypes.源表 , new List<PowerMeterInfo>()),
        };

        public DevicePage()
        {
            InitializeComponent();
            //添加所包含的设备
            var types = DeviceTypeList.Select((x) => x.Key);
            foreach (var item in types)
            {
                DecoratedButton box = new DecoratedButton() { Text = Enum.GetName(typeof(DeviceTypes), item) };
                box.Tag = item;
                box.Width = 200;
                box.Height = 50;
                UIUpdater.SetDefaultTemplate(box);
                DeviceTypeBox.Items.Add(box);
            }
        }

        public dynamic ConvertInfoListType(object infoobj, DeviceTypes type)
        {
            if (type == DeviceTypes.温控) return infoobj as List<TemperatureControllerInfo>;
            if (type == DeviceTypes.翻转镜) return infoobj as List<FlipMotorInfo>;
            if (type == DeviceTypes.开关) return infoobj as List<SwitchInfo>;
            if (type == DeviceTypes.锁相放大器) return infoobj as List<LockinInfo>;
            if (type == DeviceTypes.电源) return infoobj as List<PowerInfo>;
            if (type == DeviceTypes.信号发生器通道) return infoobj as List<SignalGeneratorInfo>;
            if (type == DeviceTypes.PulseBlaster) return infoobj as List<PulseBlasterInfo>;
            if (type == DeviceTypes.源表) return infoobj as List<PowerMeterInfo>;
            return null;
        }

        public Type GetDeviceBaseType(DeviceTypes type)
        {
            if (type == DeviceTypes.温控) return typeof(TemperatureControllerBase);
            if (type == DeviceTypes.翻转镜) return typeof(FlipMotorBase);
            if (type == DeviceTypes.开关) return typeof(SwitchBase);
            if (type == DeviceTypes.锁相放大器) return typeof(LockInBase);
            if (type == DeviceTypes.电源) return typeof(PowerBase);
            if (type == DeviceTypes.信号发生器通道) return typeof(SignalGeneratorBase);
            if (type == DeviceTypes.PulseBlaster) return typeof(PulseBlasterBase);
            if (type == DeviceTypes.源表) return typeof(PowerSourceBase);
            return null;
        }

        public dynamic GetDeviceChannels(DeviceTypes type, object device)
        {
            if (type == DeviceTypes.温控)
            {
                List<TemperatureChannelInfo> result = new List<TemperatureChannelInfo>();
                result.AddRange((device as TemperatureControllerInfo).ChannelsInfo);
                return result;
            }
            if (type == DeviceTypes.翻转镜) return null;
            if (type == DeviceTypes.开关) return null;
            if (type == DeviceTypes.锁相放大器) return null;
            if (type == DeviceTypes.电源)
            {
                List<PowerChannelInfo> result = new List<PowerChannelInfo>();
                result.AddRange((device as PowerInfo).ChannelsInfo);
                return result;
            }
            if (type == DeviceTypes.信号发生器通道)
            {
                List<SignalGeneratorChannelInfo> result = new List<SignalGeneratorChannelInfo>();
                result.AddRange((device as SignalGeneratorInfo).Channels);
                return result;
            }
            if (type == DeviceTypes.PulseBlaster) return null;
            if (type == DeviceTypes.源表) return null;
            return null;
        }

        public dynamic ConvertInfoType(object infoobj, DeviceTypes type)
        {
            if (type == DeviceTypes.温控) return infoobj as TemperatureControllerInfo;
            if (type == DeviceTypes.翻转镜) return infoobj as FlipMotorInfo;
            if (type == DeviceTypes.开关) return infoobj as SwitchInfo;
            if (type == DeviceTypes.锁相放大器) return infoobj as LockinInfo;
            if (type == DeviceTypes.电源) return infoobj as PowerInfo;
            if (type == DeviceTypes.信号发生器通道) return infoobj as SignalGeneratorInfo;
            if (type == DeviceTypes.PulseBlaster) return infoobj as PulseBlasterInfo;
            if (type == DeviceTypes.源表) return infoobj as PowerMeterInfo;
            return null;
        }

        public dynamic CreateInfo(PortObject device, HardWares.端口基类部分.设备信息.DeviceInfoBase info, DeviceTypes type)
        {
            if (type == DeviceTypes.温控)
            {
                var result = new TemperatureControllerInfo() { Device = (TemperatureControllerBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.翻转镜)
            {
                var result = new FlipMotorInfo() { Device = (FlipMotorBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.开关)
            {
                var result = new SwitchInfo() { Device = (SwitchBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.锁相放大器)
            {
                var result = new LockinInfo() { Device = (LockInBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.电源)
            {
                var result = new PowerInfo() { Device = (PowerBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.信号发生器通道)
            {
                var result = new SignalGeneratorInfo() { Device = (SignalGeneratorBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.PulseBlaster)
            {
                var result = new PulseBlasterInfo() { Device = (PulseBlasterBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            if (type == DeviceTypes.源表)
            {
                var result = new PowerMeterInfo() { Device = (PowerSourceBase)device, ConnectInfo = info };
                result.CreateDeviceInfoBehaviour();
                return result;
            }
            return null;
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        private void OpenChannelWindow(object sender, RoutedEventArgs e)
        {
        }

        private void NewConnect(object sender, RoutedEventArgs e)
        {
            if (DeviceTypeBox.SelectedItem == null) return;
            DeviceTypes type = (DeviceTypes)Enum.Parse(typeof(DeviceTypes), DeviceTypeBox.SelectedItem.Text);
            ConnectWindow window = new ConnectWindow(GetDeviceBaseType(type));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                // ★ 设备清单（仅作已连接记录与自动连接依据，不作连接准入）：清单外设备一律放行入册（不阻断连接，仅记日志告警）
                string catreason;
                if (!DeviceCatalog.EnsureInCatalog(window.ConnectedDevice as PortObject, out catreason))
                {
                    try { (window.ConnectedDevice as PortObject)?.Dispose(); } catch (Exception) { }
                    MessageLogger.LogError("手动连接被拒绝（清单外设备）：" + catreason, "DeviceCatalog");
                    MessageWindow.ShowTipWindow(catreason, Window.GetWindow(this));
                    return;
                }
                // ★ 宿主总闸（HostClient.Enabled 默认 false，此时无影响）
                if (!HostClient.AllowLocalConnect(window.ConnectedDevice == null ? "" : window.ConnectedDevice.ProductName, out catreason))
                {
                    try { (window.ConnectedDevice as PortObject)?.Dispose(); } catch (Exception) { }
                    MessageWindow.ShowTipWindow(catreason, Window.GetWindow(this));
                    return;
                }
                var info = CreateInfo(window.ConnectedDevice, window.ConnectInfo, type);
                ConvertInfoListType(DeviceTypeList.Where((x) => x.Key == type).ElementAt(0).Value, type).Add(info);
                // ★ 路径 B′：事件驱动上报只读设备镜像（非轮询；DeviceMirror.Enabled 默认 false 时为空操作）
                DeviceMirror.Publish();
            }
            else
            {
                return;
            }
            RefreshPanels();
        }

        private void CloseDevice(int arg1, int arg2, object arg3)
        {
            if (arg3 == null) return;
            var re = (KeyValuePair<DeviceTypes, InfoBase>)arg3;
            var dev = ConvertInfoType(re.Value, re.Key);
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    dev.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    ConvertInfoListType(DeviceTypeList.Where((x) => x.Key == re.Key).ElementAt(0).Value, re.Key).Remove(dev);
                    RefreshPanels();
                    return;
                }
            }
            if (arg1 == 1)
            {
                // ★ 设备清单（仅作已连接记录与自动连接依据，不作连接准入）：清单外设备同样允许打开参数编辑窗口
                string catreason;
                if (!DeviceCatalog.EnsureInCatalog(re.Value, out catreason))
                {
                    MessageLogger.LogError("参数编辑窗口被拒绝（清单外设备）：" + catreason, "DeviceCatalog");
                    MessageWindow.ShowTipWindow(catreason, Window.GetWindow(this));
                    return;
                }
                ParameterWindow window = new ParameterWindow(re.Value.SourceDevice as PortObject, Window.GetWindow(this));
                window.ShowDialog();
            }
        }

        public override void RefreshPanels()
        {
            try
            {
                DeviceList.ClearItems();
                DeviceChannelList.ClearItems();
                if (DeviceTypeBox.SelectedItem == null) return;
                DeviceTypes deviceTypes = (DeviceTypes)(DeviceTypeBox.SelectedItem).Tag;
                var infos = ConvertInfoListType(DeviceTypeList.Where((x) => x.Key == deviceTypes).ElementAt(0).Value, deviceTypes);
                foreach (var item in infos)
                {
                    DeviceList.AddItem(new KeyValuePair<DeviceTypes, InfoBase>(deviceTypes, item), item.Device.ProductName);
                }
            }
            catch (Exception)
            {
            }
        }


        public override void UpdateParam()
        {
        }

        private void DeviceList_ItemSelected(int arg1, object arg2)
        {
            if (DeviceTypeBox.SelectedItem == null) return;
            DeviceTypes type = (DeviceTypes)Enum.Parse(typeof(DeviceTypes), DeviceTypeBox.SelectedItem.Text);
            DeviceChannelList.ClearItems();
            var dev = ConvertInfoType(((KeyValuePair<DeviceTypes, InfoBase>)arg2).Value, type);
            var chs = GetDeviceChannels(type, dev);
            if (chs == null) return;
            //获取通道列表
            foreach (var item in chs)
            {
                DeviceChannelList.AddItem(new KeyValuePair<DeviceTypes, InfoBase>(type, item), item.GetChannelDescription());
            }
        }

        private void ChannelContextMenuEvent(int arg1, int arg2, object arg3)
        {
            if (arg3 == null) return;
            var re = (KeyValuePair<DeviceTypes, InfoBase>)arg3;
            var dev = ConvertInfoType(re.Value, re.Key);
            if (arg1 == 0)
            {
                // ★ 设备清单（仅作已连接记录与自动连接依据，不作连接准入）：清单外设备同样允许打开参数编辑窗口
                string catreason;
                if (!DeviceCatalog.EnsureInCatalog(re.Value, out catreason))
                {
                    MessageLogger.LogError("参数编辑窗口被拒绝（清单外设备）：" + catreason, "DeviceCatalog");
                    MessageWindow.ShowTipWindow(catreason, Window.GetWindow(this));
                    return;
                }
                ParameterWindow window = new ParameterWindow(re.Value.SourceDevice as PortElement, Window.GetWindow(this));
                window.ShowDialog();
            }
        }

        private void DeviceTypeBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            RefreshPanels();
        }
    }
}
