using Controls.Windows;
using MathLib.NormalMath.Decimal;
using MathNet.Numerics.Distributions;
using ODMR_Lab.IO操作;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ODMR_Lab.实验部分.自定义算法.算法列表
{
    internal class CWCalculate : AlgorithmBase
    {
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("CW谱峰位置1（MHz）",double.NaN,"CW1"),
            new Param<double>("CW谱峰位置2（MHz）",double.NaN,"CW2"),
            new Param<double>("零场谱峰位置(MHz)",double.NaN,"ZeroCW"),
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override string AlgorithmName { get; } = "连续波谱峰反推计算磁场";

        public override string AlgorithmDescription { get; } = "此算法通过输入NV连续波谱测量到的谱峰来计算对应的沿轴和垂直轴的磁场分量";

        public override void CalculateFunc()
        {
            CalculateB(GetInputParamValueByName("CW1"), GetInputParamValueByName("CW2"), GetInputParamValueByName("ZeroCW"), out double bp, out double bv, out double b);
            OutputParams.Add(new Param<double>("沿轴磁场(Gauss)", bp, "Bp"));
            OutputParams.Add(new Param<double>("垂直轴磁场(Gauss)", bv, "Bv"));
            OutputParams.Add(new Param<double>("总磁场(Gauss)", b, "B"));
        }

        // 旋磁比(Mhz/Gauss)
        private static double gammaE = 2.803;
        /// <summary>
        /// 根据谱峰位置计算横向和纵向的磁场大小
        /// </summary>
        /// <param name="freq1"></param>
        /// <param name="freq2"></param>
        /// <param name="D"></param>
        /// <param name="Bp"></param>
        /// <param name="Bv"></param>
        private void CalculateB(double freq1, double freq2, double D, out double Bp, out double Bv, out double B)
        {
            double detplus = (freq2 + freq1) / 2;
            double detminus = Math.Abs(freq2 - freq1);
            double det1 = 2 * D * (detplus - D) / 3;
            if (det1 < 0)
                det1 = 0;
            Bv = Math.Sqrt(det1) / gammaE;
            double det2 = Math.Pow(detminus / (2 * gammaE), 2) - Math.Pow(Bv * Bv * gammaE / (2 * D), 2);
            if (det2 < 0)
                det2 = 0;
            Bp = Math.Sqrt(det2);
            B = Math.Sqrt(Bp * Bp + Bv * Bv);
        }
    }
}
