using Controls.Windows;
using HardWares.Windows;
using HardWares.相机_CCD_;
using System.Collections.Generic;
using System.Windows;

namespace ODMR_Lab.设备部分.相机_翻转镜
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DevicePage : DevicePageBase
    {
        public override string PageName { get; set; } = "相机";

        public List<CameraInfo> Cameras { get; set; } = new List<CameraInfo>();

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

        public override void RefreshPanels()
        {
            CameraList.ClearItems();
            foreach (var item in Cameras)
            {
                CameraList.AddItem(item, item.Device.ProductName);
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

        public override void UpdateParam()
        {
        }
    }
}
