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

namespace ODMR_Lab.设备部分.源表
{
    /// <summary>
    /// TemperaturePannel.xaml 的交互逻辑
    /// </summary>
    public partial class SourceMeterPannel : Border
    {
        public enum PanelTypes
        {
            Voltage = 0,
            Current = 1
        }

        public PanelTypes PanelType { get; set; } = PanelTypes.Voltage;

        public string DisplayName { get; set; } = "";

        public SourceMeterPannel()
        {
            InitializeComponent();
        }

        public void SetRestrictState(bool isLimit)
        {
            if (isLimit)
            {
                BorderBrush = Brushes.Red;
            }
            else
            {
                BorderBrush = Brushes.Transparent;
            }
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            ChannelName.Text = DisplayName;
            if (PanelType == PanelTypes.Voltage)
            {
                Value.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF009ED2"));
            }
            if (PanelType == PanelTypes.Current)
            {
                Value.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF23B769"));
            }
        }
    }
}
