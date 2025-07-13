using CodeHelper;
using HardWares.Lock_In;
using HardWares.仪器列表.电动翻转座;

namespace ODMR_Lab.设备部分.射频源_锁相放大器
{
    public class LockinInfo : DeviceInfoBase<LockInBase>
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
