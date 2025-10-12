using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Controls.Charts;
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
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class T2Star : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override string ODMRExperimentName { get; set; } = "退相干时间测量(T2)";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            //0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
            new Param<int>("T2最小值(ns)",20,"T2min"),
            new Param<int>("T2最大值(ns)",100,"T2max"),
            new Param<int>("T2点数(ns)",20,"T2points"),
            new Param<int>("测量次数",1000,"LoopCount"),
            new Param<MultiScanType>("测量循环类型",MultiScanType.正向扫描,"ScanType"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>();
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
            };
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private int CurrentLoop = 0;
        public List<object> ScanEvent(object device, D1NumricScanRangeBase range, double locvalue, int currrentloop, List<Tuple<string, string, double, MultiLoopDataProcessBase>> outputparams, List<object> inputParams)
        {
            GlobalPulseParams.SetGlobalPulseLength("T2Step", (int)locvalue);

            PulsePhotonPack pack = DoPulseExp("T2", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 6, GetInputParamValueByName("TimeOut"));

            double signalcount0 = pack.GetPhotonsAtIndex(0).Sum();
            double signalcount1 = pack.GetPhotonsAtIndex(1).Sum();
            double refcount = pack.GetPhotonsAtIndex(2).Sum();

            double signalcontrast0 = double.NaN;
            double signalcontrast1 = double.NaN;
            try
            {
                signalcontrast0 = (signalcount0 - refcount) / refcount;
                signalcontrast1 = (signalcount1 - refcount) / refcount;
            }
            catch (Exception)
            {
            }

            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("驰豫时间长度(ns)", "T2对比度数据", locvalue, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("0态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据", signalcontrast0, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("1态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据", signalcontrast1, new StandardDataProcess()));

            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("驰豫时间长度(ns)", "T2荧光数据", locvalue, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("参考光子数", "T2荧光数据", refcount, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("0态信号光子数", "T2荧光数据", signalcount0, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("1态信号光子数", "T2荧光数据", signalcount1, new StandardDataProcess()));

            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        private double t2total = 0;

        public override void ODMRExpWithoutAFM()
        {
            MultiScan1DSession<object> Session = new MultiScan1DSession<object>();
            Session.FirstScanEvent = ScanEvent;
            Session.ScanEvent = ScanEvent;
            Session.ScanSource = null;
            Session.PlotEvent = PlotEvent;
            Session.ProgressBarMethod = new Action<object, double>((obj, v) =>
            {
                SetProgress(v);
            });
            Session.SetStateMethod = new Action<object, int, double>((obj, loop, v) =>
            {
                SetExpState("当前扫描轮数:" + loop.ToString() + ",时间点: " + Math.Round(v, 5).ToString());
            });
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("T2min"), GetInputParamValueByName("T2max"), GetInputParamValueByName("T2points"));

            Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            Session.BeginScan(GetInputParamValueByName("LoopCount"), GetInputParamValueByName("ScanType"), range, 0, 100);

        }

        private void PlotEvent(List<MultiLoopScanData> list)
        {
            (Get1DChartData("驰豫时间长度(ns)", "T2对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "驰豫时间长度(ns)", "T2对比度数据");
            (Get1DChartData("0态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "0态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据");
            (Get1DChartData("1态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "1态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据");

            (Get1DChartData("驰豫时间长度(ns)", "T2荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "驰豫时间长度(ns)", "T2荧光数据");
            (Get1DChartData("参考光子数", "T2荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "参考光子数", "T2荧光数据");
            (Get1DChartData("0态信号光子数", "T2荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "0态信号光子数", "T2荧光数据");
            (Get1DChartData("1态信号光子数", "T2荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "1态信号光子数", "T2荧光数据");

            (Get1DChartData("驰豫时间长度(ns)", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "驰豫时间长度(ns)", "T2对比度数据");
            (Get1DChartData("0态退相干信号对比度[(sig-ref)/ref]", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "0态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据");
            (Get1DChartData("1态退相干信号对比度[(sig-ref)/ref]", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "1态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据");

            (Get1DChartData("参考光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "参考光子数", "T2荧光数据");
            (Get1DChartData("0态信号光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "0态信号光子数", "T2荧光数据");
            (Get1DChartData("1态信号光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "1态信号光子数", "T2荧光数据");


            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;

            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("驰豫时间长度(ns)","T2对比度数据",ChartDataType.X),
                new NumricChartData1D("0态退相干信号对比度[(sig-ref)/ref]","T2对比度数据",ChartDataType.Y),
                new NumricChartData1D("1态退相干信号对比度[(sig-ref)/ref]","T2对比度数据",ChartDataType.Y),

                new NumricChartData1D("驰豫时间长度(ns)","T2荧光数据",ChartDataType.X),
                new NumricChartData1D("参考光子数","T2荧光数据",ChartDataType.Y),
                new NumricChartData1D("0态信号光子数","T2荧光数据",ChartDataType.Y),
                new NumricChartData1D("1态信号光子数","T2荧光数据",ChartDataType.Y),

                new NumricChartData1D("驰豫时间长度(ns)","方差",ChartDataType.X),

                 new NumricChartData1D("0态退相干信号对比度[(sig-ref)/ref]","方差",ChartDataType.Y),
                new NumricChartData1D("1态退相干信号对比度[(sig-ref)/ref]","方差",ChartDataType.Y),

                new NumricChartData1D("参考光子数","方差",ChartDataType.Y),
                new NumricChartData1D("0态信号光子数","方差",ChartDataType.Y),
                new NumricChartData1D("1态信号光子数","方差",ChartDataType.Y),
            };
            UpdatePlotChart();

            Show1DChartData("T2对比度数据", "驰豫时间长度(ns)", "退相干信号对比度[(sig-ref)/ref]");
        }

        //T1拟合函数
        private double T2FitFunc(double x, double[] ps)
        {
            double a = ps[0];
            double tau = ps[1];
            double b = ps[2];
            return a * Math.Exp(-x / tau) + b;
        }

        public override void AfterExpEventWithoutAFM()
        {
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = false;
            //计算T2
            var xs = Get1DChartDataSource("驰豫时间长度(ns)", "T2荧光数据");
            var ys = Get1DChartDataSource("退相干信号对比度[ref/sig]", "T2对比度数据");
            var tempdata = ys.Select(x => Math.Abs(x - (ys.Max() + ys.Min()) / 2)).ToList();
            double inittau = xs[tempdata.IndexOf(tempdata.Min())];
            double[] ps = CurveFitting.FitCurveWithFunc(xs, ys, new List<double>() { ys.Max() - ys.Min(), inittau, ys.Min() }, new List<double>() { 10, 10, 10 }, T2FitFunc, AlgorithmType.LevenbergMarquardt, 2000);

            //设置拟合曲线
            var ftxs = new D1NumricLinearScanRange(xs.Min(), xs.Max(), 500).ScanPoints;
            var fitys = ftxs.Select(x => T2FitFunc(x, ps)).ToList();
            D1FitDatas.Add(new FittedData1D("a*Exp(-x/t)+b", "x", new List<string>() { "a", "t", "b" }, ps.ToList(), "驰豫时间长度(ns)", "T2对比度数据", new NumricDataSeries("拟合曲线", ftxs, fitys) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            Show1DFittedData("拟合曲线");

            OutputParams.Add(new Param<double>("T2拟合值(ns)", ps[1], "T2StarFitData"));
            //计算平均光子计数
            OutputParams.Add(new Param<double>("平均光子计数", Get1DChartDataSource("参考光子数", "T2荧光数据").Average(), "AverageCount"));
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> PlotData = new List<ParentPlotDataPack>();
            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "T2对比度数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "T2对比度数据"), false));
            PlotData.Add(new ParentPlotDataPack("0态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据", ChartDataType.Y, Get1DChartDataSource("0态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据"), true));
            PlotData.Add(new ParentPlotDataPack("1态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据", ChartDataType.Y, Get1DChartDataSource("1态退相干信号对比度[(sig-ref)/ref]", "T2对比度数据"), true));


            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "T2荧光数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "T2荧光数据"), false));
            PlotData.Add(new ParentPlotDataPack("参考光子数", "T2荧光数据", ChartDataType.Y, Get1DChartDataSource("参考光子数", "T2荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("0态信号光子数", "T2荧光数据", ChartDataType.Y, Get1DChartDataSource("0态信号光子数", "T2荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("1态信号光子数", "T2荧光数据", ChartDataType.Y, Get1DChartDataSource("1态信号光子数", "T2荧光数据"), true));

            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "方差", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "方差"), false));
            PlotData.Add(new ParentPlotDataPack("参考光子数", "方差", ChartDataType.Y, Get1DChartDataSource("参考光子数", "方差"), true));
            PlotData.Add(new ParentPlotDataPack("0态信号光子数", "方差", ChartDataType.Y, Get1DChartDataSource("0态信号光子数", "方差"), true));
            PlotData.Add(new ParentPlotDataPack("1态信号光子数", "方差", ChartDataType.Y, Get1DChartDataSource("1态信号光子数", "方差"), true));

            return PlotData;
        }
    }
}
