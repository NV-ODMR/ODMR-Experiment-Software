using CodeHelper;
using Controls;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.相机_翻转镜;
using System.Windows;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public CameraInfo SelectedCamera = null;

        public SettingWindow(string wintitle)
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, null, 5, 40);
            Title = wintitle;
            title.Content = "     " + wintitle;
        }

        public void Load(CameraInfo info)
        {
            SelectedCamera = info;
            if (info != null)
            {
                Cameras.Select(info.GetDeviceDescription());
            }
        }

        private void Cameras_Click(object sender, RoutedEventArgs e)
        {
            Cameras.Items.Clear();
            //刷新相机设备
            foreach (var c in DeviceDispatcher.GetDevice(DeviceTypes.相机))
            {
                if (!c.IsWriting)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = c.GetDeviceDescription() };
                    btn.Tag = c;
                    Cameras.Items.Add(btn);
                }
            }
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            SelectedCamera = Cameras.SelectedItem.Tag as CameraInfo;
            Hide();
        }
    }
}
