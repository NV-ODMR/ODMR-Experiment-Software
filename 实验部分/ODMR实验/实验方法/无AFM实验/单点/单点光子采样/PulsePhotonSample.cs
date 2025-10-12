using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Controls.Windows;
using HardWares.射频源.Rigol_DSG_3060;
using MathLib.NormalMath.Decimal;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验
{
    class PulsePhotonSample : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override string ODMRExperimentName { get; set; } = "脉冲光子数测量";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("序列循环次数",100000,"LoopCount"),
            new Param<int>("光子采样时间(ns)",2000,"SampleTime"),
            new Param<double>("微波功率",2,"RFPower"),
            new Param<int>("超时时间(ms)",10000,"TimeOut"),
            new Param<bool>("是否使用微波",false,"IsRF")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
            };
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>();

        public override bool PreConfirmProcedure()
        {
            return true;
        }

        public override void ODMRExpWithoutAFM()
        {
            PulsePhotonPack package;
            int ratio = GlobalPulseParams.GetGlobalPulseLength("RFRatio");
            int totaltime = GetInputParamValueByName("SampleTime");
            int ontime = (int)(totaltime * (ratio / 100.0));
            int offtime = totaltime - ontime;
            GlobalPulseParams.SetGlobalPulseLength("RFOnTime", ontime);
            GlobalPulseParams.SetGlobalPulseLength("RFOffTime", offtime);
            package = DoPulseExp("FluorenscenceSample", 2870.0, (double)GetInputParamValueByName("RFPower"), (int)GetInputParamValueByName("LoopCount"), 2, (int)GetInputParamValueByName("TimeOut"));
            OutputParams.Add(new Param<int>("光子计数", package.GetPhotonsAtIndex(0).Sum(), "PhotonCount"));
            JudgeThreadEndOrResumeAction?.Invoke();
        }

        public override void PreExpEventWithoutAFM()
        {
        }

        public override void AfterExpEventWithoutAFM()
        {
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
    }
}
