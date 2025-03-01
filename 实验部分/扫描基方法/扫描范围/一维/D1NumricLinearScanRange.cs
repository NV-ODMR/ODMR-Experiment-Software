using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法.扫描范围
{
    /// <summary>
    /// 一维扫描范围
    /// </summary>
    public class D1NumricLinearScanRange : D1NumricScanRangeBase
    {
        public override string ScanName { get; protected set; } = "一维线性扫描";

        public D1NumricLinearScanRange(double lo, double hi, int count, bool reverse = false)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(hi, lo);
            Counts = count;
            Reverse = reverse;
            if (reverse)
            {
                ScanPoints = Enumerable.Range(0, Counts).Select(x => hi + (lo - hi) * x / (Counts - 1)).ToList();
            }
            else
            {
                ScanPoints = Enumerable.Range(0, Counts).Select(x => lo + (hi - lo) * x / (Counts - 1)).ToList();
            }
        }

        public D1NumricLinearScanRange(double lo, double hi, double step, bool reverse = false)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(hi, lo);
            Counts = (int)(Math.Abs(hi - lo) / step) + 1;
            Reverse = reverse;

            if (reverse)
            {
                ScanPoints = Enumerable.Range(0, Counts).Select(x => hi + (hi - lo) * x / (Counts - 1)).ToList();
            }
            else
            {
                ScanPoints = Enumerable.Range(0, Counts).Select(x => lo + (hi - lo) * x / (Counts - 1)).ToList();
            }
        }

        public override string GetDescription()
        {
            return "扫描起始值: " + Math.Round(Lo, 5).ToString() + "\n" +
                "扫描中止值: " + Math.Round(Hi, 5).ToString() + "\n" +
                "点数:" + Counts.ToString() + "\n" +
                "反向:" + Reverse.ToString();
        }
    }
}
