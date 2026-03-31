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
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class PhotonCountStatics : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override string ODMRExperimentName { get; set; } = "光子计数统计";

        public override string Description { get; set; } = "使用的序列文件名称：CounterWindow";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("测量次数",10,"LoopCount"),
            new Param<int>("序列循环次数",100000,"SeqLoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<int>("光子数统计数上限",500,"CountLimit"),
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

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
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
        public List<object> ScanEvent()
        {
            PulsePhotonPack photonpack = DoPulseExp("PhotonStaticTest", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4, GetInputParamValueByName("TimeOut"));

            var darkcounts = photonpack.GetPhotonsAtIndex(0)
                .Select((value, index) => new { value, index })
                .GroupBy(x => x.index / 5000)
                .Select(g => g.Sum(x => x.value))
                .ToLookup(n => n);
            var refcounts = photonpack.GetPhotonsAtIndex(1)
                .Select((value, index) => new { value, index })
                .GroupBy(x => x.index / 5000)
                .Select(g => g.Sum(x => x.value))
                .ToLookup(n => n); ;

            var contr = Get1DChartDataSource("光子数", "收集光子数据");
            var refcount = Get1DChartDataSource("亮态光子次数统计", "收集光子数据");
            var datkcount = Get1DChartDataSource("暗态光子次数统计", "收集光子数据");

            if (contr.Count == 0)
            {
                contr.AddRange(Enumerable.Range(0, (int)GetInputParamValueByName("CountLimit")).Select(x => (double)x).ToList());
                datkcount.AddRange(Enumerable.Repeat(0.0, (int)GetInputParamValueByName("CountLimit")).ToList());
                refcount.AddRange(Enumerable.Repeat(0.0, (int)GetInputParamValueByName("CountLimit")).ToList());
            }

            for (int i = 0; i < contr.Count; i++)
            {
                var darks = darkcounts.Where(x => x.Key == contr[i]);
                var refs = refcounts.Where(x => x.Key == contr[i]);
                if (darks.Count() == 0)
                {
                    datkcount[i] = (datkcount[i] * CurrentLoop + 0) / (CurrentLoop + 1);
                }
                else
                {
                    datkcount[i] = (datkcount[i] * CurrentLoop + darks.ElementAt(0).Count()) / (CurrentLoop + 1);
                }
                if (refs.Count() == 0)
                {
                    refcount[i] = (refcount[i] * CurrentLoop + 0) / (CurrentLoop + 1);
                }
                else
                {
                    refcount[i] = (refcount[i] * CurrentLoop + refs.ElementAt(0).Count()) / (CurrentLoop + 1);
                }
            }

            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override void ODMRExpWithoutAFM()
        {
            int Loop = GetInputParamValueByName("LoopCount");
            for (int i = 0; i < Loop; i++)
            {
                CurrentLoop = i;
                ScanEvent();
                SetProgress(i / (double)(Loop));
                SetExpState("当前扫描轮数:" + i.ToString());

                JudgeThreadEndOrResumeAction?.Invoke();
            }
            SetProgress(100);
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;

            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("光子数","收集光子数据",ChartDataType.X),
                new NumricChartData1D("亮态光子次数统计","收集光子数据",ChartDataType.Y),
                new NumricChartData1D("暗态光子次数统计","收集光子数据",ChartDataType.Y),
            };
            UpdatePlotChart();

            Show1DChartData("收集光子数据", "光子数", "亮态光子次数统计", "暗态光子次数统计");
        }

        private double GaussFunc(double x, params double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            double d = ps[3];

            return a * Math.Exp(-(x - c) * (x - c) * 4 * Math.Log(2) / (b * b)) + d;
        }

        public override void AfterExpEventWithoutAFM()
        {
            var contr = Get1DChartDataSource("光子数", "收集光子数据");
            var refcount = Get1DChartDataSource("亮态光子次数统计", "收集光子数据");

            string gaussFitExpression = "a*exp(-(x-c)*(x-c)*4*ln(2)/(b*b))+d";

            int ind = refcount.IndexOf(refcount.Max());
            double max = contr[ind];
            double maxv = refcount[ind];

            CurveFitting Fitting = new CurveFitting(gaussFitExpression, "x", new List<string>() { "a", "b", "c", "d" });
            var fitresult1 = Fitting.FitCurve(contr, refcount, new List<double>() { maxv, 2, max, 0 }, new List<double>() { 1, 1, 1, 1 }, AlgorithmType.LevenbergMarquardt, 5000);
            //设置拟合线
            var fitx = new D1NumricLinearScanRange(0, GetInputParamValueByName("CountLimit"), 500).ScanPoints;
            var fity = fitx.Select(x => GaussFunc(x, fitresult1["a"], fitresult1["b"], fitresult1["c"], fitresult1["d"])).ToList();

            //添加拟合曲线
            D1FitDatas.Add(new FittedData1D(gaussFitExpression, "x", new List<string>() { "a", "b", "c", "d" }, new List<double>() { fitresult1["a"], fitresult1["b"], fitresult1["c"], fitresult1["d"] }, "光子数", "收集光子数据",
                new NumricDataSeries("高斯拟合", fitx, fity) { LineColor = Colors.LightSkyBlue }));

            UpdatePlotChartFlow(true);
            UpdatePlotChart();
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> PlotData = new List<ParentPlotDataPack>();
            return PlotData;
        }
    }
}
