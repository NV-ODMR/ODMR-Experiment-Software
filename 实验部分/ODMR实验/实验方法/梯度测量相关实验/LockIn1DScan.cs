using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.梯度测量相关实验
{
    internal class LockIn1DScan : AFMScan1DExp
    {
        public override string ODMRExperimentName { get; set; } = "一维锁相AFM扫描";

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override bool IsDisplayAsExp { get; set; } = false;

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
                new AutoTrace(),
                new LockInHahnEcho()
            };
        }
    }
}
