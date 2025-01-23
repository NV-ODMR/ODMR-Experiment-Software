using System;
using System.IO;
using MathNet.Numerics.RootFinding;
using PythonHandler;

namespace ODMR_Lab.实验部分.磁场调节
{
    /// <summary>
    /// 圆柱形磁铁类
    /// </summary>
    public class PillarMagnetHelper
    {

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
        public static void CalculateB(double freq1, double freq2, out double Bp, out double Bv, double D = 2870)
        {
            double detplus = (freq2 + freq1) / 2;
            double detminus = Math.Abs(freq2 - freq1);
            double det1 = 2 * D * (detplus - D) / 3;
            if (det1 < 0)
                det1 = 0;
            Bv = Math.Sqrt(det1) / gammaE;
            double det2 = detminus / Math.Pow(2 * gammaE, 2) - Math.Pow(Bv * Bv * gammaE / (2 * D), 2);
            if (det2 < 0)
                det2 = 0;
            Bp = Math.Sqrt(det2);
        }

        private static double MagFun(PillarMagnet M, double x, double dist, double theta, double phi)
        {
            double ratio = M.GetNVField(x, 0, 0, theta, phi) / M.GetNVField(x + dist, 0, 0, theta, phi);
            return ratio;
        }

        /// <summary>
        /// 寻找Z方向的根
        /// </summary>
        /// <param name="M"></param>
        /// <param name="z1"></param>
        /// <param name="z2"></param>
        /// <param name="ratiovalue"></param>
        /// <returns></returns>
        public static double FindRoot(PillarMagnet M, double z1, double z2, double ratiovalue)
        {
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "LabviewHandler", "Magnet.py"), "FindRoot", TimeSpan.FromSeconds(5), M.Radius, M.Length, z1, z2, ratiovalue);
            return result;
        }

    }
}
