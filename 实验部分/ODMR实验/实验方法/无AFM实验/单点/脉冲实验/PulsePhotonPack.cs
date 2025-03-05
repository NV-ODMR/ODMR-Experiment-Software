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
        public List<SinglePulsePhotonPack> PulsesPhotons { get; set; } = new List<SinglePulsePhotonPack>();

        /// <summary>
        /// 获取所有次实验的指定序号的光子计数
        /// </summary>
        /// <returns></returns>
        public List<int> GetPhotonsAtIndex(int index)
        {
            return PulsesPhotons.Select(x => x.Photons[index]).ToList();
        }
    }

    public class SinglePulsePhotonPack
    {
        /// <summary>
        /// 单次实验的光子数
        /// </summary>
        public List<int> Photons { get; set; } = new List<int>();
    }
}
