using CodeHelper;
using HardWares;
using HardWares.相机_CCD_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.波源;

namespace ODMR_Lab.射频源_锁相放大器
{
    public class RFSourceInfo : DeviceInfoBase<RFSignalGeneratorBase>
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
