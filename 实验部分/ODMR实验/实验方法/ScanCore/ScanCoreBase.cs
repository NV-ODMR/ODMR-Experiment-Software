using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    public abstract class ScanCoreBase
    {
        /// <summary>
        /// 实验核心方法
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        public abstract List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices);
    }
}
