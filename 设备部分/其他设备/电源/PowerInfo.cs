using CodeHelper;
using HardWares.电源;
using System.Collections.Generic;

namespace ODMR_Lab.设备部分.其他设备
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
                PowerChannelInfo channelinfo = new PowerChannelInfo(this, item as PowerChannelBase, item.ChannelName);
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
