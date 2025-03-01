using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法.扫描范围
{
    public abstract class D1PointsScanRangeBase : D1ScanRangeBase
    {
        /// <summary>
        /// 扫描点
        /// </summary>
        public List<Point> ScanPoints { get; protected set; } = new List<Point>();

        public Point StartPoint { get; protected set; } = new Point();

        public Point EndPoint { get; protected set; } = new Point();

        /// <summary>
        /// 返回和输入值最接近的扫描列表的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestIndex(Point value)
        {
            var l = ScanPoints.Select(x => (x - value).Length).ToList();
            return l.IndexOf(l.Min());
        }
    }
}
