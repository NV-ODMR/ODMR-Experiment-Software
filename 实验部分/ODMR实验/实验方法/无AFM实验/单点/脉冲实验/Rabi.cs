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
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Window = System.Windows.Window;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class Rabi : PulseExpBase
    {
        public override string ODMRExperimentName { get; set; } = "拉比频率测量(Rabi)";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            //0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
            new Param<int>("时间最小值(ns)",20,"Rabimin"),
            new Param<int>("时间最大值(ns)",100,"Rabimax"),
            new Param<int>("时间点数(ns)",20,"Rabipoints"),
            new Param<int>("测量次数",1000,"LoopCount"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<bool>("将结果同步到全局脉冲",true,"ToGlobal")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>();
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();

        protected override SequenceDataAssemble GetExperimentSequence()
        {
            return SequenceDataAssemble.ReadFromSequenceName("Rabi");
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;

        public List<object> FirstScanEvent(object device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            return ScanEvent(device, range, locvalue, inputParams);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private int CurrentLoop = 0;
        public List<object> ScanEvent(object device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            GlobalPulseParams.SetGlobalPulseLength("RabiTime", (int)locvalue);

            PulsePhotonPack pack = DoPulseExp(GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4);

            double signalcount = pack.GetPhotonsAtIndex(0).Average();
            double refcount = pack.GetPhotonsAtIndex(1).Average();

            var contrfreq = Get1DChartDataSource("微波驱动时间(ns)", "Rabi对比度数据");
            var signal = Get1DChartDataSource("Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");

            var florfreq = Get1DChartDataSource("微波驱动时间(ns)", "Rabi荧光数据");
            var count = Get1DChartDataSource("平均光子数", "Rabi荧光数据");
            var sigcount = Get1DChartDataSource("信号光子数", "Rabi荧光数据");

            int ind = range.GetNearestFormalIndex(locvalue);

            double signalcontrast = 0;
            try
            {
                signalcontrast = signalcount / refcount;
            }
            catch (Exception)
            {
            }

            if (ind >= contrfreq.Count)
            {
                contrfreq.Add(locvalue);
                florfreq.Add(locvalue);
                signal.Add(signalcontrast);
                count.Add(refcount);
                sigcount.Add(signalcount);
            }
            else
            {
                signal[ind] = (signal[ind] * CurrentLoop + signalcontrast) / (CurrentLoop + 1);
                count[ind] = (count[ind] * CurrentLoop + refcount) / (CurrentLoop + 1);
                sigcount[ind] = (sigcount[ind] * CurrentLoop + signalcount) / (CurrentLoop + 1);
            }

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

                D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("Rabimin"), GetInputParamValueByName("Rabimax"), GetInputParamValueByName("Rabipoints"));

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
                new NumricChartData1D("微波驱动时间(ns)","Rabi对比度数据",ChartDataType.X),
                new NumricChartData1D("Rabi信号对比度[(sig-ref)/ref]","Rabi对比度数据",ChartDataType.Y),

                new NumricChartData1D("微波驱动时间(ns)","Rabi荧光数据",ChartDataType.X),
                new NumricChartData1D("平均光子数","Rabi荧光数据",ChartDataType.Y),
                new NumricChartData1D("信号光子数","Rabi荧光数据",ChartDataType.Y),
            };
            UpdatePlotChart();

            Show1DChartData("Rabi对比度数据", "微波驱动时间(ns)", "Rabi信号对比度[(sig-ref)/ref]");
        }

        //T1拟合函数
        private double RabiFitFunc(double x, double[] ps)
        {
            double a = ps[0];
            double tau = ps[1];
            double b = ps[2];
            double c = ps[3];
            double d = ps[4];
            return a * Math.Exp(-x / tau) * Math.Sin(2 * Math.PI / b * (x - c)) + d;
        }

        public override void AfterExpEventWithoutAFM()
        {
            RFSourceInfo RF = GetDeviceByName("RFSource") as RFSourceInfo;
            RF.Device.IsRFOutOpen = false;
            //计算Rabi脉冲周期
            var xs = Get1DChartDataSource("微波驱动时间(ns)", "Rabi对比度数据");
            var ys = Get1DChartDataSource("Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");

            double tau = (xs.Min() + xs.Max()) / 2;
            double d = ys.Average();
            double a = Math.Abs(xs.Min() - xs.Max()) / 2;
            double c = 0;
            //Pi脉冲时间
            double b = 0;
            try
            {
                var fys = ys.ToArray();
                Fourier.Forward(fys.ToArray(), Enumerable.Repeat(0.0, ys.Count).ToArray(), FourierOptions.Matlab);
                var freqs = Fourier.FrequencyScale(ys.Count, (int)1.0 / Math.Abs(xs[0] - xs[1]));
                b = 1.0 / freqs[fys.ToList().IndexOf(fys.Max())];
            }
            catch (Exception)
            {
            }

            double[] ps = CurveFitting.FitCurveWithFunc(xs, ys, new List<double>() { a, tau, b, c, d }, new List<double>() { 10, 10, 10, 10, 10 }, RabiFitFunc, AlgorithmType.LevenbergMarquardt, 2000);
            OutputParams.Add(new Param<double>("Pi脉冲长度(ns)", ps[2] / 2 + ps[3], "PiLength"));
            OutputParams.Add(new Param<double>("Pi/2脉冲长度(ns)", ps[2] + ps[3], "HalfPiLength"));
            OutputParams.Add(new Param<double>("3Pi/2脉冲长度(ns)", 3 * ps[2] / 2 + ps[3], "3HalfPiLength"));
            OutputParams.Add(new Param<double>("2Pi脉冲长度(ns)", 2 * ps[2] + ps[3], "2PiLength"));
            //计算平均光子计数
            OutputParams.Add(new Param<double>("平均光子计数", Get1DChartDataSource("平均光子数", "Rabi荧光数据").Average(), "AverageCount"));

            //
            if (GetInputParamValueByName("ToGlobal") == true)
            {
                GlobalPulseParams.SetGlobalPulseLength("HalfPi", (int)(ps[2] / 2 + ps[3]));
                GlobalPulseParams.SetGlobalPulseLength("Pi", (int)(ps[2] + ps[3]));
                GlobalPulseParams.SetGlobalPulseLength("3HalfPi", (int)(3 * ps[2] / 2 + ps[3]));
                GlobalPulseParams.SetGlobalPulseLength("2Pi", (int)(2 * ps[2] + ps[3]));
            }
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> PlotData = new List<ParentPlotDataPack>();
            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "Rabi对比度数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "Rabi对比度数据"), false));
            PlotData.Add(new ParentPlotDataPack("退相干信号对比度[ref/sig]", "Rabi对比度数据", ChartDataType.Y, Get1DChartDataSource("退相干信号对比度[ref/sig]", "Rabi对比度数据"), true));
            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "Rabi荧光数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "Rabi荧光数据"), false));
            PlotData.Add(new ParentPlotDataPack("平均光子数", "Rabi荧光数据", ChartDataType.X, Get1DChartDataSource("平均光子数", "Rabi荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("信号光子数", "Rabi荧光数据", ChartDataType.X, Get1DChartDataSource("信号光子数", "Rabi荧光数据"), true));
            return PlotData;
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
    }
}
