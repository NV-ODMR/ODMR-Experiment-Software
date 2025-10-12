using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.实验部分.自定义算法.算法列表;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.CW谱扫描
{
    internal class TwoPeakCW : CWBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override string ODMRExperimentName { get; set; } = "双峰CW谱";

        public override string Description { get; set; } = "连续波谱实验:在两个中心频率(一般是谱峰位置附近)两侧以指定的宽度和步长扫描连续波信号,之后用双峰函数进行拟合";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("谱峰位置1(MHz)",2850,"Frequency1"){ Helper="扫描的第1个中心频率" },
            new Param<double>("谱峰位置2(MHz)",2890,"Frequency2"){ Helper="扫描的第2个中心频率" },
            new Param<double>("扫描峰宽(MHz)",100,"ScanSpan"){ Helper="" },
            new Param<double>("扫描步长(MHz)",100,"ScanStep"){ Helper="" },
            new Param<double>("预计峰宽(MHz)",3.5,"PeakWidth"){ Helper="" },
            new Param<double>("微波功率(dBm)",-20,"RFPower"){ Helper="" },
            new Param<int>("循环次数",1000,"LoopCount"){ Helper="扫描每个频点时板卡序列的内部循环次数" },
            new Param<int>("单点扫描时间上限(ms)",10000,"TimeOut"){ Helper="每个频点扫描的时间上限,超时则跳过此点" },
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        public override List<double> GetScanFrequences()
        {
            //生成双峰扫描的频点
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("Frequency1") - GetInputParamValueByName("ScanSpan") / 2,
                GetInputParamValueByName("Frequency1") + GetInputParamValueByName("ScanSpan") / 2, GetInputParamValueByName("ScanStep"), false);
            List<double> points = range.ScanPoints;
            range = new D1NumricLinearScanRange(GetInputParamValueByName("Frequency2") - GetInputParamValueByName("ScanSpan") / 2,
                GetInputParamValueByName("Frequency2") + GetInputParamValueByName("ScanSpan") / 2, GetInputParamValueByName("ScanStep"), true);
            points.AddRange(range.ScanPoints);
            return points;
        }

        protected override int GetLoopCount()
        {
            return GetInputParamValueByName("LoopCount");
        }

        protected override int GetPointTimeout()
        {
            return GetInputParamValueByName("TimeOut");
        }

        protected override double GetRFPower()
        {
            return GetInputParamValueByName("RFPower");
        }

        protected override void SetOutput()
        {
            //根据拟合类型设置输出参数
            OutputParams.Clear();
            //平均光子计数率
            OutputParams.Add(new Param<double>("平均光子总数", GetReferenceCounts().Average(), "AverageCounts"));
            List<double> freqs = GetFrequences();
            List<double> contracts = GetContracts();
            FitCWPeaks(2, freqs, contracts);
            //计算磁场
            CWCalculate cal = new CWCalculate();
            cal.SetInputParamValueByName("CW1", GetOutputParamValueByName("PeakLoc1"));
            cal.SetInputParamValueByName("CW2", GetOutputParamValueByName("PeakLoc2"));
            cal.SetInputParamValueByName("ZeroCW", 2870.0);
            cal.CalculateFunc();
            //总磁场分量
            OutputParams.Add(new Param<double>("沿轴磁场(Gauss)", cal.GetOutputParamValueByName("Bp"), "BPara"));
            OutputParams.Add(new Param<double>("垂直轴磁场(Gauss)", cal.GetOutputParamValueByName("Bv"), "BVert"));
            OutputParams.Add(new Param<double>("总磁场(Gauss)", cal.GetOutputParamValueByName("B"), "B"));
        }
    }
}
