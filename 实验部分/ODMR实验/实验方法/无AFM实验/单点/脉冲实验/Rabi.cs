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
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class Rabi : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override string ODMRExperimentName { get; set; } = "拉比频率测量(Rabi)";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "使用的序列文件名称：Rabi";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            //0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
            new Param<int>("时间最小值(ns)",20,"Rabimin"),
            new Param<int>("时间最大值(ns)",100,"Rabimax"),
            new Param<int>("时间点数(ns)",20,"Rabipoints"),
            new Param<int>("测量次数",1000,"LoopCount"),        //外部的循环
            new Param<MultiScanType>("测量循环类型",MultiScanType.正向扫描,"ScanType"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),   //板卡内的
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"),
            new Param<bool>("将结果同步到全局脉冲",true,"ToGlobal")
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
            GlobalPulseParams.SetGlobalPulseLength("RabiTime", (int)locvalue);

            PulsePhotonPack pack = DoPulseExp("Rabi", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 8, GetInputParamValueByName("TimeOut"));

            //光子数
            double signalcountX = pack.GetPhotonsAtIndex(0).Sum();
            double refcountX = pack.GetPhotonsAtIndex(1).Sum();
            double signalcountY = pack.GetPhotonsAtIndex(2).Sum();
            double refcountY = pack.GetPhotonsAtIndex(3).Sum();

            //计算对比度
            double signalcontrastX = 1;
            double signalcontrastY = 1;
            try
            {
                if (refcountX != 0)
                    signalcontrastX = (signalcountX - refcountX) / refcountX;
            }
            catch (Exception)
            {
            }
            try
            {
                if (refcountY != 0)
                    signalcontrastY = (signalcountY - refcountY) / refcountY;
            }
            catch (Exception)
            {
            }

            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("微波驱动时间(ns)", "Rabi对比度数据", locvalue, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据", signalcontrastX, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据", signalcontrastY, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("微波驱动时间(ns)", "Rabi荧光数据", locvalue, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("通道X 平均光子数", "Rabi荧光数据", refcountX, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("通道X 信号光子数", "Rabi荧光数据", signalcountX, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("通道Y 平均光子数", "Rabi荧光数据", signalcountY, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("通道Y 信号光子数", "Rabi荧光数据", refcountY, new StandardDataProcess()));

            return new List<object>();
        }

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
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("Rabimin"), GetInputParamValueByName("Rabimax"), GetInputParamValueByName("Rabipoints"));

            Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            Session.BeginScan(GetInputParamValueByName("LoopCount"), GetInputParamValueByName("ScanType"), range, 0, 100);
        }

        private void PlotEvent(List<MultiLoopScanData> list)
        {
            (Get1DChartData("微波驱动时间(ns)", "Rabi对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "微波驱动时间(ns)", "Rabi对比度数据");
            (Get1DChartData("通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");
            (Get1DChartData("通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");
            
            (Get1DChartData("微波驱动时间(ns)", "Rabi荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "微波驱动时间(ns)", "Rabi荧光数据");
            (Get1DChartData("通道X 平均光子数", "Rabi荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "通道X 平均光子数", "Rabi荧光数据");
            (Get1DChartData("通道X 信号光子数", "Rabi荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "通道X 信号光子数", "Rabi荧光数据");
            (Get1DChartData("通道Y 平均光子数", "Rabi荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "通道Y 平均光子数", "Rabi荧光数据");
            (Get1DChartData("通道Y 信号光子数", "Rabi荧光数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "通道Y 信号光子数", "Rabi荧光数据");

            (Get1DChartData("微波驱动时间(ns)", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "微波驱动时间(ns)", "Rabi对比度数据");
            (Get1DChartData("通道X 平均光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "通道X 平均光子数", "Rabi荧光数据");
            (Get1DChartData("通道X 信号光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "通道X 信号光子数", "Rabi荧光数据");
            (Get1DChartData("通道Y 平均光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "通道Y 平均光子数", "Rabi荧光数据");
            (Get1DChartData("通道Y 信号光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "通道Y 信号光子数", "Rabi荧光数据");
            (Get1DChartData("通道X Rabi信号对比度[(sig-ref)/ref]", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");
            (Get1DChartData("通道Y Rabi信号对比度[(sig-ref)/ref]", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");


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
                new NumricChartData1D("微波驱动时间(ns)","Rabi对比度数据",ChartDataType.X),
                new NumricChartData1D("通道X Rabi信号对比度[(sig-ref)/ref]","Rabi对比度数据",ChartDataType.Y),
                new NumricChartData1D("通道Y Rabi信号对比度[(sig-ref)/ref]","Rabi对比度数据",ChartDataType.Y),

                new NumricChartData1D("微波驱动时间(ns)","Rabi荧光数据",ChartDataType.X),
                new NumricChartData1D("通道X 平均光子数","Rabi荧光数据",ChartDataType.Y),
                new NumricChartData1D("通道X 信号光子数","Rabi荧光数据",ChartDataType.Y),
                new NumricChartData1D("通道Y 平均光子数","Rabi荧光数据",ChartDataType.Y),
                new NumricChartData1D("通道Y 信号光子数","Rabi荧光数据",ChartDataType.Y),

                new NumricChartData1D("微波驱动时间(ns)","方差",ChartDataType.X),
                new NumricChartData1D("通道X 平均光子数","方差",ChartDataType.Y),
                new NumricChartData1D("通道X 信号光子数","方差",ChartDataType.Y),
                new NumricChartData1D("通道Y 平均光子数","方差",ChartDataType.Y),
                new NumricChartData1D("通道Y 信号光子数","方差",ChartDataType.Y),
                new NumricChartData1D("通道X Rabi信号对比度[(sig-ref)/ref]","方差",ChartDataType.Y),
                new NumricChartData1D("通道Y Rabi信号对比度[(sig-ref)/ref]","方差",ChartDataType.Y),
            };
            UpdatePlotChart();
            Show1DChartData("Rabi对比度数据", "微波驱动时间(ns)", "通道X Rabi信号对比度[(sig-ref)/ref]", "通道Y Rabi信号对比度[(sig-ref)/ref]");//实验前展示的数据
        }

        //T1拟合函数
        private double RabiFitFunc(double x, double[] ps)
        {
            double a = ps[0];
            double tau = ps[1];
            double b = ps[2];
            double c = ps[3];
            double d = ps[4];
            return a * Math.Exp(-x / tau) * Math.Cos(2 * Math.PI / b * (x - c)) + d;
        }

        public override void AfterExpEventWithoutAFM()
        {
            #region 计算通道X Rabi脉冲周期
            var xs = Get1DChartDataSource("微波驱动时间(ns)", "Rabi对比度数据");
            var ys_x = Get1DChartDataSource("通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");

            //设置初值
            double tau = (xs.Min() + xs.Max()) / 2;
            double d_x = ys_x.Average();
            double a_x = Math.Abs(ys_x.Min() - ys_x.Max()) / 2;
            double c_x = 10;
            //Pi脉冲时间
            double b_x = 0;
            try
            {
                var fys_x = ys_x.Select(x => x - ys_x.Average()).ToArray();
                Fourier.Forward(fys_x, Enumerable.Repeat(0.0, ys_x.Count).ToArray(), FourierOptions.Matlab);
                fys_x = fys_x.ToList().GetRange(0, (fys_x.Length - 1) / 2).ToArray();
                var freqs_x = Fourier.FrequencyScale(ys_x.Count, (int)1.0 / Math.Abs(xs[0] - xs[1]));
                freqs_x = freqs_x.ToList().GetRange(0, (freqs_x.Length - 1) / 2).ToArray();
                b_x = 1.0 / freqs_x[fys_x.ToList().IndexOf(fys_x.Max())];
                if (double.IsInfinity(b_x)) b_x = 0;
            }
            catch (Exception)
            {
            }

            double[] ps_x = CurveFitting.FitCurveWithFunc(xs, ys_x, new List<double>() { a_x, tau, b_x, c_x, d_x }, new List<double>() { 10, 10, 10, 10, 10 }, RabiFitFunc, AlgorithmType.LevenbergMarquardt, 20000);

            //设置拟合曲线
            var ftxs = new D1NumricLinearScanRange(xs.Min(), xs.Max(), 500).ScanPoints;
            var fitys_x = ftxs.Select(x => RabiFitFunc(x, ps_x)).ToList();
            D1FitDatas.Add(new FittedData1D("a*exp(-x/t)*cos(2*pi/b*(x-c))+d", "x", new List<string>() { "a", "t", "b", "c", "d" }, ps_x.ToList(), "微波驱动时间(ns)", "Rabi对比度数据", new NumricDataSeries("拟合曲线X", ftxs, fitys_x) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            Show1DFittedData("拟合曲线X");

            OutputParams.Add(new Param<double>("通道X Pi脉冲长度(ns)", ps_x[2] / 2 + ps_x[3], "X_PiLength"));
            OutputParams.Add(new Param<double>("通道X Pi/2脉冲长度(ns)", ps_x[2] / 4 + ps_x[3], "X_HalfPiLength"));
            OutputParams.Add(new Param<double>("通道X 3Pi/2脉冲长度(ns)", 3 * ps_x[2] / 4 + ps_x[3], "X_3HalfPiLength"));
            OutputParams.Add(new Param<double>("通道X 2Pi脉冲长度(ns)", ps_x[2] + ps_x[3], "X_2PiLength"));
            //计算平均光子计数
            OutputParams.Add(new Param<double>("通道X 平均光子计数", Get1DChartDataSource("通道X 平均光子数", "Rabi荧光数据").Average(), "X_AverageCount"));
            #endregion

            #region 计算通道Y Rabi脉冲周期
            //var xs = Get1DChartDataSource("微波驱动时间(ns)", "Rabi对比度数据");
            var ys_y = Get1DChartDataSource("通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据");

            //设置初值
            //double tau = (xs.Min() + xs.Max()) / 2;
            double d_y = ys_y.Average();
            double a_y = Math.Abs(ys_y.Min() - ys_y.Max()) / 2;
            double c_y = 10;
            //Pi脉冲时间
            double b_y = 0;
            try
            {
                var fys_y = ys_y.Select(x => x - ys_y.Average()).ToArray();
                Fourier.Forward(fys_y, Enumerable.Repeat(0.0, ys_y.Count).ToArray(), FourierOptions.Matlab);
                fys_y = fys_y.ToList().GetRange(0, (fys_y.Length - 1) / 2).ToArray();
                var freqs_y = Fourier.FrequencyScale(ys_y.Count, (int)1.0 / Math.Abs(xs[0] - xs[1]));
                freqs_y = freqs_y.ToList().GetRange(0, (freqs_y.Length - 1) / 2).ToArray();
                b_y = 1.0 / freqs_y[fys_y.ToList().IndexOf(fys_y.Max())];
                if (double.IsInfinity(b_y)) b_y = 0;
            }
            catch (Exception)
            {
            }

            double[] ps_y = CurveFitting.FitCurveWithFunc(xs, ys_y, new List<double>() { a_y, tau, b_y, c_y, d_y }, new List<double>() { 10, 10, 10, 10, 10 }, RabiFitFunc, AlgorithmType.LevenbergMarquardt, 20000);

            //设置拟合曲线
            //var ftxs = new D1NumricLinearScanRange(xs.Min(), xs.Max(), 500).ScanPoints;
            var fitys_y = ftxs.Select(x => RabiFitFunc(x, ps_y)).ToList();
            D1FitDatas.Add(new FittedData1D("a*exp(-x/t)*cos(2*pi/b*(x-c))+d", "x", new List<string>() { "a", "t", "b", "c", "d" }, ps_y.ToList(), "微波驱动时间(ns)", "Rabi对比度数据", new NumricDataSeries("拟合曲线Y", ftxs, fitys_y) { LineColor = Colors.LightGreen }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            Show1DFittedData("拟合曲线Y");

            OutputParams.Add(new Param<double>("通道Y Pi脉冲长度(ns)", ps_y[2] / 2 + ps_y[3], "Y_PiLength"));
            OutputParams.Add(new Param<double>("通道Y Pi/2脉冲长度(ns)", ps_y[2] / 4 + ps_y[3], "Y_HalfPiLength"));
            OutputParams.Add(new Param<double>("通道Y 3Pi/2脉冲长度(ns)", 3 * ps_y[2] / 4 + ps_y[3], "Y_3HalfPiLength"));
            OutputParams.Add(new Param<double>("通道Y 2Pi脉冲长度(ns)", ps_y[2] + ps_y[3], "Y_2PiLength"));
            //计算平均光子计数
            OutputParams.Add(new Param<double>("通道Y 平均光子计数", Get1DChartDataSource("通道Y 平均光子数", "Rabi荧光数据").Average(), "Y_AverageCount"));
            #endregion

            //通道x结果
            int pi_x = (int)(ps_x[2] / 2 + ps_x[3]);
            int hpi_x = (int)(ps_x[2] / 4 + ps_x[3]);
            int h3pi_x = (int)(3 * ps_x[2] / 4 + ps_x[3]);
            int pi2_x = (int)(ps_x[2] + ps_x[3]);
            if (pi_x < 0)
                pi_x = (int)(ps_x[2] + ps_x[2] / 2 + ps_x[3]);
            if (hpi_x < 0)
                hpi_x = (int)(ps_x[2] + ps_x[2] / 4 + ps_x[3]);
            if (h3pi_x < 0)
                pi_x = (int)(ps_x[2] + 3 * ps_x[2] / 4 + ps_x[3]);
            if (pi2_x < 0)
                pi_x = (int)(ps_x[2] + ps_x[2] + ps_x[3]);

            //通道y结果
            int pi_y = (int)(ps_y[2] / 2 + ps_y[3]);
            int hpi_y = (int)(ps_y[2] / 4 + ps_y[3]);
            int h3pi_y = (int)(3 * ps_y[2] / 4 + ps_y[3]);
            int pi2_y = (int)(ps_y[2] + ps_y[3]);
            if (pi_y < 0)
                pi_y = (int)(ps_y[2] + ps_y[2] / 2 + ps_y[3]);
            if (hpi_y < 0)
                hpi_y = (int)(ps_y[2] + ps_y[2] / 4 + ps_y[3]);
            if (h3pi_y < 0)
                pi_y = (int)(ps_y[2] + 3 * ps_y[2] / 4 + ps_y[3]);
            if (pi2_y < 0)
                pi_y = (int)(ps_y[2] + ps_y[2] + ps_y[3]);

            if (GetInputParamValueByName("ToGlobal") == true)
            {
                GlobalPulseParams.SetGlobalPulseLength("HalfPiX", hpi_x);
                GlobalPulseParams.SetGlobalPulseLength("PiX", pi_x);
                GlobalPulseParams.SetGlobalPulseLength("3HalfPiX", h3pi_x);
                GlobalPulseParams.SetGlobalPulseLength("2PiX", pi2_x);

                GlobalPulseParams.SetGlobalPulseLength("HalfPiY", hpi_y);
                GlobalPulseParams.SetGlobalPulseLength("PiY", pi_y);
                GlobalPulseParams.SetGlobalPulseLength("3HalfPiY", h3pi_y);
                GlobalPulseParams.SetGlobalPulseLength("2PiY", pi2_y);
            }
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> PlotData = new List<ParentPlotDataPack>();
            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "Rabi对比度数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "Rabi对比度数据"), false));//false有同名就不加，true加到结果里
            PlotData.Add(new ParentPlotDataPack("通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据", ChartDataType.Y, Get1DChartDataSource("通道X Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据"), true));
            PlotData.Add(new ParentPlotDataPack("通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据", ChartDataType.Y, Get1DChartDataSource("通道Y Rabi信号对比度[(sig-ref)/ref]", "Rabi对比度数据"), true));

            PlotData.Add(new ParentPlotDataPack("驰豫时间长度(ns)", "Rabi荧光数据", ChartDataType.X, Get1DChartDataSource("驰豫时间长度(ns)", "Rabi荧光数据"), false));
            PlotData.Add(new ParentPlotDataPack("通道X 平均光子数", "Rabi荧光数据", ChartDataType.Y, Get1DChartDataSource("通道X 平均光子数", "Rabi荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("通道X 信号光子数", "Rabi荧光数据", ChartDataType.Y, Get1DChartDataSource("通道X 信号光子数", "Rabi荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("通道Y 平均光子数", "Rabi荧光数据", ChartDataType.Y, Get1DChartDataSource("通道Y 平均光子数", "Rabi荧光数据"), true));
            PlotData.Add(new ParentPlotDataPack("通道Y 信号光子数", "Rabi荧光数据", ChartDataType.Y, Get1DChartDataSource("通道Y 信号光子数", "Rabi荧光数据"), true));
            return PlotData;
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
    }
}
