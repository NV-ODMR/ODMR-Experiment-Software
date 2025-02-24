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
using ODMR_Lab.设备部分.板卡;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = Controls.ContextMenu;

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

        private void APDList_ItemSelected(int arg1, object arg2)
        {
            UpdateTraceAndPulseList();
            TraceSource.Select(TraceSource.Items.FindIndex(x => x.Tag == (APDList.GetSelectedTag() as APDInfo).TraceSource));
            PulseSource.Select(PulseSource.Items.FindIndex(x => x.Tag == (APDList.GetSelectedTag() as APDInfo).PulseSource));
        }

        private void UpdateTraceAndPulseList()
        {
            TraceSource.Items.Clear();
            PulseSource.Items.Clear();
            List<InfoBase> pbs = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster);
            foreach (var item in pbs)
            {
                TraceSource.Items.Add(new DecoratedButton() { Text = item.GetDeviceDescription(), Tag = item });
                PulseSource.Items.Add(new DecoratedButton() { Text = item.GetDeviceDescription(), Tag = item });
            }
        }
    }
}
