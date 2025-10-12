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
using Controls.Charts;
using System.Windows.Media;
using System.Threading;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class LockInDelayCountTest : LockInExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        /// <summary>
        /// 所属锁相实验
        /// </summary>
        private LockInExpBase ParentLockInExp { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "锁相Delay光子数测量";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("起点时间(ns)",20,"StartTime"),
            new Param<int>("Delay点数",20,"DelayCount"),
            new Param<double>("终点时间(ns)",1000,"EndTime"),
            new Param<int>("测量轮数",1,"LoopCount"),
            new Param<MultiScanType>("测量循环类型",MultiScanType.正向扫描,"ScanType"),
            new Param<int>("序列循环次数",100000,"SeqLoopCount"),
            new Param<double>("锁相信号频率(MHz)",1,"SignalFreq"),
            new Param<int>("光子数采样时间(ns)",50,"CountSampleTime"),
            new Param<int>("单点超时时间",10000,"TimeOut"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> LockInExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
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

        double contrast = double.NaN;
        double Sig = double.NaN;
        double Ref = double.NaN;

        public override void ODMRExpWithoutAFM()
        {
            GlobalPulseParams.SetGlobalPulseLength("DelayCountSampleTime", GetInputParamValueByName("CountSampleTime"));

            MultiScan1DSession<object> Session = new MultiScan1DSession<object>();
            Session.FirstScanEvent = ExpScanEvent;
            Session.ScanEvent = ExpScanEvent;
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
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("StartTime"), GetInputParamValueByName("EndTime"), GetInputParamValueByName("DelayCount"));

            Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            Session.BeginScan(GetInputParamValueByName("LoopCount"), GetInputParamValueByName("ScanType"), range, 0, 100);
        }

        private void PlotEvent(List<MultiLoopScanData> list)
        {
            (Get1DChartData("时间(ns)", "Delay测试数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "时间(ns)", "Delay测试数据");
            (Get1DChartData("光子数", "Delay测试数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "光子数", "Delay测试数据");

            (Get1DChartData("时间(ns)", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "时间(ns)", "Delay测试数据");
            (Get1DChartData("光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "光子数", "Delay测试数据");

            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        private void CountExp(out double Count)
        {
            Count = 0;
            PulsePhotonPack pack = DoLockInPulseExp("DelayCountTest", 2870, -20, GetInputParamValueByName("SignalFreq"), GetInputParamValueByName("SeqLoopCount"), 2,
             GetInputParamValueByName("TimeOut"));
            Count += pack.GetPhotonsAtIndex(0).Sum();
            pack = null;
            GC.Collect();
            JudgeThreadEndOrResumeAction?.Invoke();
            if (Count == 0) Count = double.NaN;
        }

        private List<object> ExpScanEvent(object arg1, D1NumricScanRangeBase arg2, double arg3, int currrentloop, List<Tuple<string, string, double, MultiLoopDataProcessBase>> outputparams, List<object> arg4)
        {
            GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", (int)arg3);
            JudgeThreadEndOrResumeAction();
            CountExp(out double count);
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("时间(ns)", "Delay测试数据", arg3, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("光子数", "Delay测试数据", count, new StandardDataProcess()));

            return new List<object>();
        }

        public override void PreLockInExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("时间(ns)","Delay测试数据",ChartDataType.X),
                new NumricChartData1D("光子数","Delay测试数据",ChartDataType.Y),

                new NumricChartData1D("时间(ns)","方差",ChartDataType.X),
                new NumricChartData1D("光子数","方差",ChartDataType.Y)
            };
            UpdatePlotChart();
            Show1DChartData("Delay测试数据", "时间(ns)", "光子数");
            return;
        }

        public override void AfterLockInExpEventWithoutAFM()
        {
            //用正弦函数拟合得到的曲线
            var count = Get1DChartDataSource("光子数", "Delay测试数据");
            var time = Get1DChartDataSource("时间(ns)", "Delay测试数据");
            double d_x = count.Average();
            double a_x = Math.Abs(count.Min() - count.Max()) / 2;
            double c_x = 10;
            //Pi脉冲时间
            double b_x = GetInputParamValueByName("SignalFreq") * 1000;
            double[] ps_x = CurveFitting.FitCurveWithFunc(time, count, new List<double>() { a_x, b_x, c_x, d_x }, new List<double>() { 10, 10, 10, 10 }, DelayFitFunc, AlgorithmType.LevenbergMarquardt, 20000);

            //设置拟合曲线
            var ftxs = new D1NumricLinearScanRange(time.Min(), time.Max(), 500).ScanPoints;
            var fitys_x = ftxs.Select(x => DelayFitFunc(x, ps_x)).ToList();
            D1FitDatas.Add(new FittedData1D("a*cos(2*pi/b*(x-c))+d", "x", new List<string>() { "a", "b", "c", "d" }, ps_x.ToList(), "时间(ns)", "Delay测试数据", new NumricDataSeries("拟合曲线", ftxs, fitys_x) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            Show1DFittedData("拟合曲线");
            Thread.Sleep(1000);


            OutputParams.Add(new Param<double>("光子数振幅", Math.Abs(ps_x[0]), "Amplitude"));
            OutputParams.Add(new Param<double>("光子数平均值", ps_x[3], "Average"));
            double phase = ps_x[2] + ps_x[1] / 2;
            if (ps_x[0] < 0) phase = ps_x[2] + ps_x[1];
            OutputParams.Add(new Param<double>("Delay相位(ns)", phase, "Phase"));

        }


        private double DelayFitFunc(double x, double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            double d = ps[3];
            return a * Math.Cos(2 * Math.PI / b * (x - c)) + d;
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>() {
                new ParentPlotDataPack("时间(ns)", "单点Delay曲线", ChartDataType.X, Get1DChartDataSource("时间(ns)", "Delay测试数据"), false),
                new ParentPlotDataPack("光子数曲线", "单点Delay曲线", ChartDataType.Y, Get1DChartDataSource("光子数", "Delay测试数据"), true),
                new ParentPlotDataPack("光子数曲线方差", "单点Delay曲线", ChartDataType.Y, Get1DChartDataSource("光子数", "方差"), true)
            };
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override int GetMaxSeqLoopCount()
        {
            return GetInputParamValueByName("SeqLoopCount");
        }

        public override int GetMaxLaserCountPulses()
        {
            return 2;
        }
    }
}
