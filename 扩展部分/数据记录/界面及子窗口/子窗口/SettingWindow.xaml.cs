using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.相机_翻转镜;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            hel.RegisterWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
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
