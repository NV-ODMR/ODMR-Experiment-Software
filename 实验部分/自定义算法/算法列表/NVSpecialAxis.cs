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
    internal class NVSpecialAxis : AlgorithmBase
    {
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("NV轴 方位角θ(度)",double.NaN,"NVTheta1"),
            new Param<double>("NV轴 方位角φ(度)",double.NaN,"NVPhi1"),
            new Param<double>("辅助NV轴 方位角θ(度)",double.NaN,"NVTheta2"),
            new Param<double>("辅助NV轴 方位角φ(度)",double.NaN,"NVPhi2"),
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override string AlgorithmName { get; } = "NV碳原子投影方向计算";

        public override string AlgorithmDescription { get; } = "此算法通过输入两个不同的NV轴朝向,根据NV朝向具有的四面体结构,给出当前朝向对应的垂直面内另一个碳原子的投影方向(X轴,共有3个等效方向)；\n";

        public override void CalculateFunc()
        {
            double theta1 = GetInputParamValueByName("NVTheta1") / 180 * Math.PI;
            double phi1 = GetInputParamValueByName("NVPhi1") / 180 * Math.PI;
            double theta2 = GetInputParamValueByName("NVTheta2") / 180 * Math.PI;
            double phi2 = GetInputParamValueByName("NVPhi2") / 180 * Math.PI;

            Vector3D nvaxis1 = AlgorithmTools.GetDirectionVector(theta1, phi1);
            Vector3D nvaxis2 = AlgorithmTools.GetDirectionVector(theta2, phi2);

            Vector3D nvmirrory = Vector3D.CrossProduct(nvaxis1, nvaxis2);
            //投影朝向
            Vector3D nvmirrorx = Vector3D.CrossProduct(nvmirrory, nvaxis1);
            AlgorithmTools.GetAngles(nvmirrorx, out double t1, out double p1);
            var rotmatrix = AlgorithmTools.CalculateRotationMatrix(nvaxis1, 120);
            RealMatrix initvec = new RealMatrix(3, 1);
            initvec.Content[0][0] = nvmirrorx.X;
            initvec.Content[0][1] = nvmirrorx.Y;
            initvec.Content[0][2] = nvmirrorx.Z;
            var mat2 = rotmatrix * initvec;
            var mat3 = rotmatrix * rotmatrix * initvec;
            AlgorithmTools.GetAngles(new Vector3D(mat2.Content[0][0], mat2.Content[0][1], mat2.Content[0][2]), out double t2, out double p2);
            AlgorithmTools.GetAngles(new Vector3D(mat3.Content[0][0], mat3.Content[0][1], mat3.Content[0][2]), out double t3, out double p3);

            OutputParams.Add(new Param<double>("投影1 方位角θ", t1, "TargetTheta1"));
            OutputParams.Add(new Param<double>("投影1 方位角φ", p1, "TargetPhi1"));

            OutputParams.Add(new Param<double>("投影2 方位角θ", t2, "TargetTheta2"));
            OutputParams.Add(new Param<double>("投影2 方位角φ", p2, "TargetPhi2"));

            OutputParams.Add(new Param<double>("投影3 方位角θ", t3, "TargetTheta3"));
            OutputParams.Add(new Param<double>("投影3 方位角φ", p3, "TargetPhi3"));
        }

    }
}
