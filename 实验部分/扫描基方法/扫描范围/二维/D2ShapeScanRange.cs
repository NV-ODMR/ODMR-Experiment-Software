using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Point = System.Windows.Point;

namespace ODMR_Lab.实验部分.扫描基方法.扫描范围.二维
{

    public abstract class D2ShapeScanRangeBase : D2ScanRangeBase
    {

        protected LinearRing RingShape = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="XCount"></param>
        /// <param name="YCount"></param>
        /// <param name="ps">多边形轮廓点（逆时针排列）(点数大于3,必须封闭)</param>
        public D2ShapeScanRangeBase(int XCount, int YCount, bool ReverseX, bool ReverseY, bool IsXFast, List<Point> ps)
        {
            var p = new List<Coordinate>();
            foreach (var item in ps)
            {
                p.Add(new Coordinate(item.X, item.Y));
            }
            RingShape = new LinearRing(p.ToArray());
            XLo = ps.Min(x => x.X);
            XHi = ps.Max(x => x.X);
            YLo = ps.Min(x => x.Y);
            YHi = ps.Max(x => x.Y);
            this.ReverseX = ReverseX;
            this.ReverseY = ReverseY;
            IsXFastAxis = IsXFast;
            XScans = new D1LinearScanRange(XLo, XHi, XCount, false).ScanPoints;
            YScans = new D1LinearScanRange(YLo, YHi, XCount, false).ScanPoints;
            ScanPoints = GeneratePointList();
        }

    }

    /// <summary>
    /// 二维多边形扫描范围
    /// </summary>
    public class D2ShapeScanRange : D2ShapeScanRangeBase
    {
        public override string ScanName { get; protected set; } = "特殊形状同向扫描";

        private LinearRing RingShape = null;

        public D2ShapeScanRange(int XCount, int YCount, bool ReverseX, bool ReverseY, bool IsXFast, List<Point> ps) : base(XCount, YCount, ReverseX, ReverseY, IsXFast, ps)
        {
        }

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
                        NetTopologySuite.Geometries.Point newp = new NetTopologySuite.Geometries.Point(FastAxis[j], SlowAxis[i]);
                        if (RingShape.Contains(newp))
                            ps.Add(new TaggedPoint(FastAxis[j], SlowAxis[i], tag));
                    }
                    else
                    {
                        NetTopologySuite.Geometries.Point newp = new NetTopologySuite.Geometries.Point(FastAxis[j], SlowAxis[i]);
                        if (RingShape.Contains(newp))
                            ps.Add(new TaggedPoint(SlowAxis[i], FastAxis[j], tag));
                    }
                }
                tag = !tag;
            }
            return ps;
        }
    }

    /// <summary>
    /// 二维多边形扫描范围
    /// </summary>
    public class D2ShapeReverseScanRange : D2ShapeScanRangeBase
    {
        public D2ShapeReverseScanRange(int XCount, int YCount, bool ReverseX, bool ReverseY, bool IsXFast, List<Point> ps) : base(XCount, YCount, ReverseX, ReverseY, IsXFast, ps)
        {
        }

        public override string ScanName { get; protected set; } = "特殊形状正反向扫描";

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
                        NetTopologySuite.Geometries.Point newp = new NetTopologySuite.Geometries.Point(reverse ? FastAxisRev[j] : FastAxis[j], SlowAxis[i]);
                        if (RingShape.Contains(newp))
                            ps.Add(new TaggedPoint(reverse ? FastAxisRev[j] : FastAxis[j], SlowAxis[i], tag));
                    }
                    else
                    {
                        NetTopologySuite.Geometries.Point newp = new NetTopologySuite.Geometries.Point(SlowAxis[i], reverse ? FastAxisRev[j] : FastAxis[j]);
                        if (RingShape.Contains(newp))
                            ps.Add(new TaggedPoint(SlowAxis[i], reverse ? FastAxisRev[j] : FastAxis[j], tag));
                    }
                }
                tag = !tag;
                reverse = !reverse;
            }
            return ps;
        }
    }
}
