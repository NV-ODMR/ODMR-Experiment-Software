using CodeHelper;
using Controls;
using HardWares.Windows;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace ODMR_Lab.设备部分.相机_翻转镜
{
    /// <summary>
    /// CameraWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ImageProcessWindow : Window
    {
        CameraInfo cameraInfo = null;

        public ImageProcessWindow(CameraInfo info)
        {
            InitializeComponent();
            cameraInfo = info;
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterCloseWindow(this, null, null, CloseBtn, 0, 40);
            //更新参数
            SaturationSlider.Value = info.Device.Saturation;
            LightnessSlider.Value = info.Device.Lightness;
            ContrastSlider.Value = info.Device.Contrast;
        }

        private void SaturationSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            cameraInfo.Device.Saturation = SaturationSlider.Value;
        }

        private void LightnessSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            cameraInfo.Device.Lightness = LightnessSlider.Value;
        }

        private void ContrastSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            cameraInfo.Device.Contrast = ContrastSlider.Value;
        }
    }
}
