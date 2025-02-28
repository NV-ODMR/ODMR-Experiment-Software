using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 一维扫描范围
    /// </summary>
    public class D1LinearScanRange : D1ScanRangeBase
    {

        public bool Reverse { get; private set; }
        public override string ScanName { get; protected set; } = "一维线性扫描";

        public D1LinearScanRange(double lo, double hi, int count, bool reverse = false)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(lo, hi);
            int Count = count;
            Reverse = reverse;
            if (Reverse)
                ScanPoints = Enumerable.Range(0, Count).Select(x => Lo + Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
            else
                ScanPoints = Enumerable.Range(0, Count).Select(x => Hi - Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
        }

        public D1LinearScanRange(double lo, double hi, double step, bool reverse = false)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(lo, hi);
            int Count = (int)((Hi - Lo) / step) + 1;
            Reverse = reverse;
            if (Reverse)
                ScanPoints = Enumerable.Range(0, Count).Select(x => Lo + Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
            else
                ScanPoints = Enumerable.Range(0, Count).Select(x => Hi - Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
        }

        public override string GetDescription()
        {
            return "扫描范围:" + Math.Round(Lo, 5).ToString() + "—" + Math.Round(Hi, 5).ToString() + "点数:" + Counts.ToString();
        }
    }
}
