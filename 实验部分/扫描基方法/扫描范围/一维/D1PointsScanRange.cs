using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.扫描基方法
{
    internal class D1PointsScanRange : D1ScanRangeBase
    {
        public D1PointsScanRange(List<double> points)
        {
            ScanPoints = points.Where(x => !double.IsNaN(x)).ToList();
            Lo = points.Min();
            Hi = points.Max();
            Counts = ScanPoints.Count;
        }

        public override string ScanName { get; protected set; } = "一维点列表扫描";

        public override string GetDescription()
        {
            return "";
        }
    }
}
