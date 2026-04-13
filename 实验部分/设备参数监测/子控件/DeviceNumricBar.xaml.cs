using ODMR_Lab.实验部分.设备参数监控;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
