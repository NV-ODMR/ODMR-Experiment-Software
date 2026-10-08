using Controls;
using Controls.Windows;
using HardWares.APD;
using HardWares.Windows;
using ODMR_Lab.设备部分.其他设备;
using System.Collections.Generic;
using System.Windows;
using System;
using HardWares.端口基类;

namespace ODMR_Lab.设备部分.光子探测器
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "光子计数器";

        public List<APDInfo> APDs { get; set; } = new List<APDInfo>();
        public List<FlipMotorInfo> Flips { get; set; } = new List<FlipMotorInfo>();


        public DevicePage()
        {
            InitializeComponent();
            TraceSource.TemplateButton = UIUpdater.ButtonTemplate;
            PulseSource.TemplateButton = UIUpdater.ButtonTemplate;
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

        private void NewAPDConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(APDBase));
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
                APDInfo apd = new APDInfo() { Device = window.ConnectedDevice as APDBase, ConnectInfo = window.ConnectInfo };
                apd.CreateDeviceInfoBehaviour();

                APDs.Add(apd);
                RefreshPanels();
                // ★ 路径 B′：事件驱动上报只读设备镜像（非轮询；DeviceMirror.Enabled 默认 false 时为空操作）
                DeviceMirror.Publish();
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
                // ★ 设备清单（仅作已连接记录与自动连接依据，不作连接准入）：清单外设备同样允许打开参数编辑窗口
                string catreason;
                if (!DeviceCatalog.EnsureInCatalog(inf, out catreason))
                {
                    MessageLogger.LogError("参数编辑窗口被拒绝（清单外设备）：" + catreason, "DeviceCatalog");
                    MessageWindow.ShowTipWindow(catreason, Window.GetWindow(this));
                    return;
                }
                ParameterWindow window = new ParameterWindow(inf.Device, Window.GetWindow(this));
                window.ShowDialog();
            }
            #endregion
        }

        private void APDList_ItemSelected(int arg1, object arg2)
        {
            UpdateSourceState();
        }

        public void UpdateSourceState()
        {
            if (APDList.GetSelectedTag() == null) return;
            UpdateTraceList();
            UpdatePulseList();
            TraceSource.Select((APDList.GetSelectedTag() as APDInfo).TraceSourceName);
            PulseSource.Select((APDList.GetSelectedTag() as APDInfo).PulseSourceName);
            if ((APDList.GetSelectedTag() as APDInfo).IsWriting)
            {
                TraceSource.IsEnabled = false;
                PulseSource.IsEnabled = false;
            }
            else
            {
                TraceSource.IsEnabled = true;
                PulseSource.IsEnabled = true;
            }
        }

        private void UpdateTraceList()
        {
            TraceSource.Items.Clear();
            List<InfoBase> pbs = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster);
            foreach (var item in pbs)
            {
                TraceSource.Items.Add(new DecoratedButton() { Text = item.GetDeviceDescription(), Tag = item });
            }
        }

        private void UpdatePulseList()
        {
            PulseSource.Items.Clear();
            List<InfoBase> pbs = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster);
            foreach (var item in pbs)
            {
                PulseSource.Items.Add(new DecoratedButton() { Text = item.GetDeviceDescription() });
            }
        }

        private void TraceSource_Click(object sender, RoutedEventArgs e)
        {
            UpdateTraceList();
        }

        private void PulseSource_Click(object sender, RoutedEventArgs e)
        {
            UpdatePulseList();
        }


        private void TraceSource_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (APDList.GetSelectedTag() == null) return;
            if (TraceSource.SelectedItem == null)
            {
                (APDList.GetSelectedTag() as APDInfo).TraceSourceName = "";
                return;
            }
            (APDList.GetSelectedTag() as APDInfo).TraceSourceName = TraceSource.SelectedItem.Text;
        }

        private void PulseSource_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (APDList.GetSelectedTag() == null) return;
            if (PulseSource.SelectedItem == null)
            {
                (APDList.GetSelectedTag() as APDInfo).PulseSourceName = "";
                return;
            }
            (APDList.GetSelectedTag() as APDInfo).PulseSourceName = PulseSource.SelectedItem.Text; ;
        }
    }
}
