using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.设备部分.射频源_锁相放大器;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM
{
    public abstract class ODMRExperimentWithAFM : ODMRExpObject
    {
        public override void ODMRExperiment()
        {
            ODMRExpWithAFM();
        }

        public abstract void ODMRExpWithAFM();

        public override void AfterExpEvent()
        {
            AfterExpEventWithAFM();
            SetExpState("正在撤针...");
            //撤针
            AFMStopDrop drop = new AFMStopDrop();
            drop.CoreMethod(new List<object>(), GetLockIn());
            SetExpState("");
        }

        /// <summary>
        /// 实验结束后,AFM撤针前,要进行的操作
        /// </summary>
        public abstract void AfterExpEventWithAFM();

        public override void PreExpEvent()
        {
            PreExpEventBeforeDropWithAFM();
            SetExpState("正在下针...");
            //下针
            AFMDrop drop = new AFMDrop();
            var result = drop.CoreMethod(new List<object>(), GetLockIn());
            if ((bool)result[0] == false)
            {
                ///没下到针
                throw new Exception("下针未完成,PID输出已经到达最大值");
            }
            PreExpEventWithAFM();
        }

        /// <summary>
        /// AFM下针后,实验开始前要进行的操作
        /// </summary>
        public abstract void PreExpEventWithAFM();

        /// <summary>
        /// AFM下针前,实验开始前要进行的操作
        /// </summary>
        public abstract void PreExpEventBeforeDropWithAFM();

        /// <summary>
        /// 获取AFM所需的LockIn设备
        /// </summary>
        /// <returns></returns>
        public abstract LockinInfo GetLockIn();
    }
}
