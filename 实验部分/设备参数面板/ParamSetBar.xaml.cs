using Controls;
using HardWares.端口基类;
using HardWares;
using HardWares.端口基类部分;
using ODMR_Lab.实验部分.设备参数监测;
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
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.参数设置面板;

namespace ODMR_Lab.实验部分.设备参数面板
{
    /// <summary>
    /// ParamSetBar.xaml 的交互逻辑
    /// </summary>
    public partial class ParamSetBar : Grid
    {
        public event Action<ParamSetBar> SelectedEvent = null;

        public event Action<ParamSetBar> ParamDeleteEvent = null;

        ParamSetInfo ParentParam = null;

        public ParamSetBar(ParamSetInfo parentParam)
        {
            ParentParam = parentParam;
            InitializeComponent();

            //添加右键菜单
            ContextMenu menu = new ContextMenu();
            DecoratedButton btn = new DecoratedButton() { Text = "删除" };
            UIUpdater.SetDefaultTemplate(btn);
            btn.Click += DeleteEvent;
            menu.Items.Add(btn);
            menu.ItemHeight = 30;
            menu.ApplyToControl(this);
        }

        public void LoadParam()
        {
            if (ParentParam.TargetParameter == null) return;
            try
            {
                BoolValue.Visibility = Visibility.Hidden;
                EnumValue.Visibility = Visibility.Hidden;
                StringValue.Visibility = Visibility.Hidden;

                if (ParentParam.TargetParameter.ParamType == typeof(string) ||
                    ParentParam.TargetParameter.ParamType == typeof(int) ||
                    ParentParam.TargetParameter.ParamType == typeof(double) ||
                    ParentParam.TargetParameter.ParamType == typeof(float)
                    )
                {
                    StringValue.Text = ParentParam.TargetParameter.ReadValue().ToString();
                    StringValue.Visibility = Visibility.Visible;
                    if (ParentParam.TargetParameter.IsReadOnly) StringValue.IsReadOnly = true;
                    else StringValue.IsReadOnly = false;
                }
                if (ParentParam.TargetParameter.ParamType == typeof(bool))
                {
                    BoolValue.IsSelected = ParentParam.TargetParameter.ReadValue();
                    BoolValue.Visibility = Visibility.Visible;
                    if (ParentParam.TargetParameter.IsReadOnly) BoolValue.IsEnabled = false;
                    else BoolValue.IsEnabled = true;
                }
                if (ParentParam.TargetParameter.ParamType.IsEnum)
                {
                    EnumValue.Items.Clear();
                    var names = Enum.GetNames(ParentParam.TargetParameter.ParamType);
                    foreach (var item in names)
                    {
                        DecoratedButton btn = new DecoratedButton() { Text = item };
                        UIUpdater.SetDefaultTemplate(btn);
                        EnumValue.Items.Add(btn);
                    }
                    EnumValue.Select((string)Enum.GetName(ParentParam.TargetParameter.ParamType, ParentParam.TargetParameter.ReadValue()));
                    EnumValue.Visibility = Visibility.Visible;
                    if (ParentParam.TargetParameter.IsReadOnly) EnumValue.IsEnabled = false;
                    else EnumValue.IsEnabled = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private dynamic GetSetValue()
        {
            if (BoolValue.Visibility == Visibility.Visible) return BoolValue.IsSelected;
            if (EnumValue.Visibility == Visibility.Visible) return Enum.Parse(ParentParam.TargetParameter.ParamType, EnumValue.SelectedItem.Text);
            if (StringValue.Visibility == Visibility.Visible)
            {
                if (ParentParam.TargetParameter.ParamType == typeof(string))
                {
                    return StringValue.Text;
                }
                if (ParentParam.TargetParameter.ParamType == typeof(int))
                {
                    return int.Parse(StringValue.Text);
                }
                if (ParentParam.TargetParameter.ParamType == typeof(double))
                {
                    return double.Parse(StringValue.Text);
                }
                if (ParentParam.TargetParameter.ParamType == typeof(float))
                {
                    return float.Parse(StringValue.Text);
                }
            }
            return null;
        }

        private void UpdateDeviceAttach()
        {
            try
            {
                var dev = PortObject.FindDevice(ParentParam.DeviceIdentifier, ParentParam.DeviceProductName);
                if (dev == null) return;
                PortElement channel = null;
                if (dev is ElementPortObject)
                {
                    channel = (dev as ElementPortObject).Channels.Where((x) => x.ChannelName == ParentParam.Channel.ChannelName).ElementAt(0);
                }
                Parameter p = null;
                if (channel == null)
                {
                    p = dev.AvailableParameterNames().Where((x) => x.ParameterName == ParentParam.ParamName).ElementAt(0);
                }
                else
                {
                    p = channel.AvailableParameterNames().Where((x) => x.ParameterName == ParentParam.ParamName).ElementAt(0);
                }
                ParentParam.ValidateDev(dev, channel, p);
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

        private void ReadCommand(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateDeviceAttach();
                ParentParam.ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ParentParam.ErrorMessage = ex.Message;
            }

            ParentParam.ValidateErrorDisplay();
        }

        private void SetCommand(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateDeviceAttach();
                if (ParentParam.TargetParameter == null) throw new Exception("参数不存在");
                //设置参数
                ParentParam.TargetParameter.WriteValue(GetSetValue());
                TimeWindow w = new TimeWindow();
                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                w.Owner = Window.GetWindow(this);
                w.ShowWindow("设置成功");
                ParentParam.ErrorMessage = "";
                //读取参数
                LoadParam();
            }
            catch (Exception ex)
            {
                ParentParam.ErrorMessage = ex.Message;
            }

            ParentParam.ValidateErrorDisplay();
        }

        public void ApplyParentInfo(ParamSetInfo info)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentParam = info;
                string name = "";
                name = info.DeviceProductName;
                if (info.ChannelName != "")
                {
                    name += "  Ch->" + info.ChannelName;
                }
                DeviceName.Text = name;
                ParamName.Text = info.ParamDescription;
            });
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedEvent?.Invoke(this);
        }
    }
}
