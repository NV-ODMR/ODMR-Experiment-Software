using CodeHelper;
using Controls;
using Controls.Windows;
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
using HardWares.继电器模块;
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

namespace ODMR_Lab.设备部分.相机_翻转镜
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "相机";

        public List<CameraInfo> Cameras { get; set; } = new List<CameraInfo>();
        public List<FlipMotorInfo> Flips { get; set; } = new List<FlipMotorInfo>();

        public List<SwitchInfo> Switches { get; set; } = new List<SwitchInfo>();


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

        private void NewCameraConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(CameraBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                CameraInfo camera = new CameraInfo() { Device = window.ConnectedDevice as CameraBase, ConnectInfo = window.ConnectInfo };
                camera.CreateDeviceInfoBehaviour();

                Cameras.Add(camera);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }
        private void NewFlipConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(FlipMotorBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                FlipMotorInfo flips = new FlipMotorInfo() { Device = window.ConnectedDevice as FlipMotorBase, ConnectInfo = window.ConnectInfo };
                flips.CreateDeviceInfoBehaviour();

                Flips.Add(flips);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }

        private void NewSwitchConnect(object sender, RoutedEventArgs e)
        {
            ConnectWindow window = new ConnectWindow(typeof(SwitchBase));
            bool res = window.ShowDialog(Window.GetWindow(this));
            if (res == true)
            {
                SwitchInfo sw = new SwitchInfo() { Device = window.ConnectedDevice as SwitchBase, ConnectInfo = window.ConnectInfo };
                sw.CreateDeviceInfoBehaviour();

                Switches.Add(sw);
                RefreshPanels();
            }
            else
            {
                return;
            }
        }

        public override void RefreshPanels()
        {
            CameraList.ClearItems();
            foreach (var item in Cameras)
            {
                CameraList.AddItem(item, item.Device.ProductName);
            }

            FlipList.ClearItems();
            foreach (var item in Flips)
            {
                FlipList.AddItem(item, item.Device.ProductName, item.Device.Switch);
            }

            SwitchList.ClearItems();
            foreach (var item in Switches)
            {
                SwitchList.AddItem(item, item.Device.ProductName, item.Device.IsOpen);
            }
        }

        /// <summary>
        /// 显示摄像头窗口
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void ShowCameraWindow(int arg1, object arg2)
        {
            CameraInfo info = arg2 as CameraInfo;
            if (info.DisplayWindow != null)
            {
                info.DisplayWindow.Topmost = true;
                info.DisplayWindow.Topmost = false;
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
            CameraInfo inf = arg3 as CameraInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    inf.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    Cameras.Remove(inf);
                    RefreshPanels();
                }
            }
            #endregion

            #region 在独立窗口中显示
            if (arg1 == 1)
            {
                if (inf.DisplayWindow != null)
                {
                    inf.DisplayWindow.Show();
                }
                else
                {
                    CameraWindow window = new CameraWindow(this, inf);
                    inf.DisplayWindow = window;
                    window.Show();
                }
            }
            #endregion

            #region 参数设置
            if (arg1 == 2)
            {
                ParameterWindow window = new ParameterWindow(inf.Device, Window.GetWindow(this));
                window.ShowDialog();
            }
            #endregion
        }

        /// <summary>
        /// 翻转镜修改值
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void FlipList_ItemValueChanged(int arg1, int arg2, object arg3)
        {
            Task t = new Task(() =>
            {
                try
                {
                    FlipMotorInfo info = null;
                    Dispatcher.Invoke(() =>
                    {
                        FlipList.IsEnabled = false;
                        info = FlipList.GetTag(arg1) as FlipMotorInfo;
                    });
                    info.Device.Switch = (bool)arg3;
                }
                catch (Exception) { }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        FlipList.IsEnabled = true;
                    });
                }
            });
            t.Start();
        }


        private void FlipContextMenuEvent(int arg1, int arg2, object arg3)
        {
            FlipMotorInfo inf = arg3 as FlipMotorInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    inf.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    Flips.Remove(inf);
                    RefreshPanels();
                }
            }
            #endregion
        }

        /// <summary>
        /// 开关修改值
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void SwitchList_ItemValueChanged(int arg1, int arg2, object arg3)
        {
            Task t = new Task(() =>
            {
                try
                {
                    SwitchInfo info = null;
                    Dispatcher.Invoke(() =>
                    {
                        SwitchList.IsEnabled = false;
                        info = SwitchList.GetTag(arg1) as SwitchInfo;
                    });
                    info.Device.IsOpen = (bool)arg3;
                }
                catch (Exception) { }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        SwitchList.IsEnabled = true;
                    });
                }
            });
            t.Start();
        }


        private void SwitchContextMenuEvent(int arg1, int arg2, object arg3)
        {
            SwitchInfo inf = arg3 as SwitchInfo;
            #region 关闭设备
            if (arg1 == 0)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要关闭此设备吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    inf.CloseDeviceInfoAndSaveParams(out bool result);
                    if (result == false) return;
                    Switches.Remove(inf);
                    RefreshPanels();
                }
            }
            #endregion
        }

        public override void UpdateParam()
        {
        }
    }
}
