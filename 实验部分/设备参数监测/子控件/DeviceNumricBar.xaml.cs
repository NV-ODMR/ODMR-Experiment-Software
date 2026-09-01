using System.Windows.Controls;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.设备参数监测
{
    /// <summary>
    /// DeviceListenerBar.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceNumricBar : Grid
    {
        DeviceListenInfo ParentInfo = null;

        public DeviceNumricBar(DeviceListenInfo info)
        {
            InitializeComponent();
        }

        public void ApplyParentInfo(DeviceListenInfo info)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentInfo = info;
                DeviceName.Text = info.GetTotalDevDescription();
                ParamName.Text = info.ParamDescription;
                ParamValue.Foreground = new SolidColorBrush(info.DisplayColor);
            });

        }
    }
}
