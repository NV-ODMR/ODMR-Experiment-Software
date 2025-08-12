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
    public class AFMFloatDrop : ScanCoreBase
    {
        Thread AFMFloatListener = null;

        /// <summary>
        /// AFM悬浮下针操作：输入参数：最大限制电压(V),下到针之后抬高的距离(V)
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
            //如果发现锁相输出没开则什么也不做
            if (lockin.Device.PIDOutput == false)
            {
                return new List<object>() { true };
            }
            //开始下针
            lockin.Device.SetPoint = setpoint;
            lockin.Device.PIDOutput = true;
            lockin.Device.PIDOutputUpperLimit = (double)InputParams[0];
            double pidout1 = lockin.Device.PIDValue;
            Thread.Sleep(50);
            double pidout2 = lockin.Device.PIDValue;
            //如果达到上限或者PID输出出现下降(下到针)则结束下针
            while (pidout2 < lockin.Device.PIDOutputUpperLimit && pidout2 >= pidout1)
            {
                pidout1 = lockin.Device.PIDValue;
                Thread.Sleep(50);
                pidout2 = lockin.Device.PIDValue;
                Thread.Sleep(50);
            }
            Thread.Sleep(1000);
            pidout2 = lockin.Device.PIDValue;
            //没有下到返回False
            if (Math.Abs(pidout2 - lockin.Device.PIDOutputUpperLimit) < 0.005)
            {
                return new List<object>() { false };
            }
            //如果下到则撤针一定距离以悬浮测量
            else
            {
                double initheight = lockin.Device.PIDValue;
                double dropheight = (double)InputParams[1];
                double currentvalue = lockin.Device.PIDValue;
                lockin.Device.SetPoint += 30 * 1e-3;
                int time = 0;
                //撤针以达到指定高度
                double height = Math.Max(0, initheight - dropheight);
                while (currentvalue > height && time < 20000)
                {
                    Thread.Sleep(50);
                    currentvalue = lockin.Device.PIDValue;
                    time += 50;
                }
                //如果超时则返回失败结果
                if (time >= 20000)
                {
                    return new List<object>() { false };
                }
                //设置输出上限
                lockin.Device.PIDOutputUpperLimit = height;
                lockin.Device.SetPoint = setpoint;
                //判断是否达到上限
                time = 0;
                while (Math.Abs(lockin.Device.PIDValue - height) > 1e-4 && time < 20000)
                {
                    Thread.Sleep(50);
                    time += 50;
                }
                if (time >= 20000)
                {
                    return new List<object>() { false };
                }

                //持续监控,发现下降则自动降低高度
                return new List<object>() { true };
            }
        }
    }
}
