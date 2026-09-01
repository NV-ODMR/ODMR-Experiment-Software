using CodeHelper;
using System.Windows;

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
            hel.RegisterCloseWindow(this, null, null, CloseBtn, null, 4, 40);
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
