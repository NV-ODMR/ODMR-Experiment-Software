using CodeHelper;
using HardWares.板卡;

namespace ODMR_Lab.设备部分.板卡
{
    public class PulseBlasterInfo : DeviceInfoBase<PulseBlasterBase>
    {
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
