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
    }
}
