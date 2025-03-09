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
using MathNet.Numerics.LinearAlgebra;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Utilities;
using MathNet.Numerics.Distributions;

namespace ODMR_Lab.实验部分.磁场调节
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
            List<double> res = new List<double>();
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "GetPillarField", TimeSpan.FromSeconds(2000), Radius, Length, Magnetization, x, y, z);
            foreach (var item in result)
            {
                res.Add((double)item);
            }
            return res;
        }

        public double GetNVField(double x, double y, double z, double theta, double phi)
        {
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "GetPillarNVField", TimeSpan.FromSeconds(2000), Radius, Length, theta, phi, Magnetization, x, y, z);
            return (double)result;
        }

        public double GetIntensityBField(double x, double y, double z)
        {
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "GetPillarNVField", TimeSpan.FromSeconds(2000), Radius, Length, Magnetization, x, y, z);
            return (double)result;
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
            List<double> res = new List<double>();
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "FindDire", TimeSpan.FromSeconds(2000), Radius, Length, targetthe, targetphi, distance);
            foreach (var item in result)
            {
                res.Add((double)item);
            }
            return res;
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
            dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "Magnet.py"), "FindRoot", TimeSpan.FromSeconds(2000), Radius, Length, loc1, loc2, ratio);
            return (double)result;
        }
    }
}