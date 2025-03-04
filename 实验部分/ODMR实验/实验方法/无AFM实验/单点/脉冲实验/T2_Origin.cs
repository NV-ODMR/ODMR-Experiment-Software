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
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验
{
    class T2Origin : ODMRExperimentWithoutAFM
    {
        public override string ODMRExperimentName { get; set; } = "退相干时间测量(T2*)";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            //0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
            new Param<int>("T2*最小值(ns)",20,"T2min"),
            new Param<int>("T2*最大值(ns)",100,"T2max"),
            new Param<int>("T2*点数(ns)",20,"T2points"),
            new Param<int>("循环次数",1000,"LoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            /// 设备:板卡，光子计数器,微波源
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("板卡","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.射频源,new Param<string>("射频源","","RFSource")),
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

        public List<object> FirstScanEvent(object device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            return ScanEvent(device, range, locvalue, inputParams);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", System.Windows.MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private int CurrentLoop = 0;
        public List<object> ScanEvent(object device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            var freq = Get1DChartDataSource("驰豫时间长度(ns)", "T2*荧光数据");
            var signal = Get1DChartDataSource("驰豫信号对比度[sig]", "T2*荧光数据");
            var reference = Get1DChartDataSource("对比实验信号对比度[ref]", "T2*荧光数据");
            var count = Get1DChartDataSource("平均光子数", "T2*荧光数据");
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override void ODMRExpWithoutAFM()
        {
            int Loop = GetInputParamValueByName("LoopCount");
            double progressstep = 100 / Loop;
            for (int i = 0; i < Loop; i++)
            {
                CurrentLoop = i;
                Scan1DSession<object> Session = new Scan1DSession<object>();
                Session.FirstScanEvent = FirstScanEvent;
                Session.ScanEvent = ScanEvent;
                Session.ScanSource = null;
                Session.ProgressBarMethod = new Action<object, double>((obj, v) =>
                {
                    SetProgress(v);
                });
                Session.SetStateMethod = new Action<object, double>((obj, v) =>
                {
                    SetExpState("当前扫描轮数:" + i.ToString() + ",时间点: " + Math.Round(v, 5).ToString());
                });

                D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("T2min"), GetInputParamValueByName("T2max"), GetInputParamValueByName("T2points"));

                Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
                Session.BeginScan(range, progressstep * i, progressstep * (i + 1));
            }
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            RFSourceInfo RF = GetDeviceByName("RFSource") as RFSourceInfo;
            RF.Device.IsRFOutOpen = true;

            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("驰豫时间长度(ns)","T2*荧光数据",ChartDataType.X),
                new NumricChartData1D("驰豫信号对比度[sig]","T2*荧光数据",ChartDataType.Y),
                new NumricChartData1D("对比实验信号对比度[ref]","T2*荧光数据",ChartDataType.Y),
                new NumricChartData1D("平均光子数","T2*荧光数据",ChartDataType.Y),
            };
            UpdatePlotChart();

            Show1DChartData("T2*荧光数据", "驰豫时间长度(ns)", "驰豫信号对比度[sig]", "对比实验信号对比度[ref]", "平均光子数");
        }

        //T1拟合函数
        private double T1FitFunc(double x, double[] ps)
        {
            double a = ps[0];
            double tau = ps[1];
            double b = ps[2];
            return a * Math.Exp(-x / tau) + b;
        }

        public override void AfterExpEventWithoutAFM()
        {
            RFSourceInfo RF = GetDeviceByName("RFSource") as RFSourceInfo;
            RF.Device.IsRFOutOpen = false;
            //计算T1
            var xs = Get1DChartDataSource("驰豫时间长度(ns)", "T2*荧光数据");
            var y1s = Get1DChartDataSource("驰豫信号对比度[sig]", "T2*荧光数据");
            var y2s = Get1DChartDataSource("对比实验信号对比度[ref]", "T2*荧光数据");
            var sub = y1s.Zip(y2s, new Func<double, double, double>((v1, v2) => v2 - v1)).ToList();
            var tempdata = sub.Select(x => Math.Abs(x - (sub.Max() + sub.Min()) / 2)).ToList();
            double inittau = xs[tempdata.IndexOf(tempdata.Min())];
            double[] ps = CurveFitting.FitCurveWithFunc(xs, sub, new List<double>() { sub.Max() - sub.Min(), inittau, sub.Min() }, new List<double>() { 10, 10, 10 }, T1FitFunc, AlgorithmType.LevenbergMarquardt, 2000);
            OutputParams.Add(new Param<double>("T2*拟合值(ns)", ps[1], "T2FitData"));
            //计算平均光子计数
            OutputParams.Add(new Param<double>("平均光子计数", Get1DChartDataSource("平均光子数", "T2*荧光数据").Average(), "AverageCount"));
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> PlotData = new List<ParentPlotDataPack>();
            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "T2*荧光数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "T2*荧光数据"), false));
            PlotData.Add(new ParentPlotDataPack("驰豫信号对比度[sig]", "T2*荧光数据", ChartDataType.Y, Get1DChartDataSource("驰豫信号对比度[sig]", "T2*荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("对比实验信号对比度[ref]", "T2*荧光数据", ChartDataType.X, Get1DChartDataSource("对比实验信号对比度[ref]", "T2*荧光数据"), true));
            return PlotData;
        }
    }
}
