using CodeHelper;
using HardWares.Lock_In;

namespace ODMR_Lab.设备部分.其他设备
{
    public class LockinInfo : DeviceInfoBase<LockInBase>
    {
        public override bool IsLoadParams { get; set; } = false;

        public override void CreateDeviceInfoBehaviour()
        {
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
