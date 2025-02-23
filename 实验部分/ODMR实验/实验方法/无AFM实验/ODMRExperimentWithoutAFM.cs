using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.Lock_In;
using ODMR_Lab.ODMR实验;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM
{
    public abstract class ODMRExperimentWithoutAFM : ODMRExpObject
    {
        public override void ODMRExperiment()
        {
            ODMRExpWithoutAFM();
        }

        public abstract void ODMRExpWithoutAFM();

        public override void PreExpEvent()
        {
            PreExpEventWithoutAFM();
        }

        /// <summary>
        /// 实验前要进行的操作(不下针)
        /// </summary>
        public abstract void PreExpEventWithoutAFM();

        public override void AfterExpEvent()
        {
            AfterExpEventWithoutAFM();
        }

        /// <summary>
        /// 实验后要进行的操作(不下针)
        /// </summary>
        public abstract void AfterExpEventWithoutAFM();

    }
}
