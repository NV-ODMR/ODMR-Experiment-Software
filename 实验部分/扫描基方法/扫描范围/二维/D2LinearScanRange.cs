using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法.扫描范围
{
    public abstract class D2LinearScanRangeBase : D2ScanRangeBase
    {
        public D2LinearScanRangeBase(double xlo, double xhi, int xcount, double ylo, double yhi, int ycount, bool reverseX, bool reverseY, bool IsXFast)
        {
            var r1 = new D1NumricLinearScanRange(xlo, xhi, xcount, false);
            XScans = r1.ScanPoints;
            var r2 = new D1NumricLinearScanRange(ylo, yhi, ycount, false);
            YScans = r2.ScanPoints;
            ReverseX = reverseX;
            ReverseY = reverseY;
            XCount = xcount;
            YCount = ycount;
            IsXFastAxis = IsXFast;
            ScanPoints = GeneratePointList();
        }

        public override string GetDescription()
        {
            return "X: " + Math.Round(XLo, 5).ToString() + "—" + Math.Round(XHi, 5).ToString() + "  点数:" + XCount.ToString() + "\n" +
                "Y: " + Math.Round(YLo, 5).ToString() + "—" + Math.Round(YHi, 5).ToString() + "  点数:" + YCount.ToString() + "\n"
                + "X反向:" + ReverseX.ToString() + " , " + "Y反向:" + ReverseY.ToString() + "\n" + "X为快轴:" + IsXFastAxis.ToString() + "\n";
        }

        public D2LinearScanRangeBase(double xlo, double xhi, double xstep, double ylo, double yhi, double ystep, bool reverseX, bool reverseY, bool IsXFast)
        {
            var r1 = new D1NumricLinearScanRange(xlo, xhi, xstep, false);
            XScans = r1.ScanPoints;
            var r2 = new D1NumricLinearScanRange(ylo, yhi, ystep, false);
            YScans = r2.ScanPoints;
            ReverseX = reverseX;
            ReverseY = reverseY;
            IsXFastAxis = IsXFast;
            ScanPoints = GeneratePointList();
        }
    }

    /// <summary>
    /// 二维正反向扫描类型
    /// </summary>
    public class D2LinearReverseScanRange : D2LinearScanRangeBase
    {
        public D2LinearReverseScanRange(double xlo, double xhi, int xcount, double ylo, double yhi, int ycount, bool reverseX, bool reverseY, bool IsXFast) : base(xlo, xhi, xcount, ylo, yhi, ycount, reverseX, reverseY, IsXFast)
        {
        }

        public D2LinearReverseScanRange(double xlo, double xhi, double xstep, double ylo, double yhi, double ystep, bool reverseX, bool reverseY, bool IsXFast) : base(xlo, xhi, xstep, ylo, yhi, ystep, reverseX, reverseY, IsXFast)
        {
        }

        public override string ScanName { get; protected set; } = "正反向扫描";

        public override List<TaggedPoint> GeneratePointList()
        {
            List<double> xs = XScans.ToArray().ToList();
            List<double> ys = YScans.ToArray().ToList();
            if (ReverseX)
            {
                xs.Reverse();
            }
            if (ReverseY)
            {
                ys.Reverse();
            }
            List<double> SlowAxis = new List<double>();
            List<double> FastAxis = new List<double>();
            List<double> FastAxisRev = new List<double>();
            if (IsXFastAxis)
            {
                SlowAxis = ys;
                FastAxis = xs;
                FastAxisRev = xs.ToArray().ToList();
                FastAxisRev.Reverse();
            }
            else
            {
                SlowAxis = xs;
                FastAxis = ys;
                FastAxisRev = ys.ToArray().ToList();
                FastAxisRev.Reverse();
            }
            bool reverse = false;
            List<TaggedPoint> ps = new List<TaggedPoint>();
            bool tag = true;
            for (int i = 0; i < SlowAxis.Count; i++)
            {
                for (int j = 0; j < FastAxis.Count; j++)
                {
                    if (IsXFastAxis)
                    {
                        ps.Add(new TaggedPoint(reverse ? FastAxisRev[j] : FastAxis[j], SlowAxis[i], tag));
                    }
                    else
                    {
                        ps.Add(new TaggedPoint(SlowAxis[i], reverse ? FastAxisRev[j] : FastAxis[j], tag));
                    }
                }
                tag = !tag;
                reverse = !reverse;
            }
            return ps;
        }
    }

    /// <summary>
    /// 二维同向扫描类型
    /// </summary>
    public class D2LinearScanRange : D2LinearScanRangeBase
    {
        public D2LinearScanRange(double xlo, double xhi, int xcount, double ylo, double yhi, int ycount, bool reverseX, bool reverseY, bool IsXFast) : base(xlo, xhi, xcount, ylo, yhi, ycount, reverseX, reverseY, IsXFast)
        {
        }

        public D2LinearScanRange(double xlo, double xhi, double xstep, double ylo, double yhi, double ystep, bool reverseX, bool reverseY, bool IsXFast) : base(xlo, xhi, xstep, ylo, yhi, ystep, reverseX, reverseY, IsXFast)
        {
        }

        public override string ScanName { get; protected set; } = "同向扫描";

        public override List<TaggedPoint> GeneratePointList()
        {
            List<double> xs = XScans.ToArray().ToList();
            List<double> ys = YScans.ToArray().ToList();
            if (ReverseX)
            {
                xs.Reverse();
            }
            if (ReverseY)
            {
                ys.Reverse();
            }
            List<double> SlowAxis = new List<double>();
            List<double> FastAxis = new List<double>();
            if (IsXFastAxis)
            {
                SlowAxis = ys;
                FastAxis = xs;
            }
            else
            {
                SlowAxis = xs;
                FastAxis = ys;
            }
            List<TaggedPoint> ps = new List<TaggedPoint>();
            bool tag = true;
            for (int i = 0; i < SlowAxis.Count; i++)
            {
                for (int j = 0; j < FastAxis.Count; j++)
                {
                    if (IsXFastAxis)
                    {
                        ps.Add(new TaggedPoint(FastAxis[j], SlowAxis[i], tag));
                    }
                    else
                    {
                        ps.Add(new TaggedPoint(SlowAxis[i], FastAxis[j], tag));
                    }
                }
                tag = !tag;
            }
            return ps;
        }
    }
}
