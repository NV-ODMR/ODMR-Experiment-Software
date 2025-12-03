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
    /// <summary>
    /// 计算NV的X轴
    /// </summary>
    internal class VectorAngle : AlgorithmBase
    {
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("向量1 方位角θ(度)",double.NaN,"Theta1"),
            new Param<double>("向量1 方位角φ(度)",double.NaN,"Phi1"),
            new Param<double>("向量2 方位角θ(度)",double.NaN,"Theta2"),
            new Param<double>("向量2 方位角φ(度)",double.NaN,"Phi2"),
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override string AlgorithmName { get; } = "向量夹角计算";

        public override string AlgorithmDescription { get; } = "此算法通过输入两个不同的方向,计算这两个方向之间的夹角";

        public override void CalculateFunc()
        {
            double theta1 = GetInputParamValueByName("Theta1");
            double phi1 = GetInputParamValueByName("Phi1");
            double theta2 = GetInputParamValueByName("Theta2");
            double phi2 = GetInputParamValueByName("Phi2");

            Vector3D v1 = AlgorithmTools.GetDirectionVector(theta1, phi1);
            Vector3D v2 = AlgorithmTools.GetDirectionVector(theta2, phi2);

            double angle = Vector3D.AngleBetween(v1, v2);
            OutputParams.Add(new Param<double>("夹角(度)", angle, "Angle"));
        }

    }
}
