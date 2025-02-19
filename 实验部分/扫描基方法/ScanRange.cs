using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 范围
    /// </summary>
    public class ScanRange
    {
        public double Lo { get; set; }

        public double Hi { get; set; }

        public int Count { get; set; }

        public ScanRange(double lo, double hi, int count)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(lo, hi);
            Count = count;
        }

        public List<double> GenerateScanList()
        {
            return Enumerable.Range(0, Count).Select(x => Lo + Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
        }
        public List<double> GenerateReversedScanList()
        {
            return Enumerable.Range(0, Count).Select(x => Hi - Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
        }
    }
}
