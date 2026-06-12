using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HardWares.Lock_In.Zurich_LockIn;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.板卡;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.其他设备;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    /// <summary>
    /// AFM下针
    /// </summary>
    public class AFMFloatDrop : ScanCoreBase
    {

        /// <summary>
        /// AFM悬浮下针操作：输入参数：最大限制电压(V),下到针之后抬高的距离(V),参数I,PID采样时间,悬浮操作距离,悬浮操作的方法,下针后是否关闭输出
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
            (lockin.Device as LockIn).SourceOutput = true;
            Thread.Sleep(2000);
            lockin.Device.PIDOutputUpperLimit = (double)InputParams[0];
            lockin.Device.PIDOutputLowerLimit = 0;
            lockin.Device.I = (double)InputParams[2];
            double pidout1 = lockin.Device.PIDValue;
            Thread.Sleep(50);
            double pidout2 = lockin.Device.PIDValue;
            //如果达到上限或者PID输出出现下降(下到针)则结束下针
            while (pidout2 < lockin.Device.PIDOutputUpperLimit && Math.Abs(pidout2 - lockin.Device.PIDOutputUpperLimit) > 1e-3 && pidout2 >= pidout1)
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
            //如果下到则撤针一定距离以悬浮测量(设置一定长度的采样时间,取这段时间中的PID平均值作为高度数据)
            else
            {
                int sampletime = 0;
                int Totalsampletime = (int)InputParams[3];
                List<double> pids = new List<double>();
                pids.Add(lockin.Device.PIDValue);
                while (sampletime < Totalsampletime)
                {
                    double temppid = lockin.Device.PIDValue;
                    if (!double.IsNaN(temppid))
                        pids.Add(temppid);
                    Thread.Sleep(50);
                    sampletime += 50;
                }

                double initheight = pids.Average();

                //这里先下到悬浮操作位置进行操作,之后再下到指定悬浮高度
                double dropheight = (double)InputParams[4];
                double currentvalue = lockin.Device.PIDValue;
                lockin.Device.SetPoint += 10 * 1e-3;
                int time = 0;
                //撤针以达到指定高度
                double height = Math.Max(0, initheight - dropheight);
                while (currentvalue > height && time < 50000)
                {
                    Thread.Sleep(20);
                    currentvalue = lockin.Device.PIDValue;
                    time += 20;
                }
                //如果超时则返回失败结果
                if (time >= 50000)
                {
                    return new List<object>() { false };
                }
                lockin.Device.PIDOutputUpperLimit = height;
                lockin.Device.SetPoint = setpoint;
                if ((double)InputParams[1] <= 0)
                {
                    //如果下针悬浮高度小于0则默认为接触扫描
                    lockin.Device.PIDOutputUpperLimit = (double)InputParams[0];
                    lockin.Device.SetPoint = setpoint;
                    //判断是否达到上限
                    pidout1 = lockin.Device.PIDValue;
                    Thread.Sleep(50);
                    pidout2 = lockin.Device.PIDValue;

                    //如果达到上限或者PID输出出现下降(下到针)则结束下针
                    while (pidout2 < lockin.Device.PIDOutputUpperLimit && Math.Abs(pidout2 - lockin.Device.PIDOutputUpperLimit) > 1e-3 && pidout2 >= pidout1 && pidout2 > 1e-3)
                    {
                        pidout1 = lockin.Device.PIDValue;
                        Thread.Sleep(50);
                        pidout2 = lockin.Device.PIDValue;
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    //在悬浮操作高度需要进行的操作
                    Action method = null;
                    if (InputParams[5] != null) method = InputParams[5] as Action;
                    method?.Invoke();
                    //判断,如果悬浮操作高度小于悬浮高度则报错
                    if ((double)InputParams[1] > dropheight) return new List<object>() { false };
                    //设置输出上限
                    dropheight = (double)InputParams[1];
                    height = Math.Max(0, initheight - dropheight);
                    lockin.Device.PIDOutputUpperLimit = height;
                    lockin.Device.SetPoint = setpoint;
                    //判断是否达到上限,如果达到上限或者小于上限则结束下针
                    time = 0;
                    while (Math.Abs(lockin.Device.PIDValue - height) > 1e-4 && time < 50000)
                    {
                        Thread.Sleep(50);
                        time += 50;
                    }
                    if (time >= 50000)
                    {
                        //如果小于上限则认为已经下到但是接触
                        if (lockin.Device.PIDValue < height)
                            return new List<object>() { true };
                        else
                            return new List<object>() { false };
                    }
                }
                //持续监控,发现下降则自动降低高度,这是,如果选择关闭驱动电压,则进行如下操作:设置LowerLimit和Upperlimit保持一致,然后关闭输出
                if ((bool)InputParams[6])
                {
                    Thread.Sleep(1000);
                    //设置LowerLimit和Upperlimit保持一致
                    lockin.Device.PIDOutputLowerLimit = lockin.Device.PIDOutputUpperLimit;
                    //关闭输出
                    (lockin.Device as LockIn).SourceOutput = false;
                    Thread.Sleep(1000);
                }
                return new List<object>() { true };
            }
        }

        /// <summary>
        /// 撤针指定距离：输入参数：撤针目标距离,下针后是否关闭输出
        /// 设备:LockIn
        /// 返回参数:下针结果(Bool,成功为True)
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        public List<object> DistractDistance(List<object> InputParams, params InfoBase[] devices)
        {
            if ((double)InputParams[0] < 0) return new List<object>() { false };

            //下针
            LockinInfo lockin = devices[0] as LockinInfo;
            //读取SetPoint
            double setpoint = lockin.Device.SetPoint;
            //如果发现锁相输出没开则什么也不做
            if (lockin.Device.PIDOutput == false)
            {
                return new List<object>() { true };
            }

            (lockin.Device as LockIn).SourceOutput = true;
            Thread.Sleep(2000);
            //打开驱动输出,同时恢复下限到0
            lockin.Device.PIDOutputLowerLimit = 0;


            double initheight = lockin.Device.PIDValue;
            double dropheight = (double)InputParams[0];
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
            //判断是否达到上限,如果达到上限或者小于上限则结束下针
            time = 0;
            while (Math.Abs(lockin.Device.PIDValue - height) > 1e-4 && time < 20000)
            {
                Thread.Sleep(50);
                time += 50;
            }
            if (time >= 20000)
            {
                //如果小于上限则认为已经下到但是接触
                if (lockin.Device.PIDValue < height)
                    return new List<object>() { true };
                else
                    return new List<object>() { false };
            }

            //持续监控,发现下降则自动降低高度,这是,如果选择关闭驱动电压,则进行如下操作:设置LowerLimit和Upperlimit保持一致,然后关闭输出
            if ((bool)InputParams[1])
            {
                Thread.Sleep(1000);
                //设置LowerLimit和Upperlimit保持一致
                lockin.Device.PIDOutputLowerLimit = lockin.Device.PIDOutputUpperLimit;
                //关闭输出
                (lockin.Device as LockIn).SourceOutput = false;
                Thread.Sleep(1000);
            }
            return new List<object>() { true };
        }
    }
}
