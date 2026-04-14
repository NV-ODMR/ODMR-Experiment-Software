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

namespace ODMR_Lab.设备部分.其他设备
{
    public class TemperatureChannelInfo : DeviceElementInfoBase<TemperatureChannelBase>
    {
        public TemperatureControllerInfo ParentInfo { get; protected set; } = null;

        public string Name
        { get; set; } = "";
        public override bool IsLoadParams { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TemperatureChannelInfo(TemperatureControllerInfo parentinfo, TemperatureChannelBase channel)
        {
            ParentInfo = parentinfo;
            Device = channel;
        }

        public override void CreateDeviceInfoBehaviour()
        {
        }

        public override string GetDeviceDescription()
        {
            return ParentInfo.Device.ProductName + " " + Device.ChannelName;
        }
        public override string GetChannelDescription()
        {
            return Device.ChannelName;
        }
    }

    public class TemperatureControllerInfo : DeviceInfoBase<TemperatureControllerBase>
    {
        public override bool IsLoadParams { get; set; } = false;

        public TemperatureControllerInfo()
        {
        }

        public List<TemperatureChannelInfo> ChannelsInfo { get; set; } = new List<TemperatureChannelInfo>();

        /// <summary>
        /// 新建一个设备Info
        /// </summary>
        /// <param name="device"></param>
        /// <param name="connectinfo"></param>
        /// <returns></returns>
        public override void CreateDeviceInfoBehaviour()
        {
            //添加通道按钮
            foreach (var item in Device.Channels)
            {
                TemperatureChannelInfo sensorinfo = new TemperatureChannelInfo(this, item as TemperatureChannelBase);
                ChannelsInfo.Add(sensorinfo);
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
