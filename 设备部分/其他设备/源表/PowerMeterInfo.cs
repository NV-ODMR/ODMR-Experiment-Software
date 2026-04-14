using CodeHelper;
using HardWares.纳米位移台;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.源表;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.设备部分.其他设备
{

    /// <summary>
    /// 源表
    /// </summary>
    public class PowerMeterInfo : DeviceInfoBase<PowerSourceBase>
    {
        /// <summary>
        /// 电流测量数据
        /// </summary>
        public List<double> CurrentBuffer { get; set; } = new List<double>();

        public override bool IsLoadParams { get; set; } = false;
        public PowerMeterInfo()
        {
        }

        public override void CreateDeviceInfoBehaviour()
        {
        }

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }
    }
}
