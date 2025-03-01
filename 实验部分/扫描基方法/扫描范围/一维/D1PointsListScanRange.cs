using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法.扫描范围
{
    internal class D1PointsListScanRange : D1PointsScanRangeBase
    {
        public D1PointsListScanRange(List<double> points)
        {
            ScanPoints = points.Where(x => !double.IsNaN(x)).Select(v => new Point(0, v)).ToList();
            StartPoint = new Point(0, points.Min());
            EndPoint = new Point(0, points.Max());
            Counts = ScanPoints.Count;
        }

        public override string ScanName { get; protected set; } = "一维点列表扫描";

        public override string GetDescription()
        {
            string des = "扫描总点数:" + Counts.ToString() + "\n";
            des += "扫描值:\n";
            foreach (var item in ScanPoints)
            {
                des += item.Y.ToString() + "\n";
            }
            return des;
        }
    }
}
