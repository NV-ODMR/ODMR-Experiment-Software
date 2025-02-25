using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    public class LaserOff : ScanCoreBase
    {
        /// <summary>
        /// 关闭激光：输入参数:无
        /// 设备:板卡
        /// 返回参数:无
        /// </summary>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            PulseBlasterInfo pb = devices[0] as PulseBlasterInfo;
            pb.Device.End();
            return new List<object>();
        }
    }
}
