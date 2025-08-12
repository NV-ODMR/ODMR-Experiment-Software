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
    /// AFM下针
    /// </summary>
    public class AFMDrop : ScanCoreBase
    {
        /// <summary>
        /// AFM下针操作：输入参数：无
        /// 设备:LockIn
        /// 返回参数:下针结果(Bool,成功为True)
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
            //开始下针
            //如果初始输入为开,则先撤针
            if (lockin.Device.PIDOutput == true)
            {
                lockin.Device.SetPoint += 0.1;
                while (Math.Abs(lockin.Device.PIDValue) > 1e-9)
                {
                    Thread.Sleep(50);
                }
            }
            //开始下针
            lockin.Device.SetPoint = setpoint;
            lockin.Device.PIDOutput = true;
            double pidout1 = lockin.Device.PIDValue;
            Thread.Sleep(500);
            double pidout2 = lockin.Device.PIDValue;
            //如果达到上限或者PID输出出现下降(下到针)则结束下针
            while (pidout2 < lockin.Device.PIDOutputUpperLimit && pidout2 > pidout1)
            {
                pidout1 = lockin.Device.PIDValue;
                Thread.Sleep(500);
                pidout2 = lockin.Device.PIDValue;
                Thread.Sleep(50);
            }
            Thread.Sleep(500);
            pidout2 = lockin.Device.PIDValue;
            //没有下到返回False
            if (Math.Abs(pidout2 - lockin.Device.PIDOutputUpperLimit) < 0.005)
            {
                return new List<object>() { false };
            }
            return new List<object>() { true };
        }
    }
}
