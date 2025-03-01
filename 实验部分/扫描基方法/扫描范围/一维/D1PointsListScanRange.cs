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
        public D1PointsListScanRange(List<Point> points)
        {
            ScanPoints = points.ToArray().ToList();
            StartPoint = points.First();
            EndPoint = points.Last();
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
