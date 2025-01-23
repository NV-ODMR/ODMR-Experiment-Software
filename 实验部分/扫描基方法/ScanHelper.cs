using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ODMR_Lab.位移台部分;

namespace ODMR_Lab.实验部分.扫描基方法
{
    public abstract class ScanHelper
    {
        /// <summary>
        /// 扫描时进行的操作（函数具有输入和输出参数,originOutput为上一步的输出）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public delegate List<object> ScanOperation(NanoStageInfo stage, double loc, List<object> originOutput);

        /// <summary>
        /// 移动X轴
        /// </summary>
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        protected static void MoveWithoutUse(NanoStageInfo mover, Action StopMethod, double RestrictLo, double RestrictHi, double target, int timeout, double maxStep = 0.1)
        {
            return;
            if (target > RestrictHi || target < RestrictLo)
            {
                MessageLogger.AddLogger("磁场定位", "位移台" + Enum.GetName(typeof(MoverTypes), mover.MoverType) + "超量程", MessageTypes.Warning);
                return;
            }
            double target0 = mover.Device.Target;
            double det = target - target0;
            int sgn = det > 0 ? 1 : -1;
            det = Math.Abs(det);
            while (det > maxStep)
            {
                target0 += sgn * maxStep;
                mover.Device.MoveToAndWait(target, timeout);
                StopMethod?.Invoke();
                det -= maxStep;
                Thread.Sleep(50);
            }
            mover.Device.MoveToAndWait(target0 + sgn * det, timeout);
            StopMethod?.Invoke();
        }

        /// <summary>
        /// 移动位移台，如果移动超量程或者设备被占用则报错
        /// </summary>
        /// <param name="mover"></param>
        /// <param name="StopMethod"></param>
        /// <param name="RestrictLo"></param>
        /// <param name="RestrictHi"></param>
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        /// <param name="ex"></param>
        /// <param name="maxStep"></param>
        public static void Move(NanoStageInfo mover, Action StopMethod, double RestrictLo, double RestrictHi, double target, int timeout, double maxStep = 0.1)
        {
            return;
            bool result = mover.Use(true, true);
            if (result == false)
            {
                MessageLogger.AddLogger("设备", "设备" + mover.Parent.Device.ProductName + "的" + mover.Device.AxisName + "被占用", MessageTypes.Warning);
                throw new Exception("设备" + mover.Parent.Device.ProductName + "的" + mover.Device.AxisName + "被占用");
            }
            if (target > RestrictHi || target < RestrictLo)
            {
                MessageLogger.AddLogger("设备", "位移台" + Enum.GetName(typeof(MoverTypes), mover.MoverType) + "超量程", MessageTypes.Warning);
                throw new Exception("位移台" + Enum.GetName(typeof(MoverTypes), mover.MoverType) + "超量程");
            }
            try
            {
                MoveWithoutUse(mover, StopMethod, RestrictLo, RestrictHi, target, timeout, maxStep);
                mover.UnUse();
            }
            catch (Exception e)
            {
                mover.UnUse();
                throw e;
            }
        }
    }
}
