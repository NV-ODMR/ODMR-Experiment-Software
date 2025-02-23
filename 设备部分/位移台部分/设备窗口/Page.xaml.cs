using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.设备部分.位移台部分
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {

        public List<NanoMoverInfo> MoverList { get; set; } = new List<NanoMoverInfo>();
        public override string PageName { get; set; } = "位移台";

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

        private void NewConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(NanoControllerBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                DecoratedButton btn = sender as DecoratedButton;

                NanoMoverInfo controller = new NanoMoverInfo() { Device = window.ConnectedDevice as NanoControllerBase, ConnectInfo = window.ConnectInfo };
                controller.CreateDeviceInfoBehaviour();
                MoverList.Add(controller);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }


        public override void RefreshPanels()
        {
            ProbeMoverList.ClearItems();
            SampleMoverList.ClearItems();
            MWMoverList.ClearItems();
            MagnetMoverList.ClearItems();
            LenMoverList.ClearItems();
            DeviceList.ClearItems();
            ScannerMoverList.ClearItems();

            foreach (var item in MoverList)
            {
                foreach (var stage in item.Stages)
                {
                    if (stage.PartType == PartTypes.Probe)
                    {
                        ProbeMoverList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                    if (stage.PartType == PartTypes.Sample)
                    {
                        SampleMoverList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                    if (stage.PartType == PartTypes.Magnnet)
                    {
                        MagnetMoverList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                    if (stage.PartType == PartTypes.Microwave)
                    {
                        MWMoverList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                    if (stage.PartType == PartTypes.Len)
                    {
                        LenMoverList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                    if (stage.PartType == PartTypes.Scanner)
                    {
                        ScannerMoverList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                    if (stage.PartType == PartTypes.None)
                    {
                        DeviceList.AddItem(stage, stage.Parent.Device.ProductName, stage.Device.AxisName);
                    }
                }
            }
        }


        private void OpenLabelWindow(object sender, RoutedEventArgs e)
        {
            DecoratedButton btn = sender as DecoratedButton;
            try
            {
                MoverTestWindow window = null;
                if (btn.Name == "MagnetLabelBtn")
                {
                    window = new MoverTestWindow(PartTypes.Magnnet, "磁铁位移台设置窗口");
                }
                if (btn.Name == "PrbeLabelBtn")
                {
                    window = new MoverTestWindow(PartTypes.Probe, "探针位移台设置窗口");
                }
                if (btn.Name == "SampleLabelBtn")
                {
                    window = new MoverTestWindow(PartTypes.Sample, "样品位移台设置窗口");
                }
                if (btn.Name == "MicrowaveLabelBtn")
                {
                    window = new MoverTestWindow(PartTypes.Microwave, "微波线位移台设置窗口");
                }
                if (btn.Name == "LenLabelBtn")
                {
                    window = new MoverTestWindow(PartTypes.Len, "镜头位移台设置窗口");
                }
                if (btn.Name == "ScannerLabelBtn")
                {
                    window = new MoverTestWindow(PartTypes.Scanner, "扫描台设置窗口");
                }
                window?.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("设备被占用", Window.GetWindow(this));
                return;
            }
        }

        #region 设备列表
        private void ContextMenuEvent(int arg1, int arg2, object arg3)
        {
            NanoStageInfo info = arg3 as NanoStageInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    info.Parent.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    MoverList.Remove(info.Parent);
                    RefreshPanels();
                }
            }
            #endregion
            #region 参数设置
            if (arg1 == 1)
            {
                ParameterWindow window = new ParameterWindow(info.Device);
                window.ShowDialog();
            }
            #endregion
            #region 设置为探针位移台或者设置为空位移台
            if (arg1 == 2)
            {
                var dev = (arg3 as NanoStageInfo);
                if (dev.PartType == PartTypes.None)
                {
                    (arg3 as NanoStageInfo).PartType = PartTypes.Probe;
                    RefreshPanels();
                    return;
                }
                if (dev.PartType != PartTypes.None)
                {
                    (arg3 as NanoStageInfo).PartType = PartTypes.None;
                    RefreshPanels();
                    return;
                }
            }
            #endregion
            #region 设置为磁铁位移台
            if (arg1 == 3)
            {
                (arg3 as NanoStageInfo).PartType = PartTypes.Magnnet;
                RefreshPanels();
            }
            #endregion
            #region 设置为样品位移台
            if (arg1 == 4)
            {
                (arg3 as NanoStageInfo).PartType = PartTypes.Sample;
                RefreshPanels();
            }
            #endregion
            #region 设置为微波位移台
            if (arg1 == 5)
            {
                (arg3 as NanoStageInfo).PartType = PartTypes.Microwave;
                RefreshPanels();
            }
            #endregion
            #region 设置为镜头位移台
            if (arg1 == 6)
            {
                (arg3 as NanoStageInfo).PartType = PartTypes.Len;
                RefreshPanels();
            }
            #endregion
            #region 设置为扫描台
            if (arg1 == 7)
            {
                (arg3 as NanoStageInfo).PartType = PartTypes.Scanner;
                RefreshPanels();
            }
            #endregion
        }
        #endregion
    }
}
