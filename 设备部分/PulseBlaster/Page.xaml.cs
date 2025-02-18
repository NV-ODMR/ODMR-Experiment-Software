using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using HardWares.APD;
using HardWares.APD.Exclitas_SPCM_AQRH;
using HardWares.Windows;
using HardWares.仪器列表.电动翻转座;
using HardWares.板卡;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.设备部分.板卡
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "板卡";

        public List<PulseBlasterInfo> PBs { get; set; } = new List<PulseBlasterInfo>();
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

        private void NewPBConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(PulseBlasterBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                PulseBlasterInfo apd = new PulseBlasterInfo() { Device = window.ConnectedDevice as PulseBlasterBase, ConnectInfo = window.ConnectInfo };
                apd.CreateDeviceInfoBehaviour();

                PBs.Add(apd);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }

        public override void RefreshPanels()
        {
            PBList.ClearItems();
            foreach (var item in PBs)
            {
                PBList.AddItem(item, item.Device.ProductName);
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
            PulseBlasterInfo inf = arg3 as PulseBlasterInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    inf.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    PBs.Remove(inf);
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

        private void ShowChannelInfos(int arg1, object arg2)
        {
            PBChannels.ClearItems();
            PulseBlasterInfo pb = arg2 as PulseBlasterInfo;
            foreach (var item in pb.ChannelDescriptions)
            {
                PBChannels.AddItem(item, item.Value, item.Key);
            }
        }

        private void ApplyChannels(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PBList.GetSelectedTag() == null) return;
                var pb = PBList.GetSelectedTag() as PulseBlasterInfo;
                //检查
                var inds = Enumerable.Range(0, PBChannels.GetRowCount()).Select(x => int.Parse(PBChannels.GetCellValue(x, 0) as string));
                var names = Enumerable.Range(0, PBChannels.GetRowCount()).Select(x => PBChannels.GetCellValue(x, 1) as string);
                HashSet<string> set = new HashSet<string>(names);
                if (set.Count != names.Count()) { throw new Exception("通道名不能重复"); }
                pb.ChannelDescriptions.Clear();
                pb.ChannelDescriptions = pb.Device.ChannelInds.Select(x => new KeyValuePair<string, int>(x.ToString(), x)).ToList();
                for (int i = 0; i < inds.Count(); i++)
                {
                    int ind = pb.ChannelDescriptions.FindIndex(x => x.Value == inds.ElementAt(i));
                    if (ind != -1)
                    {
                        pb.ChannelDescriptions[ind] = new KeyValuePair<string, int>(names.ElementAt(i), inds.ElementAt(i));
                    }
                }
                MessageWindow.ShowTipWindow("设置成功", Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("设置未完成," + ex.Message, Window.GetWindow(this));
            }
        }
    }
}
