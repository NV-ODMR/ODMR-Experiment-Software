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
    internal class NVElectricFieldDirection : AlgorithmBase
    {
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("NV轴 方位角θ(度)",double.NaN,"NVTheta"),
            new Param<double>("NV轴 方位角φ(度)",double.NaN,"NVPhi"),
            new Param<double>("NV碳原子投影方向 方位角θ(度)",double.NaN,"CTheta"),
            new Param<double>("NV碳原子投影方向 方位角φ(度)",double.NaN,"CPhi"),
            new Param<double>("相对于参考方向的角度(度)",double.NaN,"RelevAngle"),
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override string AlgorithmName { get; } = "NV电场测量灵敏轴方向计算";

        public override string AlgorithmDescription { get; } = "此算法通过输入NV轴朝向和碳原子在轴垂直面的的投影方向，计算要想测量给定角度投影电场需要的磁场方向(角度的参考方向为输入的碳原子投影方向)和电场投影轴方向，投影权函数cos（2*ΦB+ΦE）," +
            "灵敏轴角度θ和磁场夹角的关系为：θ=-2φB";

        public override void CalculateFunc()
        {
            double nvtheta = GetInputParamValueByName("NVTheta");
            double nvphi = GetInputParamValueByName("NVPhi");
            double ctheta = GetInputParamValueByName("CTheta");
            double cphi = GetInputParamValueByName("CPhi");
            double angle = GetInputParamValueByName("RelevAngle") / 180 * Math.PI;

            Vector3D nvaxis1 = AlgorithmTools.GetDirectionVector(nvtheta, nvphi);
            Vector3D nvaxis2 = AlgorithmTools.GetDirectionVector(ctheta, cphi);

            var rotmatrix1 = AlgorithmTools.CalculateRotationMatrix(nvaxis1, -angle / 2);

            var rotmatrix2 = AlgorithmTools.CalculateRotationMatrix(nvaxis1, angle);

            RealMatrix initvec = new RealMatrix(3, 1);
            initvec.Content[0][0] = nvaxis2.X;
            initvec.Content[1][0] = nvaxis2.Y;
            initvec.Content[2][0] = nvaxis2.Z;
            var mat2 = rotmatrix1 * initvec;
            var mat3 = rotmatrix2 * initvec;
            AlgorithmTools.GetAngles(new Vector3D(mat2.Content[0][0], mat2.Content[1][0], mat2.Content[2][0]), out double t2, out double p2);
            AlgorithmTools.GetAngles(new Vector3D(mat3.Content[0][0], mat3.Content[1][0], mat3.Content[2][0]), out double t3, out double p3);

            OutputParams.Add(new Param<double>("灵敏轴电场方向 方位角θ", t3, "TargetTheta1"));
            OutputParams.Add(new Param<double>("灵敏轴电场方向 方位角φ", p3, "TargetPhi1"));

            OutputParams.Add(new Param<double>("磁场方向 方位角θ", t2, "TargetTheta2"));
            OutputParams.Add(new Param<double>("磁场方向 方位角φ", p2, "TargetPhi2"));
        }
    }
}
