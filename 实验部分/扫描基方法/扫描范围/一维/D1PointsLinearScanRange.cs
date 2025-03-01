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
    public class D1PointsLinearScanRange : D1PointsScanRangeBase
    {
        public override string ScanName { get; protected set; } = "一维线性扫描";

        public D1PointsLinearScanRange(Point start, Point end, int count, bool reverse = false)
        {
            StartPoint = start;
            EndPoint = end;
            Counts = count;
            Reverse = reverse;
            var scanlist = Enumerable.Range(0, Counts).Select(x => x / (Counts - 1)).ToList();
            double length = (StartPoint - EndPoint).Length;

            if (Reverse)
            {
                Vector unitvec = StartPoint - EndPoint;
                unitvec.Normalize();
                ScanPoints = scanlist.Select(x => EndPoint + unitvec * x * length).ToList();
            }
            else
            {
                Vector unitvec = EndPoint - StartPoint;
                unitvec.Normalize();
                ScanPoints = scanlist.Select(x => StartPoint + unitvec * x * length).ToList();
            }
        }

        public D1PointsLinearScanRange(Point start, Point end, double step, bool reverse = false)
        {
            StartPoint = start;
            EndPoint = end;
            int Count = (int)((end - start).Length / step) + 1;
            Reverse = reverse;

            var scanlist = Enumerable.Range(0, Count).Select(x => x / (Count - 1)).ToList();
            double length = (StartPoint - EndPoint).Length;

            if (Reverse)
            {
                Vector unitvec = StartPoint - EndPoint;
                unitvec.Normalize();
                ScanPoints = scanlist.Select(x => EndPoint + unitvec * x * length).ToList();
            }
            else
            {
                Vector unitvec = EndPoint - StartPoint;
                unitvec.Normalize();
                ScanPoints = scanlist.Select(x => StartPoint + unitvec * x * length).ToList();
            }
        }

        public override string GetDescription()
        {
            return "扫描起始点 X: " + Math.Round(StartPoint.X, 5).ToString() + " Y: " + Math.Round(StartPoint.Y, 5).ToString() + "\n" +
                "扫描中止点 X: " + Math.Round(EndPoint.X, 5).ToString() + " Y: " + Math.Round(EndPoint.Y, 5).ToString() + "\n" +
                "点数:" + Counts.ToString() + "\n" +
                "反向:" + Reverse.ToString();
        }
    }
}
