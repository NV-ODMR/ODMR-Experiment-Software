using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描
{
    internal class TwoPointCW : CWBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override string ODMRExperimentName { get; set; } = "CW双频点";

        public override string Description { get; set; } = "连续波谱实验:取2个频率点分别测量对应的连续波对比度.";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("频率点1",2850,"Frequency1"){ Helper="" },
            new Param<double>("频率点2",2890,"Frequency2"){ Helper="" },
            new Param<double>("微波功率(dBm)",-20,"RFPower"){ Helper="" },
            new Param<int>("循环次数",1000,"LoopCount"){ Helper="扫描每个频点时板卡序列的内部循环次数" },
            new Param<int>("单点扫描时间上限(ms)",10000,"TimeOut"){ Helper="每个频点扫描的时间上限,超时则跳过此点" },
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override List<double> GetScanFrequences()
        {
            return new List<double>() { GetInputParamValueByName("Frequency1"), GetInputParamValueByName("Frequency2") };
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
            List<double> contracts = GetContracts();
            List<double> counts = GetReferenceCounts();
            OutputParams.Add(new Param<double>("平均光子总数", counts.Average(), "Counts"));
            OutputParams.Add(new Param<double>("对比度1", contracts[0], "Contracts1"));
            OutputParams.Add(new Param<double>("对比度2", contracts[1], "Contracts2"));
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }
    }
}
