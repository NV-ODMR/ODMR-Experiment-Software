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
    internal class StandardDataProcess : MultiLoopDataProcessBase
    {
        public override double GetAverage(List<double> data, List<MultiLoopScanData> otherdata)
        {
            var nonnan = data.Where((x) => !double.IsNaN(x));
            if (nonnan.Count() == 0) return double.NaN;
            return data.Where((x) => !double.IsNaN(x)).Average();
        }

        public override double GetSigma(List<double> data, List<MultiLoopScanData> otherdata)
        {
            var nonnan = data.Where((x) => !double.IsNaN(x));
            if (nonnan.Count() == 0) return double.NaN;
            double ave = nonnan.Average();
            double sum = nonnan.Sum(x => (x - ave) * (x - ave));
            return sum / nonnan.Count();
        }

        public override double GetSum(List<double> data, List<MultiLoopScanData> otherdata)
        {
            var nonnan = data.Where((x) => !double.IsNaN(x));
            if (nonnan.Count() == 0) return double.NaN;
            return nonnan.Sum();
        }
    }
}
