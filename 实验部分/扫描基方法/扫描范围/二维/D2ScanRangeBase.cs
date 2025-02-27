using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 二维扫描范围基类
    /// </summary>
    public abstract class D2ScanRangeBase
    {
        public abstract string ScanName { get; protected set; }

        public double XLo { get; protected set; } = 0;

        public double XHi { get; protected set; } = 1;

        public double YLo { get; protected set; } = 0;

        public double YHi { get; protected set; } = 1;

        public int XCount { get; protected set; } = 0;

        public int YCount { get; protected set; } = 0;

        public bool ReverseX { get; set; } = false;

        public bool ReverseY { get; set; } = false;

        public bool IsXFastAxis { get; set; } = true;

        public List<TaggedPoint> ScanPoints { get; protected set; } = new List<TaggedPoint>();

        protected List<double> XScans = new List<double>();

        protected List<double> YScans = new List<double>();


        /// <summary>
        /// 返回和输入值最接近的列表的序号(x序号)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestXIndex(double xloc)
        {
            var l = XScans.Select(x => Math.Abs(x - xloc)).ToList();
            return l.IndexOf(l.Min());
        }

        /// <summary>
        /// 生成范围描述
        /// </summary>
        /// <returns></returns>
        public abstract string GetDescription();


        /// <summary>
        /// 返回和输入值最接近的列表的序号(x序号)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestYIndex(double yloc)
        {
            var l = YScans.Select(x => Math.Abs(x - yloc)).ToList();
            return l.IndexOf(l.Min());
        }

        /// <summary>
        /// 生成点集
        /// </summary>
        /// <returns></returns>
        public abstract List<TaggedPoint> GeneratePointList();
    }

    /// <summary>
    /// 带标签的点（每隔一行标签变化一次，用于区分新行）
    /// </summary>
    public class TaggedPoint
    {
        public Point LocPoint { get; set; } = new Point();

        public bool Tag { get; set; } = false;

        public TaggedPoint(Point locPoint, bool tag)
        {
            LocPoint = locPoint;
            Tag = tag;
        }

        public TaggedPoint(double x, double y, bool tag)
        {
            LocPoint = new Point(x, y);
            Tag = tag;
        }
    }
}
