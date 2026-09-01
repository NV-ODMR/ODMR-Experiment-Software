using HardWares.Lock_In.Zurich_LockIn;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.其他设备;
using System;
using System.Collections.Generic;
using System.Threading;

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

            (lockin.Device as LockIn).SourceOutput = true;
            Thread.Sleep(1000);
            lockin.Device.PIDOutputLowerLimit = 0;

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
