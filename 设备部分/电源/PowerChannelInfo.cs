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
using HardWares.电源;

namespace ODMR_Lab.设备部分.电源
{
    public class PowerChannelInfo : InfoBase
    {
        public PowerInfo ParentInfo { get; protected set; } = null;

        /// <summary>
        /// 通道
        /// </summary>
        public PowerChannelBase Channel { get; private set; } = null;

        public string Name
        { get; set; } = "";

        public PowerChannelInfo(PowerInfo parent, PowerChannelBase channel, string name)
        {
            ParentInfo = parent;
            Channel = channel;
            Name = name;
        }

        public override void CreateDeviceInfoBehaviour()
        {
        }

        public override string GetDeviceDescription()
        {
            return ParentInfo.Device.ProductName + " " + Channel.ChannelName;
        }
    }
}
