using CodeHelper;
using HardWares;
using HardWares.APD;
using HardWares.APD.Exclitas_SPCM_AQRH;
using HardWares.相机_CCD_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分.光子探测器
{
    public class APDInfo : DeviceInfoBase<APDBase>
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
