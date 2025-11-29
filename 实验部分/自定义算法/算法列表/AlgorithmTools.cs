using MathLib.NormalMath.Decimal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ODMR_Lab.实验部分.自定义算法.算法列表
{
    internal class AlgorithmTools
    {
        // 计算绕任意轴旋转指定角度的旋转矩阵(逆时针为正)
        public static RealMatrix CalculateRotationMatrix(Vector3D axis, double angleInRadians)
        {
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            double oneMinusCos = 1 - cosTheta;

            double x = axis.X;
            double y = axis.Y;
            double z = axis.Z;

            double xSq = x * x;
            double ySq = y * y;
            double zSq = z * z;

            double xy = x * y;
            double yz = y * z;
            double zx = z * x;

            double xSin = x * sinTheta;
            double ySin = y * sinTheta;
            double zSin = z * sinTheta;

            double m11 = cosTheta + xSq * oneMinusCos;
            double m12 = xy * oneMinusCos - zSin;
            double m13 = zx * oneMinusCos + ySin;

            double m21 = xy * oneMinusCos + zSin;
            double m22 = cosTheta + ySq * oneMinusCos;
            double m23 = yz * oneMinusCos - xSin;

            double m31 = zx * oneMinusCos - ySin;
            double m32 = yz * oneMinusCos + xSin;
            double m33 = cosTheta + zSq * oneMinusCos;

            var mat = new RealMatrix(3, 3);
            mat.Content = new List<List<double>> { new List<double> { m11, m12, m13 },
                    new List<double>() { m21, m22, m23 },
                    new List<double>() { m31, m32, m33 } };

            return mat;
        }

        public static Vector3D GetDirectionVector(double theta, double phi)
        {
            return new Vector3D(Math.Cos(phi) * Math.Sin(theta), Math.Sin(phi) * Math.Sin(theta), Math.Cos(theta));
        }
        /// <summary>
        /// 获取向量的球坐标(度)
        /// </summary>
        /// <param name="theta"></param>
        /// <param name="phi"></param>
        public static void GetAngles(Vector3D vec, out double theta, out double phi)
        {
            vec.Normalize();
            theta = Math.Acos(vec.Z) / Math.PI * 180;
            phi = Math.Atan2(vec.Y, vec.X);
            return;
        }

    }
}
