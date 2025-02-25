using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    public class ConfocalAPDSample : ScanCoreBase
    {
        /// <summary>
        /// 共聚焦单点光子采样方法：输入参数：采样率（Hz）
        /// 设备:光子计数器
        /// 返回参数:光子计数率(整数)
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            //打开APD
            APDInfo apd = devices[0] as APDInfo;
            apd.StartContinusSample();
            double ratio = apd.GetContinusSampleRatio();
            //关闭APD
            apd.EndContinusSample();
            return new List<object>() { ratio };
        }
    }
}
