using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法
{
    public class ExperimentHelper
    {
        /// <summary>
        /// 获取删除掉NAN数据后的数据
        /// </summary>
        public static void GetNonNaNData(List<double> xs, List<double> ys, out List<double> outxs, out List<double> outys)
        {
            var inds = ys.Select((x, ind) => { if (!double.IsNaN(x)) return ind; else return double.NaN; }).Where((x) => !double.IsNaN(x));

            outxs = inds.Select((x) => xs[(int)x]).ToList();
            outys = inds.Select((x) => ys[(int)x]).ToList();
        }
    }
}
