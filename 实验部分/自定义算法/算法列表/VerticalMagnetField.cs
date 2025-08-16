using Controls.Windows;
using MathLib.NormalMath.Decimal;
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
    internal class VerticalMagnetField : AlgorithmBase
    {
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("NV轴方位角θ(度)",double.NaN,"NVTheta"),
            new Param<double>("NV轴方位角φ(度)",double.NaN,"NVPhi"),
            new Param<double>("垂直参考方位角θ(度)",double.NaN,"VerticalTheta"),
            new Param<double>("垂直参考方位角φ(度)",double.NaN,"VerticalPhi"),
            new Param<double>("旋转角度(度)",double.NaN,"RotateAngle"),
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override string AlgorithmName { get; } = "NV垂直面磁场计算";

        public override string AlgorithmDescription { get; } = "此算法通过输入：\n1：NV轴的实验室坐标系方位角；\n2：垂直于NV轴的一个参考方向的实验室坐标系方位角；\n3：相对于参考方向在NV轴垂直面上转动的角度（沿着NV轴看逆时针为正）；\n" +
            "最终计算出相应方向的磁场方位角。";

        public override void CalculateFunc()
        {
            double nvtheta = GetInputParamValueByName("NVTheta") / 180 * Math.PI;
            double nvphi = GetInputParamValueByName("NVPhi") / 180 * Math.PI;
            double verticaltheta = GetInputParamValueByName("VerticalTheta") / 180 * Math.PI;
            double verticalphi = GetInputParamValueByName("VerticalPhi") / 180 * Math.PI;
            double rotateangle = GetInputParamValueByName("RotateAngle") / 180 * Math.PI;

            Vector3D nvaxis = new Vector3D(Math.Cos(nvphi) * Math.Sin(nvtheta), Math.Sin(nvphi) * Math.Sin(nvtheta), Math.Cos(nvtheta));
            Vector3D verticalaxis = new Vector3D(Math.Cos(verticalphi) * Math.Sin(verticaltheta), Math.Sin(verticalphi) * Math.Sin(verticaltheta), Math.Cos(verticaltheta));
            var mat = AlgorithmTools.CalculateRotationMatrix(nvaxis, rotateangle);
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
            OutputParams.Add(new Param<double>("目标方向方位角θ", theta, "TargetTheta"));
            OutputParams.Add(new Param<double>("目标方向方位角φ", phi, "TargetPhi"));
        }

    }
}
