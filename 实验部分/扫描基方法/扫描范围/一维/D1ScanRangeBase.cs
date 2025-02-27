using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 一维扫描范围基类
    /// </summary>
    public class D1ScanRangeBase
    {
        /// <summary>
        /// 扫描点
        /// </summary>
        public List<double> ScanPoints { get; protected set; } = new List<double>();

        public double Lo { get; protected set; } = 0;

        public double Hi { get; protected set; } = 1;

        public int Counts { get; protected set; } = 1;

        /// <summary>
        /// 返回和输入值最接近的正向扫描列表的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestIndex(double value)
        {
            var l = ScanPoints.Select(x => Math.Abs(x - value)).ToList();
            return l.IndexOf(l.Min());
        }
    }
}
