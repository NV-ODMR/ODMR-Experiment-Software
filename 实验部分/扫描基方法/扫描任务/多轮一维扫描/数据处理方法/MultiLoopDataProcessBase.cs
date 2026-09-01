using System.Collections.Generic;

namespace ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法
{
    internal abstract class MultiLoopDataProcessBase
    {
        /// <summary>
        /// 标准差获取方法
        /// </summary>
        public abstract double GetSigma(List<double> data, List<MultiLoopScanData> otherdata);

        /// <summary>
        /// 平均值获取方法
        /// </summary>
        public abstract double GetAverage(List<double> data, List<MultiLoopScanData> otherdata);

        /// <summary>
        /// 平均值获取方法
        /// </summary>
        public abstract double GetSum(List<double> data, List<MultiLoopScanData> otherdata);
    }
}
