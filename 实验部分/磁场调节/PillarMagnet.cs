using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using ODMR_Lab.Python.LbviewHandler;

namespace ODMR_Lab.实验部分.磁场调节
{
    /// <summary>
    /// 圆柱形磁铁(轴向为Z轴)
    /// </summary>
    public class PillarMagnet
    {
        /// <summary>
        /// 磁铁半径
        /// </summary>
        public double Radius { get; set; } = 1;

        /// <summary>
        /// 磁铁长度
        /// </summary>
        public double Length { get; set; } = 1;

        /// <summary>
        /// 磁化强度
        /// </summary>
        public double Magnetization { get; set; } = 1;

        /// <summary>
        /// 真空磁导率
        /// </summary>
        private double Miu = 4 * Math.PI * Math.Pow(10, -7);


        public PillarMagnet(double R, double L)
        {
            Radius = R;
            Length = L;
        }

        private double Getrou2(double x, double y)
        {
            return x * x + y * y;
        }
        private double Getr2(double x, double y, double z, double h)
        {
            return x * x + y * y + (z - h) * (z - h);
        }
        private double Getalpha2(double r0, double x, double y, double z, double h)
        {
            return r0 * r0 + Getr2(x, y, z, h) - 2 * r0 * Math.Sqrt(Getrou2(x, y));
        }

        private double Getbeta2(double r0, double x, double y, double z, double h)
        {
            return Getalpha2(r0, x, y, z, h) + 4 * r0 * Math.Sqrt(Getrou2(x, y));
        }
        private double Getk2(double r0, double x, double y, double z, double h)
        {
            return 1 - Getalpha2(r0, x, y, z, h) / Getbeta2(r0, x, y, z, h);
        }

        private double GetiG(double x, double y, double z, double h)
        {
            double alpha2 = Getalpha2(Radius, x, y, z, h);
            double beta2 = Getbeta2(Radius, x, y, z, h);
            double rou2 = Getrou2(x, y);
            double r2 = Getr2(x, y, z, h);
            double e = LabviewConverter.EllipseE(Getk2(Radius, x, y, z, h));
            double k = LabviewConverter.EllipseK(Getk2(Radius, x, y, z, h));
            return ((Radius * Radius + r2) * e - alpha2 * k) * (z - h) / (2 * alpha2 * Math.Sqrt(beta2) * rou2);
        }

        private double GetiBz(double x, double y, double z, double h)
        {
            double alpha2 = Getalpha2(Radius, x, y, z, h);
            double beta2 = Getbeta2(Radius, x, y, z, h);
            double rou2 = Getrou2(x, y);
            double r2 = Getr2(x, y, z, h);
            double e = LabviewConverter.EllipseE(Getk2(Radius, x, y, z, h));
            double k = LabviewConverter.EllipseK(Getk2(Radius, x, y, z, h));
            return ((Radius * Radius - r2) * e + alpha2 * k) / (2 * alpha2 * Math.Sqrt(beta2));
        }

        /// <summary>
        /// 获取三个分量的磁场
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public List<double> GetField(double x, double y, double z)
        {
            double bG = Integrate.GaussKronrod(new Func<double, double>((h) => GetiG(x, y, z, h)), -Length / 2, Length / 2);
            double bx = x * bG;
            double by = y * bG;
            double bz = Integrate.GaussKronrod(new Func<double, double>((h) => GetiBz(x, y, z, h)), -Length / 2, Length / 2);
            double temp = Miu / Math.PI * Magnetization;
            return new List<double>() { bx * temp, by * temp, bz * temp };
        }

        /// <summary>
        /// 获取沿NV轴磁场
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="theta"></param>
        /// <param name="phi"></param>
        /// <returns></returns>
        public double GetNVField(double x, double y, double z, double theta, double phi)
        {
            List<double> B = GetField(x, y, z);
            return B[0] * Math.Cos(phi) * Math.Sin(theta) + B[1] * Math.Sin(phi) * Math.Sin(
                theta) + B[2] * Math.Cos(theta);

        }

        /// <summary>
        /// 获取指定位置的磁场强度
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public double GetIntensityBField(double x, double y, double z)
        {
            List<double> B = GetField(x, y, z);
            return Math.Sqrt(B[0] * B[0] + B[1] * B[1] + B[2] * B[2]);
        }
    }
}
