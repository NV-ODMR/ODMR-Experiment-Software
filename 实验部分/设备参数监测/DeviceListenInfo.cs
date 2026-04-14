using CodeHelper;
using Controls;
using Controls.Charts;
using HardWares;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.端口基类部分.设备信息;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.温度监测;
using ODMR_Lab.实验部分.设备参数监测;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.实验部分.设备参数监测
{
    /// <summary>
    /// 监控目标设备类
    /// </summary>
    public class DeviceListenInfo
    {
        /// <summary>
        /// 设备
        /// </summary>
        public PortObject Device { get; private set; } = null;

        public DisplayPage ParentPage = null;

        /// <summary>
        /// 通道（不存在时为空）
        /// </summary>
        public PortElement Channel { get; private set; } = null;

        /// <summary>
        /// 显示位数
        /// </summary>
        public int DisplayDigit { get; set; } = 5;

        public string DeviceProductName { get; private set; } = "";

        public string DeviceIdentifier { get; private set; } = "";

        public string ChannelName { get; private set; } = "";

        public string ParamName { get; private set; } = "";

        public string ParamDescription { get; private set; } = "";

        /// <summary>
        /// 目标参数
        /// </summary>
        public Parameter TargetParameter
        { get; set; }

        /// <summary>
        /// 在已经存在连接设备时调用此初始化方法，直接创建一个包含已连接设备的DeviceListenInfo对象
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="device"></param>
        /// <param name="channel"></param>
        /// <param name="targetParameter"></param>
        public DeviceListenInfo(DisplayPage parent, PortObject device, PortElement channel, Parameter targetParameter)
        {
            ParentPage = parent;
            Device = device;
            Channel = channel;
            TargetParameter = targetParameter;

            App.Current.Dispatcher.Invoke(() =>
            {
                DeviceNumberBar = new DeviceNumricBar(this);
                DeviceParamBar = new ParamBar(this);
                DeviceDisplayData = new TimeDataSeries("")
                {
                    LineColor = DisplayColor,
                    LineThickness = ParentPage.StyleParam.LineWidth.Value,
                    MarkerSize = ParentPage.StyleParam.PointSize.Value,
                    Smooth = ParentPage.StyleParam.IsSmooth.Value,
                };
            });

            DeviceProductName = device.ProductName;
            DeviceIdentifier = device.ProductIdentifier;
            ChannelName = channel == null ? "" : channel.ChannelName;
            ParamName = targetParameter.ParameterName;
            ParamDescription = targetParameter.Description;
        }

        /// <summary>
        /// 在不存在连接设备时调用此初始化方法用于创建占位的参数栏，在UI中刷新后通过输入参数自动搜索设备并包含在内
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="device"></param>
        /// <param name="channel"></param>
        /// <param name="targetParameter"></param>
        public DeviceListenInfo(DisplayPage parent, string deviceProductName, string deviceIdentifier, string channelname, string paramName, string paramdescription)
        {
            ParentPage = parent;
            DeviceProductName = deviceProductName;
            DeviceIdentifier = deviceIdentifier;
            ChannelName = channelname;
            ParamName = paramName;
            ParamDescription = paramdescription;

            App.Current.Dispatcher.Invoke(() =>
            {
                DeviceNumberBar = new DeviceNumricBar(this);
                DeviceParamBar = new ParamBar(this);
                DeviceDisplayData = new TimeDataSeries("")
                {
                    LineColor = DisplayColor,
                    LineThickness = ParentPage.StyleParam.LineWidth.Value,
                    MarkerSize = ParentPage.StyleParam.PointSize.Value,
                    Smooth = ParentPage.StyleParam.IsSmooth.Value,
                };
            });
        }

        /// <summary>
        /// 获取完整描述（包含通道信息）
        /// </summary>
        /// <returns></returns>
        public string GetTotalDevDescription()
        {
            string result = DeviceProductName;
            if (ChannelName != "")
            {
                result += "  ->" + ChannelName;
            }
            return result;
        }

        public string GetParamName()
        {
            return ParamName;
        }

        public void ValidateDev(PortObject device, PortElement channel, Parameter targetParameter)
        {
            Device = device;
            Channel = channel;
            TargetParameter = targetParameter;

            DeviceNumberBar.ApplyParentInfo(this);
            DeviceParamBar.ApplyParentInfo(this);

            DeviceProductName = device.ProductName;
            DeviceIdentifier = device.ProductIdentifier;
            ChannelName = channel == null ? "" : channel.ChannelName;
            ParamName = targetParameter.ParameterName;
        }

        public DeviceNumricBar DeviceNumberBar { get; set; } = null;

        public ParamBar DeviceParamBar { get; set; } = null;

        public bool IsSample = false;
        public TimeDataSeries DeviceDisplayData = null;

        public Queue<DateTime> time = new Queue<DateTime>();
        public Queue<double> value = new Queue<double>();

        public double LastValue = double.NaN;

        public string ErrorMessage { get; set; } = "";

        public void ValidateErrorDisplay()
        {
            if (ErrorMessage == "") DeviceParamBar.Warning.Visibility = System.Windows.Visibility.Hidden;
            else
            {
                DeviceParamBar.Warning.Visibility = System.Windows.Visibility.Visible;
            }
        }


        public void AddData(DateTime t, double v)
        {
            lock (time)
            {
                lock (value)
                {
                    time.Enqueue(t);
                    value.Enqueue(v);
                    while (time.Count > ParentPage.StoredPointCount)
                    {
                        time.Dequeue();
                    }
                    while (value.Count > ParentPage.StoredPointCount)
                    {
                        value.Dequeue();
                    }
                    DeviceDisplayData.X = time.ToList();
                    DeviceDisplayData.Y = value.ToList();
                }
            }
        }
        public void ClearData()
        {
            lock (time)
            {
                lock (value)
                {
                    time.Clear();
                    value.Clear();
                    DeviceDisplayData.X = time.ToList();
                    DeviceDisplayData.Y = value.ToList();
                }
            }
        }

        public void GetData(out List<DateTime> t, out List<double> v)
        {
            lock (time)
            {
                lock (value)
                {
                    t = time.ToArray().ToList();
                    v = value.ToArray().ToList();
                }
            }
        }

        private Color displaycolor = CodeHelper.ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF1A1A1A"));
        /// <summary>
        /// 显示颜色
        /// </summary>
        public Color DisplayColor
        {
            get { return displaycolor; }
            set
            {
                displaycolor = value;
                DeviceDisplayData.LineColor = value;
                App.Current.Dispatcher.Invoke(() =>
                {
                    DeviceNumberBar.ParamValue.Foreground = new SolidColorBrush(value);
                    DeviceParamBar.ColorLabel.Background = new SolidColorBrush(value);
                });
            }
        }
    }
}
