using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares;
using HardWares.端口基类;
using HardWares.端口基类部分;
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
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.实验部分.设备参数监测
{
    /// <summary>
    /// DeviceListenerBar.xaml 的交互逻辑
    /// </summary>
    public partial class ParamBar : Grid
    {

        protected static DecoratedButton ButtonTemplate = new DecoratedButton()
        {
            FontSize = 12,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")),
            Foreground = Brushes.White,
            MoveInColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF424242")),
            PressedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF39393A")),
            MoveInForeground = Brushes.White,
            PressedForeground = Brushes.White,
        };

        public DeviceListenInfo ParentInfo = null;

        public event Action<ParamBar> ColorSelectEvent = null;

        public event Action<ParamBar> ParamDeleteEvent = null;

        public event Action<ParamBar> SelectedEvent = null;

        public ParamBar(DeviceListenInfo info)
        {
            InitializeComponent();

            //添加右键菜单
            ContextMenu menu = new ContextMenu();
            DecoratedButton btn = new DecoratedButton() { Text = "删除" };
            ButtonTemplate.CloneStyleTo(btn);
            btn.Click += DeleteEvent;
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "刷新" };
            ButtonTemplate.CloneStyleTo(btn);
            btn.Click += RefreshEvent;
            menu.Items.Add(btn);
            menu.ItemHeight = 30;
            menu.ApplyToControl(this);

            ControlEventHelper chelper = new ControlEventHelper(Warning);
            chelper.MouseDoubleClick += new MouseButtonEventHandler((s, e) =>
            {
                MessageWindow.ShowTipWindow(ParentInfo.ErrorMessage, Window.GetWindow(this));
            });
        }

        /// <summary>
        /// 重新寻找设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshEvent(object sender, RoutedEventArgs e)
        {
            try
            {
                var dev = PortObject.FindDevice(ParentInfo.DeviceIdentifier, ParentInfo.DeviceProductName);
                if (dev == null) return;
                PortElement channel = null;
                if (dev is ElementPortObject)
                {
                    channel = (dev as ElementPortObject).Channels.Where((x) => x.ChannelName == ParentInfo.Channel.ChannelName).ElementAt(0);
                }
                Parameter p = null;
                if (channel == null)
                {
                    p = dev.AvailableParameterNames().Where((x) => x.ParameterName == ParentInfo.ParamName).ElementAt(0);
                }
                else
                {
                    p = channel.AvailableParameterNames().Where((x) => x.ParameterName == ParentInfo.ParamName).ElementAt(0);
                }
                ParentInfo.ValidateDev(dev, channel, p);
            }
            catch (Exception)
            {
                return;
            }
        }

        private void DeleteEvent(object sender, RoutedEventArgs e)
        {
            ParamDeleteEvent?.Invoke(this);
        }

        public void ApplyParentInfo(DeviceListenInfo info)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentInfo = info;
                string name = "";
                name = info.Device.ProductName;
                if (info.Channel != null)
                {
                    name += "  Ch->" + info.Channel.ChannelName;
                }
                DeviceName.Text = name;
                ParamName.Text = info.ParamDescription;
                ColorLabel.Background = new SolidColorBrush(info.DisplayColor);
            });

        }

        private void ColorLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ColorSelectEvent?.Invoke(this);
        }

        private void DataVisible_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataInVisible.Visibility = Visibility.Visible;
            DataVisible.Visibility = Visibility.Hidden;
            ParentInfo.DeviceDisplayData.IsVisible = false;
            ParentInfo.ParentPage.Chart.RefreshPlotWithAutoScaleY();
        }

        private void DataInVisible_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataInVisible.Visibility = Visibility.Hidden;
            DataVisible.Visibility = Visibility.Visible;
            ParentInfo.DeviceDisplayData.IsVisible = true;
            ParentInfo.ParentPage.Chart.RefreshPlotWithAutoScaleY();
        }

        private void Sample_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Sample.Visibility = Visibility.Hidden;
            StopSample.Visibility = Visibility.Visible;
            ParentInfo.IsSample = true;
        }

        private void StopSample_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Sample.Visibility = Visibility.Visible;
            StopSample.Visibility = Visibility.Hidden;
            ParentInfo.IsSample = false;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedEvent?.Invoke(this);
        }
    }
}
