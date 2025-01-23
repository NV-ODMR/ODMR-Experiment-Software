using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ODMR_Lab.实验部分.磁场调节
{
    /// <summary>
    /// CW实验结果，包含数据点和CW谱截图
    /// </summary>
    public class CWPointObject
    {
        /// <summary>
        /// CW谱1位置
        /// </summary>
        public double CW1 { get; private set; } = 0;
        /// <summary>
        /// CW谱2位置
        /// </summary>
        public double CW2 { get; private set; } = 0;

        public List<double> CW1Freqs { get; private set; } = new List<double>();

        public List<double> CW1Values { get; private set; } = new List<double>();

        public List<double> CW2Freqs { get; private set; } = new List<double>();

        public List<double> CW2Values { get; private set; } = new List<double>();

        /// <summary>
        /// 沿轴磁场
        /// </summary>
        public double Bp { get; private set; } = 0;
        /// <summary>
        /// 垂直磁场
        /// </summary>
        public double Bv { get; private set; } = 0;

        /// <summary>
        /// 垂直磁场
        /// </summary>
        public double B { get; private set; } = 0;

        /// <summary>
        /// 位移台位置
        /// </summary>
        public double MoverLoc { get; set; } = 0;

        public CWPointObject(double loc, double cw1, double cw2, double D, List<double> freqs1, List<double> values1, List<double> freqs2, List<double> values2)
        {
            MoverLoc = loc;
            MagnetAutoScanHelper.CalculateB(cw1, cw2, out double bp, out double bv, D: D);
            Bp = bp;
            Bv = bv;
            B = Math.Sqrt(bp * bp + bv * bv);
            CW1 = cw1;
            CW2 = cw2;
            CW1Freqs = freqs1;
            CW1Values = values1;
            CW2Freqs = freqs2;
            CW2Values = values2;
        }
    }
}
