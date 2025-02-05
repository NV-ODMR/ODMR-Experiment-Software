using HardWares;
using HardWares.温度控制器;
using HardWares.相机_CCD_;
using HardWares.纳米位移台;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    public class DeviceConnectInfo
    {
        public string USBName { get; protected set; } = "";

        public string COMName { get; protected set; } = "";

        public string COMBaud { get; protected set; } = "";

        public DeviceConnectInfo(PortType type, params string[] param)
        {
            if (type == PortType.USB)
            {
                USBName = param[0];
            }
            if (type == PortType.COM)
            {
                COMName = param[0];
                COMBaud = param[1];
            }
        }
    }
}
