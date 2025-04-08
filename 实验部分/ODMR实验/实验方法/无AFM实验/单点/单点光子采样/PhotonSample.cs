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
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验
{
    class PhotonSample : ODMRExperimentWithoutAFM
    {
        public override string ODMRExperimentName { get; set; } = "光子数测量";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("采样率(Hz)",20,"SampleRate"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            /// 设备:板卡，光子计数器,微波源
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("激光触发源","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("APD Trace 触发源","","TraceSource")),
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;

        public override bool PreConfirmProcedure()
        {
            return true;
        }

        public override void ODMRExpWithoutAFM()
        {
            SetProgress(0);
            ConfocalAPDSample confocalAPDSample = new ConfocalAPDSample();
            var result = confocalAPDSample.CoreMethod(new List<object>() { GetInputParamValueByName("SampleRate") }, GetDeviceByName("APD"));
            OutputParams.Add(new Param<double>("光子计数", (double)result[0], "SampleCount"));
            SetProgress(100);
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开激光
            LaserOn lon = new LaserOn();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            lon.CoreMethod(new List<object>() { 1.0 }, pb);
            //设置触发源
            PulseBlasterInfo trace = GetDeviceByName("TraceSource") as PulseBlasterInfo;
            trace.Device.PulseFrequency = GetInputParamValueByName("SampleRate");
            trace.Device.Start();
            //打开APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.StartContinusSample();
        }

        public override void AfterExpEventWithoutAFM()
        {
            //关闭激光
            LaserOff loff = new LaserOff();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            loff.CoreMethod(new List<object>() { }, pb);
            //关闭APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.EndContinusSample();
            //关闭APD触发源
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.End();
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }
    }
}
