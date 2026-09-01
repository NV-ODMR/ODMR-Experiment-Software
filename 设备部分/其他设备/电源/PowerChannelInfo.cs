using HardWares.电源;

namespace ODMR_Lab.设备部分.其他设备
{
    public class PowerChannelInfo : DeviceElementInfoBase<PowerChannelBase>
    {
        public override bool IsLoadParams { get; set; } = false;

        public PowerInfo ParentInfo { get; protected set; } = null;

        public string Name
        { get; set; } = "";

        public PowerChannelInfo(PowerInfo parent, PowerChannelBase channel, string name)
        {
            ParentInfo = parent;
            Device = channel;
            Name = name;
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
}
