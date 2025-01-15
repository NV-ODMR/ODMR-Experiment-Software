using CodeHelper;
using Controls;
using DataBaseLib;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.相机_CCD_;
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

namespace ODMR_Lab.相机
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : PageBase
    {
        public List<CameraInfo> Cameras { get; set; } = new List<CameraInfo>();

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
            ConnectWindow window = new ConnectWindow(typeof(CameraBase), MainWindow.Handle);
            bool res = window.ShowDialog(out PortObject dev);
            if (res == true)
            {
                CameraInfo camera = (CameraInfo)new CameraInfo().CreateDeviceInfo(dev as CameraBase, window.ConnectInfo);
                Cameras.Add(camera);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }

        private DecoratedButton CreateCameraBar(CameraInfo device)
        {
            DecoratedButton pbtn = new DecoratedButton();
            pbtn.Height = 40;
            pbtn.Text = device.Device.ProductName;
            pbtn.Tag = device;
            ContextMenu menu = new ContextMenu();
            menu.PanelMinWidth = 200;
            menu.ItemHeight = 40;
            DecoratedButton btn = new DecoratedButton()
            {
                Text = "关闭设备"
            };
            btn.Tag = device;
            TemplateBtn.CloneStyleTo(btn);
            btn.Click += CloseDevice;
            menu.Items.Add(btn);

            btn = new DecoratedButton()
            {
                Text = "在独立窗口中打开"
            };
            btn.Tag = device;
            TemplateBtn.CloneStyleTo(btn);
            btn.Click += OpenInWindow;
            menu.Items.Add(btn);

            btn = new DecoratedButton()
            {
                Text = "参数设置"
            };
            btn.Tag = device;
            TemplateBtn.CloneStyleTo(btn);
            TemplateBtn.CloneStyleTo(pbtn);
            btn.Click += OpenParamWindow;
            menu.Items.Add(btn);

            menu.ApplyToControl(pbtn);

            return pbtn;
        }

        /// <summary>
        /// 在独立窗口中打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OpenInWindow(object sender, RoutedEventArgs e)
        {
            CameraInfo info = ((sender as DecoratedButton).Tag as CameraInfo);
            if (info.DisplayWindow != null)
            {
                info.DisplayWindow.Show();
            }
            else
            {
                CameraWindow window = new CameraWindow(info);
                info.DisplayWindow = window;
                window.Show();
            }
        }

        public void RefreshPanels()
        {
            CameraList.Children.Clear();
            foreach (var item in Cameras)
            {
                DecoratedButton btn = CreateCameraBar(item);
                btn.Click += new RoutedEventHandler((sender, e) =>
                {
                    if ((btn.Tag as CameraInfo).DisplayWindow != null)
                    {
                        (btn.Tag as CameraInfo).DisplayWindow.Topmost = true;
                        (btn.Tag as CameraInfo).DisplayWindow.Topmost = false;
                    }
                });
                CameraList.Children.Add(btn);
            }
        }

        private void OpenParamWindow(object sender, RoutedEventArgs e)
        {
            CameraInfo obj = (sender as DecoratedButton).Tag as CameraInfo;
            ParameterWindow window = new ParameterWindow(obj.Device.AvailableParameterNames());
            window.ShowDialog();
        }

        private void CloseDevice(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: MainWindow.Handle) == MessageBoxResult.Yes)
            {
                CameraInfo camera = (sender as DecoratedButton).Tag as CameraInfo;
                camera.CloseDeviceInfoAndSaveParams(out bool result);
                if (result == false) return;
                Cameras.Remove(camera);
                RefreshPanels();
            }
        }
    }
}
