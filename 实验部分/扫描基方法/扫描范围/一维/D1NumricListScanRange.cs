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
    public class D1NumricListScanRange : D1NumricScanRangeBase
    {
        public override string ScanName { get; protected set; } = "一维数值列表扫描";

        public D1NumricListScanRange(List<double> ps)
        {
            Lo = ps.Min();
            Hi = ps.Max();
            Counts = ps.Count;
            Reverse = false;
            ScanPoints = ps;
        }
        public override string GetDescription()
        {
            string ss = "扫描点:\n";
            foreach (var item in ScanPoints)
            {
                ss += item.ToString() + "\n";
            }
            ss += "点数:" + Counts.ToString() + "\n" + "反向:" + Reverse.ToString();
            return ss;
        }
    }
}
