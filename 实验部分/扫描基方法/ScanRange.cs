using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 范围
    /// </summary>
    public class ScanRange
    {
        public double Lo { get; set; }

        public double Hi { get; set; }

        public int Count { get; set; }

        public bool Reverse { get; set; }

        public ScanRange(double lo, double hi, int count, bool reverse = false)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(lo, hi);
            Count = count;
            Reverse = reverse;
        }

        public ScanRange(double lo, double hi, double step, bool reverse = false)
        {
            Lo = Math.Min(lo, hi);
            Hi = Math.Max(lo, hi);
            Count = (int)((Hi - Lo) / step) + 1;
            Reverse = reverse;
        }

        /// <summary>
        /// 生成扫描序列,根据Reverse属性来确定扫描列表是正向还是反向
        /// </summary>
        /// <returns></returns>
        public List<double> GenerateScanList()
        {
            if (!Reverse)
                return GenSeq();
            else
                return GenRevSeq();
        }

        private List<double> GenSeq()
        {
            return Enumerable.Range(0, Count).Select(x => Lo + Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
        }

        private List<double> GenRevSeq()
        {
            return Enumerable.Range(0, Count).Select(x => Hi - Math.Abs(Hi - Lo) * x / (Count - 1)).ToList();
        }

        /// <summary>
        /// 返回和输入值最接近的正向扫描列表的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetNearestIndex(double value)
        {
            var l = GenSeq().Select(x => Math.Abs(x - value)).ToList();
            return l.IndexOf(l.Min());
        }
    }
}
