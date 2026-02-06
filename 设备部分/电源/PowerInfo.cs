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
using HardWares.电源;

namespace ODMR_Lab.设备部分.电源
{
    public class PowerInfo : DeviceInfoBase<PowerBase>
    {
        public override bool IsLoadParams { get; set; } = false;

        public PowerInfo()
        {
        }

        public List<PowerChannelInfo> ChannelsInfo { get; set; } = new List<PowerChannelInfo>();

        public override void CreateDeviceInfoBehaviour()
        {
            foreach (var item in Device.Channels)
            {
                PowerChannelInfo channelinfo = new PowerChannelInfo(this, item, item.ChannelName);
                ChannelsInfo.Add(channelinfo);
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
