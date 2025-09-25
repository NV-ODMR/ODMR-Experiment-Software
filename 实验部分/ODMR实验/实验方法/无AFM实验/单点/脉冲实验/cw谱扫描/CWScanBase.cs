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
using OpenCvSharp.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描
{
    public abstract class CWBase : PulseExpBase
    {
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

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private List<object> FirstScanEvent(SignalGeneratorChannelInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
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

        private List<object> ScanEvent(SignalGeneratorChannelInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            PulsePhotonPack pack = DoPulseExp("CW", locvalue, GetRFPower(), GetLoopCount(), 4, GetPointTimeout());

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
            return new List<object>();
        }

        public abstract List<double> GetScanFrequences();

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
        /// 获取循环次数
        /// </summary>
        /// <returns></returns>
        protected abstract int GetLoopCount();

        /// <summary>
        /// 获取单点超时时间
        /// </summary>
        /// <returns></returns>
        protected abstract int GetPointTimeout();

        /// <summary>
        /// 获取微波功率
        /// </summary>
        /// <returns></returns>
        protected abstract double GetRFPower();

        /// <summary>
        /// 扫描完成后设置输出结果
        /// </summary>
        protected abstract void SetOutput();

        public override void ODMRExpWithoutAFM()
        {
            Scan1DSession<SignalGeneratorChannelInfo> session = new Scan1DSession<SignalGeneratorChannelInfo>();
            session.FirstScanEvent = FirstScanEvent;
            session.ScanEvent = ScanEvent;
            var dev = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            dev.Device.IsOutOpen = true;
            session.ScanSource = dev;
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            session.ProgressBarMethod = new Action<SignalGeneratorChannelInfo, double>((sour, v) =>
            {
                SetProgress(v);
            });
            session.SetStateMethod = new Action<SignalGeneratorChannelInfo, double>((sour, v) =>
            {
                SetExpState("CW谱扫描,当前频率:" + Math.Round(v, 5).ToString());
            });
            session.BeginScan(new D1NumricListScanRange(GetScanFrequences()), 0, 100);
        }

        protected List<double> GetFrequences()
        {
            return (Get1DChartData("频率", "CW对比度数据") as NumricChartData1D).Data;
        }

        protected List<double> GetContracts()
        {
            return (Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D).Data; ;
        }

        protected List<double> GetReferenceCounts()
        {
            return (Get1DChartData("参考信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
        }

        protected List<double> GetSignalCounts()
        {
            return (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
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

        protected void FitCWPeaks(int peakcounts, List<double> freqs, List<double> contracts)
        {
            var FittedData = new List<List<double>>();
            var RawFitData = new List<List<double>>();
            var ParamNames = new List<string>();


            string expression = "-(0.25*a*b*b)/((x-c)*(x-c)+0.25*b*b)";
            string origin = "";

            for (int i = 0; i < peakcounts; i++)
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
    }
}
