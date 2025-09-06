using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using MathLib.NormalMath.Decimal;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描
{
    public class AdjustedCW : PulseExpBase
    {
        public override string ODMRExperimentName { get; set; } = "自适应CW谱";
        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
            };
        }

        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>();

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("频率起始点(MHz)",2830,"RFFreqLo"),
            new Param<double>("频率中止点(MHz)",2890,"RFFreqHi"),
            new Param<double>("扫描步长(MHz)",1,"RFStep"),
            new Param<double>("微波功率(dBm)",-20,"RFPower"),
            new Param<bool>("反向扫描",false,"Reverse"),
            new Param<int>("扫描峰个数",1,"PeakCount"),
            new Param<double>("对比度判别阈值",-0.05,"Threshold"),
            new Param<int>("结束前扫描点数",5,"BeforeEndScanRange"),
            new Param<int>("循环次数",1000,"LoopCount"),
            new Param<int>("单点扫描时间上限(ms)",10000,"TimeOut"),
            new Param<double>("预计峰宽",5,"PeakWidth"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private List<object> FirstScanEvent(SignalGeneratorInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("频率","CW对比度数据",ChartDataType.X),
                new NumricChartData1D("对比度","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("频率","CW荧光计数",ChartDataType.X),
                new NumricChartData1D("信号总计数","CW荧光计数",ChartDataType.Y),
                new NumricChartData1D("参考信号总计数","CW荧光计数",ChartDataType.Y),
            };
            UpdatePlotChart();
            Show1DChartData("CW对比度数据", "频率", "对比度");
            return ScanEvent(device, range, locvalue, inputParams);
        }

        bool BeginEnd = false;
        int EndPointCount = 0;

        private List<object> ScanEvent(SignalGeneratorInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            PulsePhotonPack pack = DoPulseExp("CW", locvalue, GetInputParamValueByName("RFPower"), GetInputParamValueByName("LoopCount"), 4, GetInputParamValueByName("TimeOut"));

            double signal = pack.GetPhotonsAtIndex(0).Sum();
            double reference = pack.GetPhotonsAtIndex(1).Sum();
            double contrast = 0;
            try
            {
                contrast = (signal - reference) / reference;
            }
            catch (Exception)
            {
            }

            (Get1DChartData("频率", "CW对比度数据") as NumricChartData1D).Data.Add(locvalue);
            (Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D).Data.Add(contrast);
            (Get1DChartData("频率", "CW荧光计数") as NumricChartData1D).Data.Add(locvalue);
            (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data.Add(signal);
            (Get1DChartData("参考信号总计数", "CW荧光计数") as NumricChartData1D).Data.Add(reference);
            UpdatePlotChartFlow(true);

            //判断是否要停止
            CalculateOutput(out var ps, out var cs, out var ws);
            if (cs.Where(x => x < GetInputParamValueByName("Threshold")).Count() == GetInputParamValueByName("PeakCount"))
            {
                BeginEnd = true;
            }
            if (BeginEnd)
            {
                if (EndPointCount >= GetInputParamValueByName("BeforeEndScanRange"))
                {
                    //结束实验
                    throw new Exception("自适应扫描结束");
                }
                ++EndPointCount;
            }
            return new List<object>();
        }

        protected override void InnerRead(FileObject fobj)
        {
        }

        protected override void InnerWrite(FileObject obj)
        {
        }

        public override void PreExpEventWithoutAFM()
        {
        }

        public override void AfterExpEventWithoutAFM()
        {
            SetOutput();
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

        /// <summary>
        /// 扫描完成后设置输出结果
        /// </summary>
        protected void CalculateOutput(out List<double> Peaks, out List<double> Contracts, out List<double> Widths)
        {
            Peaks = new List<double>();
            Contracts = new List<double>();
            Widths = new List<double>();

            int deducetime = 0;
            deducetime = GetInputParamValueByName("PeakCount");
            List<double> freqs = Get1DChartDataSource("频率", "CW对比度数据").ToArray().ToList();
            List<double> contracts = Get1DChartDataSource("对比度", "CW对比度数据").ToArray().ToList();


            string expression = "-(0.25*a*b*b)/((x-c)*(x-c)+0.25*b*b)";

            for (int i = 0; i < deducetime; i++)
            {
                try
                {
                    double min = contracts.Min();
                    if (min < -0.05)
                        min = min;
                    double peakwidth = GetInputParamValueByName("PeakWidth");
                    double peakloc = freqs[contracts.IndexOf(contracts.Min())];
                    //拟合
                    var ps = CurveFitting.FitCurveWithFunc(freqs, contracts, new List<double>() { Math.Abs(min), peakwidth, peakloc }, new List<double>() { 10, 10, 100 }, LorentzFunc, AlgorithmType.LevenbergMarquardt, 2000);
                    double fittedloc = ps[2];
                    var data = contracts.Select(x => Math.Abs(x + ps[0])).ToList();
                    double fittedcontrast = contracts[data.IndexOf(data.Min())];
                    double fittedpeakwidth = ps[1];
                    contracts = contracts.Zip(freqs.Select(x => LorentzFunc(x, new double[] { Math.Abs(fittedcontrast), fittedpeakwidth, fittedloc })), new Func<double, double, double>((x, y) => x - y)).ToList();
                    if (Peaks.Count == 0 || Math.Abs(Peaks.Last() - fittedloc) > 0.1)
                    {
                        Peaks.Add(fittedloc);
                        Contracts.Add(fittedcontrast);
                        Widths.Add(fittedpeakwidth);
                    }
                }
                catch (Exception)
                {
                    Contracts.Add(0);
                    Widths.Add(0);
                    Peaks.Add(0);
                }
            }
        }


        protected void SetOutput()
        {
            //根据拟合类型设置输出参数
            OutputParams.Clear();
            //平均光子计数率
            OutputParams.Add(new Param<double>("平均光子总数", Get1DChartDataSource("参考信号总计数", "CW荧光计数").Average(), "AverageCounts"));
            int deducetime = GetInputParamValueByName("PeakCount");
            List<double> freqs = Get1DChartDataSource("频率", "CW对比度数据");
            List<double> contracts = Get1DChartDataSource("对比度", "CW对比度数据");

            List<List<double>> FittedData = new List<List<double>>();
            List<List<double>> RawFitData = new List<List<double>>();
            List<string> ParamNames = new List<string>();


            string expression = "-(0.25*a*b*b)/((x-c)*(x-c)+0.25*b*b)";
            string origin = "";

            for (int i = 0; i < deducetime; i++)
            {
                try
                {
                    double min = contracts.Min();
                    double peakwidth = GetInputParamValueByName("PeakWidth");
                    double peakloc = freqs[contracts.IndexOf(contracts.Min())];
                    //拟合
                    var ps = CurveFitting.FitCurveWithFunc(freqs, contracts, new List<double>() { Math.Abs(min), peakwidth, peakloc }, new List<double>() { 10, 10, 100 }, LorentzFunc, AlgorithmType.LevenbergMarquardt, 2000);
                    double fittedloc = ps[2];
                    var data = contracts.Select(x => Math.Abs(x + ps[0])).ToList();
                    double fittedcontrast = contracts[data.IndexOf(data.Min())];
                    double fittedpeakwidth = ps[1];
                    contracts = contracts.Zip(freqs.Select(x => LorentzFunc(x, new double[] { Math.Abs(fittedcontrast), fittedpeakwidth, fittedloc })), new Func<double, double, double>((x, y) => x - y)).ToList();
                    FittedData.Add(new List<double>() { fittedloc, fittedcontrast, fittedpeakwidth });
                    RawFitData.Add(ps.ToList());
                }
                catch (Exception)
                {
                    RawFitData.Add(new List<double>() { 0, 0, 0 });
                    FittedData.Add(new List<double>() { 0, 0, 0 });
                }
                ParamNames.AddRange(new List<string>() { "a" + i.ToString(), "b" + i.ToString(), "c" + i.ToString() });
                origin += expression.Replace("a", "a" + i.ToString()).Replace("b", "b" + i.ToString()).Replace("c", "c" + i.ToString());
            }

            //添加拟合曲线
            List<double> fitdata = new List<double>();
            foreach (var item in FittedData)
            {
                fitdata.AddRange(item);
            }
            List<double> xs = new D1NumricLinearScanRange(freqs.Min(), freqs.Max(), 500).ScanPoints;
            List<double> ys = null;
            foreach (var item in RawFitData)
            {
                if (ys == null)
                {
                    ys = xs.Select(x => LorentzFunc(x, item.ToArray())).ToList();
                }
                else
                {
                    ys = ys.Zip(xs.Select(x => LorentzFunc(x, item.ToArray())), new Func<double, double, double>((x, y) => x + y)).ToList();
                }
            }
            D1FitDatas.Add(new FittedData1D(origin, "x", ParamNames, fitdata, "频率", "CW对比度数据", new NumricDataSeries("拟合数据", xs, ys) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            Show1DFittedData("拟合数据");

            //将结果按频率从低到高排列
            FittedData.Sort((d1, d2) => { return d1[0].CompareTo(d2[0]); });
            for (int i = 0; i < FittedData.Count; i++)
            {
                OutputParams.Add(new Param<double>("谱峰位置" + (i + 1).ToString(), FittedData[i][0], "PeakLoc" + (i + 1).ToString()));
                OutputParams.Add(new Param<double>("谱峰对比度" + (i + 1).ToString(), FittedData[i][1], "PeakContrast" + (i + 1).ToString()));
                OutputParams.Add(new Param<double>("谱峰峰宽" + (i + 1).ToString(), FittedData[i][2], "PeakWidth" + (i + 1).ToString()));
            }
        }


        public override void ODMRExpWithoutAFM()
        {
            Scan1DSession<SignalGeneratorInfo> session = new Scan1DSession<SignalGeneratorInfo>();
            session.FirstScanEvent = FirstScanEvent;
            session.ScanEvent = ScanEvent;
            var dev = GetDeviceByName("RFSource") as SignalGeneratorInfo;
            session.ScanSource = dev;
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            session.ProgressBarMethod = new Action<SignalGeneratorInfo, double>((sour, v) =>
            {
                SetProgress(v);
            });
            session.SetStateMethod = new Action<SignalGeneratorInfo, double>((sour, v) =>
            {
                SetExpState(BeginEnd ? "即将停止" : "" + "CW谱扫描,当前频率:" + Math.Round(v, 5).ToString());
            });
            BeginEnd = false;
            EndPointCount = 0;
            try
            {
                session.BeginScan(new D1NumricLinearScanRange(GetInputParamValueByName("RFFreqLo"),
                        GetInputParamValueByName("RFFreqHi"), GetInputParamValueByName("RFStep"), GetInputParamValueByName("Reverse")), 0, 100);
            }
            catch (Exception ex)
            {
                if (ex.Message == "自适应扫描结束") return;
                else
                    throw ex;
            }
        }

        protected List<double> GetSignalCounts()
        {
            return (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }
    }
}
