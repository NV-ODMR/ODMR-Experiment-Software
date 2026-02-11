using CodeHelper;
using Controls.Windows;
using MathLib.NormalMath.Decimal;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.磁场调节;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Vector = System.Windows.Vector;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// OffsetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VerticalFieldWindow : Window
    {
        private MagnetLoc exp = null;

        public VerticalFieldWindow(MagnetLoc parentexp)
        {
            exp = parentexp;
            InitializeComponent();
            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterHideWindow(this, null, null, CloseBtn, 0, 30);
        }

        /// <summary>
        /// 计算偏移量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calc(object sender, RoutedEventArgs e)
        {
            try
            {
                double nvtheta = double.Parse(NVTheta.Text) / 180 * Math.PI;
                double nvphi = double.Parse(NVPhi.Text) / 180 * Math.PI;
                double verticaltheta = double.Parse(VerticalTheta.Text) / 180 * Math.PI;
                double verticalphi = double.Parse(VerticalPhi.Text) / 180 * Math.PI;
                double rotateangle = double.Parse(RotateAngle.Text) / 180 * Math.PI;

                Vector3D nvaxis = new Vector3D(Math.Cos(nvphi) * Math.Sin(nvtheta), Math.Sin(nvphi) * Math.Sin(nvtheta), Math.Cos(nvtheta));
                Vector3D verticalaxis = new Vector3D(Math.Cos(verticalphi) * Math.Sin(verticaltheta), Math.Sin(verticalphi) * Math.Sin(verticaltheta), Math.Cos(verticaltheta));
                var mat = CalculateRotationMatrix(nvaxis, rotateangle);
                RealMatrix vec = new RealMatrix(3, 1)
                {
                    Content = new List<List<double>>() { new List<double>() { verticalaxis.X },
                    new List<double>() { verticalaxis.Y },
                    new List<double>() { verticalaxis.Z }}
                };
                var res = mat * vec;
                Vector3D result = new Vector3D(res.Content[0][0], res.Content[1][0], res.Content[2][0]);
                result.Normalize();
                //计算角度
                double phi = Math.Atan2(result.Y, result.X) * 180 / Math.PI;
                double theta = Math.Atan2(new Vector(result.X, result.Y).Length, result.Z) * 180 / Math.PI;
                targetPhi.Text = phi.ToString();
                targetTheta.Text = theta.ToString();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("计算未完成", this);
            }
            ;
        }

        // 计算绕任意轴旋转指定角度的旋转矩阵
        private RealMatrix CalculateRotationMatrix(Vector3D axis, double angleInRadians)
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
    }
}
