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
    public abstract class D1NumricScanRangeBase : D1ScanRangeBase
    {
        public double Lo { get; protected set; } = 0;

        public double Hi { get; protected set; } = 1;

        public List<double> ScanPoints { get; protected set; } = new List<double>();

        public override string GetDescription()
        {
            return "扫描起始值 X: " + Math.Round(Lo, 5).ToString() + "\n" +
                "扫描中值 X: " + Math.Round(Hi, 5).ToString() + "\n" +
                "点数:" + Counts.ToString() + "\n" +
                "反向:" + Reverse.ToString();
        }

        /// <summary>
        /// 返回和输入值最接近的扫描列表的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestIndex(double value)
        {
            var l = ScanPoints.Select(x => Math.Abs(x - value)).ToList();
            return l.IndexOf(l.Min());
        }

        /// <summary>
        /// 返回和输入值最接近的正向扫描列表的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestFormalIndex(double value)
        {
            var list = ScanPoints.ToArray().ToList();
            if (Reverse)
            {
                list.Reverse();
            }
            var l = list.Select(x => Math.Abs(x - value)).ToList();
            return l.IndexOf(l.Min());
        }
    }
}
