using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using PythonHandler;
using System.IO;
using OpenCvSharp.Dnn;
using MathLib;
using System.Net;
using System.Windows.Documents;
using MathNet.Numerics.RootFinding;
using MathLib.NormalMath.Decimal.Integral;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    internal class Magnet
    {
        public double Radius { get; set; } = 1;

        public double Length { get; set; } = 1;


        public double Magnetization { get; set; } = 1;

        private double miu { get; set; } = 4 * Math.PI * Math.Pow(10, -7);

        public Magnet(double r, double l0, double mag)
        {
            Radius = r;
            Length = l0;
            Magnetization = mag;
        }

        private double GetG(double x, double y, double z)
        {
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "GetG", TimeSpan.FromSeconds(2000), Radius, Length, x, y, z);
            return (double)result;
        }

        private double GetGZ(double x, double y, double z)
        {
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "GetGZ", TimeSpan.FromSeconds(2000), Radius, Length, x, y, z);
            return (double)result;
        }

        /// <summary>
        /// 得到目标磁场(磁感应强度沿Z轴正向,单位为高斯)
        /// </summary>
        /// <param name=""></param>
        /// <param name="x"></param>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public List<double> GetField(double x, double y, double z)
        {
            double temp = miu / Math.PI * Magnetization;
            double bG = GetG(x, y, z);
            double bx = x * bG;
            double by = y * bG;
            double bz = GetGZ(x, y, z);
            return new List<double>() { temp * bx, temp * by, bz * temp };
        }

        public double GetNVField(double x, double y, double z, double theta, double phi)
        {
            List<double> B = GetField(x, y, z);
            return B[0] * Math.Cos(phi) * Math.Sin(theta) + B[1] * Math.Sin(phi) * Math.Sin(
                    theta) + B[2] * Math.Cos(theta);
        }

        public double GetIntensityBField(double x, double y, double z)
        {
            List<double> B = GetField(x, y, z);
            return Math.Sqrt(B[0] * B[0] + B[1] * B[1] + B[2] * B[2]);
        }

        /// <summary>
        /// 获取在rou处，z=0的G的一阶导数(数值解)
        /// </summary>
        /// <param name="x"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public double GetD1GAtz0(double x, double y)
        {
            double det = 1e-5;
            double vp1 = GetG(x, y, det);
            double vn1 = GetG(x, y, -det);
            double vp2 = GetG(x, y, 2 * det);
            double vn2 = GetG(x, y, -2 * det);
            return (-vp2 + 8 * vp1 - 8 * vn1 + vn2) / (12 * det);
        }

        public double GetD1NVAtz0(double x, double y, double theta, double phi)
        {
            double det = 1e-5;
            double vp1 = GetNVField(x, y, det, theta, phi);
            double vn1 = GetNVField(x, y, -det, theta, phi);
            double vp2 = GetNVField(x, y, 2 * det, theta, phi);
            double vn2 = GetNVField(x, y, -2 * det, theta, phi);
            return (-vp2 + 8 * vp1 - 8 * vn1 + vn2) / (12 * det);
        }

        public double GetD2NVAtz0(double x, double y, double theta, double phi)
        {
            double det = 1e-5;
            double v1 = GetNVField(x, y, det, theta, phi);
            double v2 = GetNVField(x, y, -det, theta, phi);
            double v0 = GetNVField(x, y, 0, theta, phi);
            return (v2 + v1 - 2 * v0) / (det * det);
        }

        public double GetD3GAtz0(double x, double y)
        {
            double det = 0.0001;
            double vp1 = GetG(x, y, det);
            double vp2 = GetG(x, y, 2 * det);
            double vp3 = GetG(x, y, 3 * det);
            double vn1 = GetG(x, y, -det);
            double vn2 = GetG(x, y, -2 * det);
            double vn3 = GetG(x, y, -3 * det);
            return (-vp3 + 8 * vp2 - 13 * vp1 + 13 * vn1 - 8 * vn2 + vn3) / (8 * Math.Pow(det, 3));
        }

        public double GetD2BzAtz0(double rou)
        {
            double det = 1e-5;
            double v1 = GetField(rou, 0, det)[2];
            double v2 = GetField(rou, 0, -det)[2];
            double v0 = GetField(rou, 0, 0)[2];
            return (v2 + v1 - 2 * v0) / (det * det);
        }

        public double GetD1RouBzAtz0(double rou)
        {
            double det = 1e-5;
            double v1 = GetField(rou - det, 0, 0)[2];
            double v2 = GetField(rou + det, 0, 0)[2];
            return (v2 - v1) / (2 * det);
        }



        private double GetAngle(double x, double dis)
        {
            var B = GetField(0, dis, x);
            double an = Math.Atan2(Math.Sqrt(B[0] * B[0] + B[2] * B[2]), B[1]);
            an = an * 180 / Math.PI;
            return an;
        }

        /// <summary>
        /// 指定方向和给定的Z方向距离，找出对应方向上的磁场，相对于零度要转动的角度，以及XY平面上相对于原点的坐标
        /// </summary>
        /// <param name="targetthe"></param>
        /// <param name="targetphi"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public List<double> FindDire(double targetthe, double targetphi, double distance)
        {
            double r = FindRoots.OfFunction((x => GetAngle(x, distance) - targetthe), -1, 1);

            //计算以X轴为正向的给定theta角的距离
            var B = GetField(0, distance, r);
            double detphi = targetphi - 180;
            while (detphi > 360)
            {
                detphi -= 360;
            }

            while (detphi < -360)
            {
                detphi += 360;
            }

            return new List<double>(){
                detphi,
                r * Math.Cos(detphi / 180 * Math.PI),
                r * Math.Sin(detphi / 180 * Math.PI),
                Math.Sqrt(B[0] *B[0] + B[1]*B[1] + B[2]*B[2])
            };
        }

        /// <summary>
        /// 获取Z方向的距离
        /// </summary>
        /// <param name="loc1"></param>
        /// <param name="loc2"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public double FindZRoot(double loc1, double loc2, double ratio)
        {
            double det = Math.Abs(loc1 - loc2);

            double zdis = FindRoots.OfFunction(x => GetIntensityBField(x, 0, 0) / GetIntensityBField(x + det, 0, 0) - ratio, Radius + 0.001, 100);
            return zdis;
        }
    }
}