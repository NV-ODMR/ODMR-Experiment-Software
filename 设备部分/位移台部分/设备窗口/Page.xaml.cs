using CodeHelper;
using Controls;
using DataBaseLib;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 温度监控程序.Windows;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.位移台部分
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : PageBase
    {

        public List<NanoMoverInfo> MoverList { get; set; } = new List<NanoMoverInfo>();


        public DevicePage()
        {
            InitializeComponent();
        }

        public override void Init()
        {
        }

        public override void CloseBehaviour()
        {
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

        private void NewConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(NanoControllerBase), MainWindow.Handle);
            bool res = window.ShowDialog(out PortObject dev);
            if (res == true)
            {
                DecoratedButton btn = sender as DecoratedButton;

                NanoMoverInfo controller = (NanoMoverInfo)new NanoMoverInfo().CreateDeviceInfo(dev as NanoControllerBase, window.ConnectInfo);
                if (btn.Name == "MagnetBtn")
                {
                    foreach (var item in controller.Stages)
                    {
                        item.PartType = PartTypes.Magnnet;
                    }
                }
                if (btn.Name == "SampleBtn")
                {
                    foreach (var item in controller.Stages)
                    {
                        item.PartType = PartTypes.Sample;
                    }
                }
                if (btn.Name == "MicrowaveBtn")
                {
                    foreach (var item in controller.Stages)
                    {
                        item.PartType = PartTypes.Microwave;
                    }
                }
                if (btn.Name == "ProbeBtn")
                {
                    foreach (var item in controller.Stages)
                    {
                        item.PartType = PartTypes.Probe;
                    }
                }
                MoverList.Add(controller);
                RefreshPanels();
            }
            else
            {
                return;
            }
            NanoControllerBase tem = dev as NanoControllerBase;
        }

        private Grid CreateMoverBar(NanoStageInfo device, string axisname, string controllername)
        {
            Grid grid = new Grid();
            grid.Height = 40;
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            FontChangeText lbl = new FontChangeText();
            lbl.InnerTextBox.IsReadOnly = true;
            lbl.InnerTextBox.Text = controllername;
            TextTemplate.CloneStyleTo(lbl);
            grid.Children.Add(lbl);
            Grid.SetColumn(lbl, 0);

            FontChangeText lb2 = new FontChangeText();
            lb2.InnerTextBox.IsReadOnly = true;
            lb2.InnerTextBox.Text = axisname;
            TextTemplate.CloneStyleTo(lb2);
            grid.Children.Add(lb2);
            Grid.SetColumn(lb2, 1);

            ContextMenu menu = new ContextMenu();
            menu.PanelMinWidth = 200;
            menu.ItemHeight = 40;
            DecoratedButton btn = new DecoratedButton()
            {
                Text = "关闭设备"
            };
            btn.Tag = device;
            MagnetBtn.CloneStyleTo(btn);
            btn.Click += CloseDevice;
            menu.Items.Add(btn);

            btn = new DecoratedButton()
            {
                Text = "参数设置"
            };
            btn.Tag = device;
            MagnetBtn.CloneStyleTo(btn);
            btn.Click += OpenParamWindow;
            menu.Items.Add(btn);

            menu.ApplyToControl(grid);

            return grid;
        }

        public void RefreshPanels()
        {
            ProbeMovers.Children.Clear();
            MagnetMovers.Children.Clear();
            SampleMovers.Children.Clear();
            MocrowaveMovers.Children.Clear();

            foreach (var item in MoverList)
            {
                foreach (var stage in item.Stages)
                {
                    Grid g = CreateMoverBar(stage, stage.Device.AxisName, item.Device.ProductName);
                    if (stage.PartType == PartTypes.Probe)
                    {
                        ProbeMovers.Children.Add(g);
                    }
                    if (stage.PartType == PartTypes.Sample)
                    {
                        SampleMovers.Children.Add(g);
                    }
                    if (stage.PartType == PartTypes.Magnnet)
                    {
                        MagnetMovers.Children.Add(g);
                    }
                    if (stage.PartType == PartTypes.Microwave)
                    {
                        MocrowaveMovers.Children.Add(g);
                    }
                }
            }
        }

        private void OpenParamWindow(object sender, RoutedEventArgs e)
        {
            NanoStageInfo obj = (sender as DecoratedButton).Tag as NanoStageInfo;
            ParameterWindow window = new ParameterWindow(obj.Device.AvailableParameterNames());
            window.ShowDialog();
        }

        private void CloseDevice(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: MainWindow.Handle) == MessageBoxResult.Yes)
            {
                NanoMoverInfo mover = ((sender as DecoratedButton).Tag as NanoStageInfo).Parent;
                mover.CloseDeviceInfoAndSaveParams(out bool result);
                if (result == false) return;
                MoverList.Remove(mover);
                RefreshPanels();
            }
        }


        private void OpenLabelWindow(object sender, RoutedEventArgs e)
        {
            DecoratedButton btn = sender as DecoratedButton;
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

            window?.ShowDialog();
            return;
        }
    }
}
