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
    public class PulseResultPack
    {
        /// <summary>
        /// 对比度
        /// </summary>
        public double Contrast { get; private set; } = double.NaN;

        /// <summary>
        /// 布居度
        /// </summary>
        public double P { get; private set; } = double.NaN;

        /// <summary>
        /// 亮态光子数
        /// </summary>
        public double LightStatePhoton { get; private set; } = double.NaN;

        /// <summary>
        /// 暗态光子数
        /// </summary>
        public double DarkStatePhoton { get; private set; } = double.NaN;

        /// <summary>
        /// 信号光子数
        /// </summary>
        public double SignalPhoton { get; private set; } = double.NaN;

        public PulseResultPack(double contrast, double p, double lightStatePhoton, double darkStatePhoton, double signalPhoton)
        {
            Contrast = contrast;
            P = p;
            LightStatePhoton = lightStatePhoton;
            DarkStatePhoton = darkStatePhoton;
            SignalPhoton = signalPhoton;
        }
    }
}
