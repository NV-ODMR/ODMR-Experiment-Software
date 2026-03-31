using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    /// <summary>
    /// 单次脉冲实验的光子计数结果
    /// </summary>
    public class PulsePhotonPack
    {
        //试验光子数,外层为实验的光子计数序号,内层为不同的实验
        public List<List<int>> PulsesPhotons { get; set; } = new List<List<int>>();

        /// <summary>
        /// 获取所有次实验的指定序号的光子计数
        /// </summary>
        /// <returns></returns>
        public List<int> GetPhotonsAtIndex(int index)
        {
            return PulsesPhotons.Select(x => x[index]).ToList();
        }

        public List<KeyValuePair<int, int>> GetPhotonStatic(int index, int dividetimes)
        {
            var photonpack = GetPhotonsAtIndex(index);
            var processed = photonpack
                 .Select((value, ind) => new { value, ind })
                 .GroupBy(x => x.ind / dividetimes)
                 .Select(g => g.Sum(x => x.value))
                 .ToLookup(n => n);
            return processed.Select(x => new KeyValuePair<int, int>(x.Key, x.Count())).ToList();
        }

        public static List<KeyValuePair<int, int>> AddStatic(List<KeyValuePair<int, int>> add1, List<KeyValuePair<int, int>> add2)
        {
            if (add2.Count == 0) return add1;
            if (add1.Count == 0) return add2;
            var key1 = add1.Select(x => x.Key).ToList();
            var v1 = add1.Select(x => x.Value).ToList();
            var key2 = add2.Select(x => x.Key).ToList();
            var v2 = add2.Select(x => x.Value).ToList();
            var hashset = key1.Concat(key2).ToHashSet().ToList();
            hashset.Sort();
            List<KeyValuePair<int, int>> result = new List<KeyValuePair<int, int>>();
            foreach (var item in hashset)
            {
                result.Add(new KeyValuePair<int, int>(item, (key1.Contains(item) ? v1[key1.IndexOf(item)] : 0) + (key2.Contains(item) ? v2[key2.IndexOf(item)] : 0)));
            }
            return result;
        }
    }
}
