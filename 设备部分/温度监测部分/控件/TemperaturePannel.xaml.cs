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

namespace ODMR_Lab.设备部分.温控
{
    /// <summary>
    /// TemperaturePannel.xaml 的交互逻辑
    /// </summary>
    public partial class TemperaturePannel : Border
    {
        public enum PanelType
        {
            Temperature = 0,
            Output = 1
        }

        public TemperaturePannel(PanelType type, string panelName)
        {
            InitializeComponent();
            ChannelName.Text = panelName;
            if (type == PanelType.Temperature)
            {
                Value.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4FC058"));
            }
            if (type == PanelType.Output)
            {
                Value.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC0A916"));
            }
        }
    }
}
