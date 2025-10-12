using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ODMR_Lab.实验部分.磁场调节;
using System.Threading;
using System.Windows;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;

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
    }
}
