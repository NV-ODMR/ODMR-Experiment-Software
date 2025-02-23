using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.板卡;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    /// <summary>
    /// AFM撤针
    /// </summary>
    public class AFMStopDrop : ScanCoreBase
    {
        /// <summary>
        /// AFM撤针操作：输入参数：无
        /// 设备:LockIn
        /// 返回参数:无
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            //下针
            LockinInfo lockin = devices[0] as LockinInfo;
            //读取SetPoint
            double setpoint = lockin.Device.SetPoint;
            //开始撤针
            lockin.Device.SetPoint += 0.1;
            while (Math.Abs(lockin.Device.PIDValue) > 1e-9)
            {
                Thread.Sleep(500);
            }
            //关闭PID输出
            lockin.Device.PIDOutput = false;
            lockin.Device.SetPoint = setpoint;
            return new List<object>();
        }
    }
}
