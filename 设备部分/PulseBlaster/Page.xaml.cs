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

        public List<PulseBlasterInfo> APDs { get; set; } = new List<PulseBlasterInfo>();
        public List<FlipMotorInfo> Flips { get; set; } = new List<FlipMotorInfo>();


        public DevicePage()
        {
            InitializeComponent();
            Chart.DataList.Add(APDDisplayData);
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

        private void NewPulseBlasterConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(PulseBlasterBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                PulseBlasterInfo apd = new PulseBlasterInfo() { Device = window.ConnectedDevice as PulseBlasterBase, ConnectInfo = window.ConnectInfo };
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
            PulseBlasterInfo inf = arg3 as PulseBlasterInfo;
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

        private void Snap(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(Chart));
            TimeWindow window = new TimeWindow();
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            string save = "时间(s)\t" + "计数(cps)\n";
            List<double> res = APDSampleData.ToArray().ToList();
            double freq = ConfigParam.SampleFreq.Value;
            for (int i = 0; i < res.Count; i++)
            {
                save += (i / freq).ToString() + "\t" + res[i].ToString() + "\n";
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "文本文件 (*.txt)|*.txt";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter wr = new StreamWriter(new FileStream(dlg.FileName, FileMode.Create)))
                    {
                        wr.Write(save);
                    }
                    TimeWindow win = new TimeWindow();
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("文件未成功保存，原因：" + ex.Message, Window.GetWindow(this));
                }
            }
        }
    }
}
