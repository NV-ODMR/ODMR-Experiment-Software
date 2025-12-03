using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.源表;
using HardWares.电源;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.温控;
using ODMR_Lab.设备部分.源表;
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

namespace ODMR_Lab.设备部分.电源
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "电源";

        public List<PowerInfo> PowerList { get; set; } = new List<PowerInfo>();

        /// <summary>
        /// 当前选中的源表
        /// </summary>
        public PowerInfo CurrentSelectedMeter { get; set; } = null;

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
        }

        #region 设备部分
        private void NewConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(PowerBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                PowerInfo power = new PowerInfo() { Device = window.ConnectedDevice as PowerBase, ConnectInfo = window.ConnectInfo };
                power.CreateDeviceInfoBehaviour();

                PowerList.Add(power);

                RefreshPanels();
            }
            else
            {
                return;
            }
        }

        public override void RefreshPanels()
        {
            DeviceList.ClearItems();
            DeviceChannelList.ClearItems();

            foreach (var item in PowerList)
            {
                DeviceList.AddItem(item, item.Device.ProductName);
            }
        }

        private void PowerSelected(int arg1, object arg2)
        {
            CurrentSelectedMeter = arg2 as PowerInfo;
            //显示通道
            DeviceChannelList.ClearItems();
            foreach (var item in CurrentSelectedMeter.ChannelsInfo)
            {
                DeviceChannelList.AddItem(item, item.Name);
            }
        }

        private void ContextMenuEvent(int arg1, int arg2, object arg3)
        {
            PowerInfo info = arg3 as PowerInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    info.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false)
                    {
                        return;
                    }
                    PowerList.Remove(info);
                    RefreshPanels();
                }
            }
            #endregion
            #region 参数设置
            if (arg1 == 1)
            {
                ParameterWindow window = new ParameterWindow(info.Device, Window.GetWindow(this));
                window.ShowDialog();
            }
            #endregion
        }

        private void ChannelContextMenuEvent(int arg1, int arg2, object arg3)
        {
            PowerMeterInfo info = arg3 as PowerMeterInfo;
            #region 参数设置
            if (arg1 == 0)
            {
                ParameterWindow window = new ParameterWindow(info.Device, Window.GetWindow(this));
                window.ShowDialog();
            }
            #endregion
        }

        #endregion
    }
}
