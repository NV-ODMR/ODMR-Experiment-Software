using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.温度控制器.SRS_PTC10;
using System.Windows.Media;
using Controls.Charts;
using HardWares.温度控制器;
using Controls;
using ODMR_Lab.Windows;
using System.Windows;
using CodeHelper;
using System.IO;
using ODMR_Lab.基本控件;

namespace ODMR_Lab.温度监测部分
{
    public abstract class ChannelInfo
    {
        public TemperatureControllerInfo ParentInfo { get; protected set; } = null;

        public bool IsSelected { get; set; } = false;

        public TimeChartData1D Time { get; protected set; } = new TimeChartData1D("", "");

        public NumricChartData1D Value { get; protected set; } = new NumricChartData1D("", "");

        public DecoratedButton DisplayBtn { get; set; } = null;

        public string Name
        { get; set; } = "";

        public ChannelInfo(string name, DecoratedButton btn)
        {
            DisplayBtn = btn;
            Name = name;
        }
    }

    public class SensorChannelInfo : ChannelInfo
    {
        /// <summary>
        /// 通道
        /// </summary>
        public SensorChannelBase Channel { get; private set; } = null;


        public SensorChannelInfo(TemperatureControllerInfo parentinfo, SensorChannelBase channel, string name, DecoratedButton btn) : base(name, btn)
        {
            Channel = channel;
            Name = name;
            Value.Name = name + "温度数据(" + channel.Unit + ")";
            Value.GroupName = "温度监测数据";
            ParentInfo = parentinfo;
        }

    }

    public class OutputChannelInfo : ChannelInfo
    {
        /// <summary>
        /// 通道
        /// </summary>
        public OutputChannelBase Channel { get; private set; } = null;

        public OutputChannelInfo(TemperatureControllerInfo parentinfo, OutputChannelBase channel, string name, DecoratedButton btn) : base(name, btn)
        {
            Channel = channel;
            Name = name;
            Value.Name = name + "输出数据(" + channel.Unit + ")";
            Value.GroupName = "温度监测数据";
            ParentInfo = parentinfo;
        }
    }

    public class TemperatureControllerInfo : DeviceInfoBase<TemperatureControllerBase>
    {
        public TemperatureControllerInfo()
        {
        }

        public List<SensorChannelInfo> SensorsInfo { get; set; } = new List<SensorChannelInfo>();

        public List<OutputChannelInfo> OutputsInfo { get; set; } = new List<OutputChannelInfo>();

        /// <summary>
        /// 新建一个设备Info
        /// </summary>
        /// <param name="device"></param>
        /// <param name="connectinfo"></param>
        /// <returns></returns>
        public override void CreateDeviceInfoBehaviour()
        {
            DevicePage devpage = MainWindow.Dev_TemPeraPage;
            //添加通道按钮
            foreach (var item in Device.SensorChannels)
            {
                DecoratedButton btn = new DecoratedButton();
                btn.Text = Device.ProductName + ":" + item.Name;
                SensorChannelInfo sensorinfo = new SensorChannelInfo(this, item, Device.ProductName + "_" + item.Name, btn);
                SensorsInfo.Add(sensorinfo);
            }
            foreach (var item in Device.OutputChannels)
            {
                DecoratedButton btn = new DecoratedButton();
                btn.Text = Device.ProductName + ":" + item.Name;
                OutputChannelInfo outputinfo = new OutputChannelInfo(this, item, Device.ProductName + "_" + item.Name, btn);
                OutputsInfo.Add(outputinfo);
            }
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }
    }
}
