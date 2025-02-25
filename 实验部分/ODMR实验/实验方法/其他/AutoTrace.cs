using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Controls.Charts;
using Controls.Windows;
using HardWares.射频源.Rigol_DSG_3060;
using MathLib.NormalMath.Decimal;
using MathLib.NormalMath.Decimal.Function;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// AutoTrace定位NV
    /// </summary>
    class AutoTrace : ODMRExpObject
    {
        public override string ODMRExperimentName { get; set; } = "AutoTrace";

        public override string ODMRExperimentGroupName { get; set; } = "定位操作";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            //0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
            new Param<double>("X扫描范围",0.5,"XRange"),
            new Param<int>("X扫描点数",20,"XCount"),
            new Param<double>("Y扫描范围",0.5,"YRange"),
            new Param<int>("Y扫描点数",20,"YCount"),
            new Param<double>("Z扫描范围",0.5,"ZRange"),
            new Param<int>("Z扫描点数",20,"ZCount"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
           new Param<double>("X峰值",20,"XPeak"),
           new Param<double>("Y峰值",20,"YPeak"),
           new Param<double>("Z峰值",20,"ZPeak"),
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("APD Trace源","","TraceSource")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头X","","LenX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头Y","","LenY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头Z","","LenZ")),
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();

        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>()
            ;
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();
        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        /// <summary>
        /// 是否等待拟合曲线显示
        /// </summary>
        private bool WaitToDisplayFit = true;

        public AutoTrace()
        {
        }

        public AutoTrace(bool waitToDisplayFit = true)
        {
            WaitToDisplayFit = waitToDisplayFit;
        }

        public List<object> FirstScanEvent(NanoStageInfo device, ScanRange range, double locvalue, List<object> inputParams)
        {
            return ScanEvent(device, range, locvalue, inputParams);
        }

        public override bool PreConfirmProcedure()
        {
            return true;
        }

        public List<object> ScanEvent(NanoStageInfo device, ScanRange range, double locvalue, List<object> inputParams)
        {
            //移动位移台
            device.Device.MoveToAndWait(locvalue, 500);
            //打开APD
            APDInfo info = GetDeviceByName("APD") as APDInfo;
            info.StartContinusSample();
            //光子采样
            double rat = (GetDeviceByName("APD") as APDInfo).GetContinusSampleRatio();
            info.EndContinusSample();

            if (device == GetDeviceByName("LenX"))
            {
                (Get1DChartData("位置", "AutoTrace X") as NumricChartData1D).Data.Add(locvalue);
                (Get1DChartData("计数", "AutoTrace X") as NumricChartData1D).Data.Add(rat);
            }
            if (device == GetDeviceByName("LenY"))
            {
                (Get1DChartData("位置", "AutoTrace Y") as NumricChartData1D).Data.Add(locvalue);
                (Get1DChartData("计数", "AutoTrace Y") as NumricChartData1D).Data.Add(rat);
            }
            if (device == GetDeviceByName("LenZ"))
            {
                (Get1DChartData("位置", "AutoTrace Z") as NumricChartData1D).Data.Add(locvalue);
                (Get1DChartData("计数", "AutoTrace Z") as NumricChartData1D).Data.Add(rat);
            }
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        private double GaussFunc(double x, params double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            double d = ps[3];

            return a * Math.Exp(-(x - c) * (x - c) * 4 * Math.Log(2) / (b * b)) + d;
        }

        public override void ODMRExperiment()
        {
            //高斯拟合函数表达式
            string gaussFitExpression = "a*exp(-(x-c)*(x-c)*4*ln(2)/(b*b))+d";
            CurveFitting Fitting = new CurveFitting(gaussFitExpression, "x", new List<string>() { "a", "b", "c", "d" });

            Scan1DSession<NanoStageInfo> session = new Scan1DSession<NanoStageInfo>();
            session.StateJudgeEvent = JudgeThreadEndOrResume;
            session.ProgressBarMethod = new Action<NanoStageInfo, double>((dev, v) =>
              {
                  SetProgress(v);
              });
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;

            #region 扫X方向
            Show1DChartData("AutoTrace X", "位置", "计数");
            session.ScanSource = GetDeviceByName("LenX") as NanoStageInfo;
            session.SetStateMethod = new Action<NanoStageInfo, double>((dev, v) =>
            {
                SetExpState("正在扫描," + "镜头X位置:" + Math.Round(v, 5).ToString());
            });
            double range = GetInputParamValueByName("XRange");
            int count = GetInputParamValueByName("XCount");
            double pos = (GetDeviceByName("LenX") as NanoStageInfo).Device.Position;
            session.BeginScan(new ScanRange(pos - range / 2, pos + range / 2, count), 0, 100.0 / 3);
            //拟合
            var locs = (Get1DChartData("位置", "AutoTrace X") as NumricChartData1D).Data;
            var counts = (Get1DChartData("计数", "AutoTrace X") as NumricChartData1D).Data;
            //对比度
            double a = counts.Max() - counts.Min();
            //峰宽
            double b = 1.2;
            //最大值位置
            double c = locs[counts.IndexOf(counts.Max())];
            //偏移量
            double d = counts.Min();
            var fitresult1 = Fitting.FitCurve(locs, counts, new List<double>() { a, b, c, d }, new List<double>() { 1, 1, 1, 1 }, AlgorithmType.LevenbergMarquardt);

            a = fitresult1["a"];
            b = fitresult1["b"];
            c = fitresult1["c"];
            d = fitresult1["d"];

            SetOutputParamByName("XPeak", c);
            //设置拟合线
            var fitx = new ScanRange(pos - range / 2, pos + range / 2, 500).GenerateScanList();
            var fity = fitx.Select(x => GaussFunc(x, a, b, c, d)).ToList();

            //添加拟合曲线
            D1FitDatas.Add(new FittedData1D(gaussFitExpression, "x", new List<string>() { "a", "b", "c", "d" }, new List<double>() { a, b, c, d }, "位置", "AutoTrace X",
                new NumricDataSeries("高斯拟合(X)", fitx, fity) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            Show1DFittedData("高斯拟合(X)");
            if (WaitToDisplayFit)
            {
                Thread.Sleep(1500);
            }
            //移动位移台
            double newloc = c;
            if (newloc < pos - range / 2)
                newloc = pos - range / 2;
            if (newloc > pos + range / 2)
                newloc = pos + range / 2;
            session.ScanSource.Device.MoveToAndWait(newloc, 1000);
            #endregion

            #region 扫Y方向
            Show1DChartData("AutoTrace Y", "位置", "计数");
            session.ScanSource = GetDeviceByName("LenY") as NanoStageInfo;
            session.SetStateMethod = new Action<NanoStageInfo, double>((dev, v) =>
            {
                SetExpState("正在扫描," + "镜头Y位置:" + Math.Round(v, 5).ToString());
            });
            range = GetInputParamValueByName("YRange");
            count = GetInputParamValueByName("YCount");
            pos = (GetDeviceByName("LenY") as NanoStageInfo).Device.Position;
            session.BeginScan(new ScanRange(pos - range / 2, pos + range / 2, count), 100.0 / 3, 2 * 100.0 / 3);
            //拟合
            locs = (Get1DChartData("位置", "AutoTrace Y") as NumricChartData1D).Data;
            counts = (Get1DChartData("计数", "AutoTrace Y") as NumricChartData1D).Data;
            //对比度
            a = counts.Max() - counts.Min();
            //峰宽
            b = 1.2;
            //最大值位置
            c = locs[counts.IndexOf(counts.Max())];
            //偏移量
            d = counts.Min();
            fitresult1 = Fitting.FitCurve(locs, counts, new List<double>() { a, b, c, d }, new List<double>() { 1, 1, 1, 1 }, AlgorithmType.LevenbergMarquardt);

            a = fitresult1["a"];
            b = fitresult1["b"];
            c = fitresult1["c"];
            d = fitresult1["d"];

            SetOutputParamByName("YPeak", c);
            //设置拟合线
            fitx = new ScanRange(pos - range / 2, pos + range / 2, 500).GenerateScanList();
            fity = fitx.Select(x => GaussFunc(x, a, b, c, d)).ToList();

            //添加拟合曲线
            D1FitDatas.Add(new FittedData1D(gaussFitExpression, "x", new List<string>() { "a", "b", "c", "d" }, new List<double>() { a, b, c, d }, "位置", "AutoTrace Y",
          new NumricDataSeries("高斯拟合(Y)", fitx, fity) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            Show1DFittedData("高斯拟合(Y)");
            if (WaitToDisplayFit)
            {
                Thread.Sleep(1500);
            }
            //移动位移台
            newloc = c;
            if (newloc < pos - range / 2)
                newloc = pos - range / 2;
            if (newloc > pos + range / 2)
                newloc = pos + range / 2;
            session.ScanSource.Device.MoveToAndWait(newloc, 1000);
            #endregion

            #region 扫Z方向
            Show1DChartData("AutoTrace Z", "位置", "计数");
            session.ScanSource = GetDeviceByName("LenZ") as NanoStageInfo;
            session.SetStateMethod = new Action<NanoStageInfo, double>((dev, v) =>
            {
                SetExpState("正在扫描," + "镜头Z位置:" + Math.Round(v, 5).ToString());
            });
            range = GetInputParamValueByName("ZRange");
            count = GetInputParamValueByName("ZCount");
            pos = (GetDeviceByName("LenZ") as NanoStageInfo).Device.Position;
            session.BeginScan(new ScanRange(pos - range / 2, pos + range / 2, count), 2 * 100.0 / 3, 100);
            //拟合
            locs = (Get1DChartData("位置", "AutoTrace Z") as NumricChartData1D).Data;
            counts = (Get1DChartData("计数", "AutoTrace Z") as NumricChartData1D).Data;
            //对比度
            a = counts.Max() - counts.Min();
            //峰宽
            b = 1.2;
            //最大值位置
            c = locs[counts.IndexOf(counts.Max())];
            //偏移量
            d = counts.Min();
            fitresult1 = Fitting.FitCurve(locs, counts, new List<double>() { a, b, c, d }, new List<double>() { 1, 1, 1, 1 }, AlgorithmType.LevenbergMarquardt);

            a = fitresult1["a"];
            b = fitresult1["b"];
            c = fitresult1["c"];
            d = fitresult1["d"];

            SetOutputParamByName("ZPeak", c);

            //设置拟合线
            fitx = new ScanRange(pos - range / 2, pos + range / 2, 500).GenerateScanList();
            fity = fitx.Select(x => GaussFunc(x, a, b, c, d)).ToList();

            //添加拟合曲线
            D1FitDatas.Add(new FittedData1D(gaussFitExpression, "x", new List<string>() { "a", "b", "c", "d" }, new List<double>() { a, b, c, d }, "位置", "AutoTrace Z",
                new NumricDataSeries("高斯拟合(Z)", fitx, fity) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            Show1DFittedData("高斯拟合(Z)");
            if (WaitToDisplayFit)
            {
                Thread.Sleep(1500);
            }
            //移动位移台
            newloc = c;
            if (newloc < pos - range / 2)
                newloc = pos - range / 2;
            if (newloc > pos + range / 2)
                newloc = pos + range / 2;
            session.ScanSource.Device.MoveToAndWait(newloc, 1000);
            #endregion
        }

        /// <summary>
        /// 实验前操作
        /// </summary>
        public override void PreExpEvent()
        {
            //设置图表
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("位置","AutoTrace X",ChartDataType.X),
                new NumricChartData1D("位置","AutoTrace Y",ChartDataType.X),
                new NumricChartData1D("位置","AutoTrace Z",ChartDataType.X),
                new NumricChartData1D("计数","AutoTrace X",ChartDataType.Y),
                new NumricChartData1D("计数","AutoTrace Y",ChartDataType.Y),
                new NumricChartData1D("计数","AutoTrace Z",ChartDataType.Y),
            };
            D1FitDatas = new List<FittedData1D>();
            UpdatePlotChart();
            //打开激光
            LaserOn lon = new LaserOn();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            lon.CoreMethod(new List<object>() { 1.0 }, pb);
            //打开APD Trace触发源
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.PulseFrequency = 10;
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.Start();
        }

        public override void AfterExpEvent()
        {
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            //关闭激光
            LaserOff loff = new LaserOff();
            loff.CoreMethod(new List<object>() { pb.FindChannelEnumOfDescription("激光触发源") }, pb);
            //关闭APD Trace触发源
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.End();
        }
    }
}
