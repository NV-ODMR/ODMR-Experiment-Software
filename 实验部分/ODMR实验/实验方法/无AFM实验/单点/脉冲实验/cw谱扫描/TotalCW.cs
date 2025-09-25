using Controls.Charts;
using MathLib.NormalMath.Decimal;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描
{
    /// <summary>
    /// CW全谱
    /// </summary>
    public class TotalCW : CWBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("频率起始点(MHz)",2830,"RFFreqLo"),
            new Param<double>("频率中止点(MHz)",2890,"RFFreqHi"),
            new Param<double>("扫描步长(MHz)",1,"RFStep"),
            new Param<double>("微波功率(dBm)",-20,"RFPower"),
            new Param<bool>("反向扫描",false,"Reverse"),
            new Param<int>("循环次数",1000,"LoopCount"),
            new Param<int>("单点扫描时间上限(ms)",0,"TimeOut"),
            new Param<CWFitModes>("谱峰拟合类型",CWFitModes.单峰洛伦兹拟合,"FitType"),
            new Param<double>("预计峰宽",2.5,"PeakWidth"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };

        public override string ODMRExperimentName { get; set; } = "CW全谱";

        protected override int GetLoopCount()
        {
            return GetInputParamValueByName("LoopCount");
        }

        protected override int GetPointTimeout()
        {
            return GetInputParamValueByName("TimeOut");
        }

        public override List<double> GetScanFrequences()
        {
            return new D1NumricLinearScanRange(GetInputParamValueByName("RFFreqLo"), GetInputParamValueByName("RFFreqHi"),
                GetInputParamValueByName("RFStep"), GetInputParamValueByName("Reverse")).ScanPoints;
        }

        /// <summary>
        /// 洛伦兹拟合函数
        /// </summary>
        /// <returns></returns>
        private double LorentzFunc(double x, double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            return -(0.25 * a * b * b) / ((x - c) * (x - c) + 0.25 * b * b);
        }

        protected override void SetOutput()
        {
            //根据拟合类型设置输出参数
            OutputParams.Clear();
            //平均光子计数率
            OutputParams.Add(new Param<double>("平均光子总数", GetReferenceCounts().Average(), "AverageCounts"));
            int deducetime = 0;
            CWFitModes mode = GetInputParamValueByName("FitType");
            if (mode == CWFitModes.单峰洛伦兹拟合) deducetime = 1;
            if (mode == CWFitModes.双峰洛伦兹拟合) deducetime = 2;
            if (mode == CWFitModes.三峰洛伦兹拟合) deducetime = 3;
            if (mode == CWFitModes.四峰洛伦兹拟合) deducetime = 4;
            List<double> freqs = GetFrequences();
            List<double> contracts = GetContracts();
            FitCWPeaks(deducetime, freqs, contracts);
        }

        protected override double GetRFPower()
        {
            return GetInputParamValueByName("RFPower");
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> Plotdata = new List<ParentPlotDataPack>();
            Plotdata.Add(new ParentPlotDataPack("对比度", "对比度数据", ChartDataType.Y, GetContracts(), true));
            Plotdata.Add(new ParentPlotDataPack("频率", "对比度数据", ChartDataType.X, GetFrequences(), false));
            Plotdata.Add(new ParentPlotDataPack("参考信号计数", "光子计数数据", ChartDataType.Y, GetReferenceCounts(), true));
            Plotdata.Add(new ParentPlotDataPack("频率", "光子计数数据", ChartDataType.X, GetFrequences(), false));
            Plotdata.Add(new ParentPlotDataPack("信号计数", "光子计数数据", ChartDataType.Y, GetSignalCounts(), true));
            return Plotdata;
        }
    }
}
