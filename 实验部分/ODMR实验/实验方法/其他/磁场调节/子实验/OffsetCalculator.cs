using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他.磁场调节.子实验
{
    /// <summary>
    /// 磁场扫描
    /// </summary>
    class OffsetCalculator : ODMRExperimentWithoutAFM
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = true;

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        public override string ODMRExperimentName { get; set; } = "偏心参数测量";
        public override string ODMRExperimentGroupName { get; set; } = "磁场定位";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("起始位置X",double.NaN,"StartLocX"),
            new Param<double>("起始位置Y",double.NaN,"StartLocY"),
            new Param<double>("起始位置Z",double.NaN,"StartLocZ"),
            new Param<double>("起始角度",double.NaN,"StartAngle"),
            new Param<bool>("反转X轴", false, "ReverseX"){ Helper="如果X方向位移台的示数增加方向和规定的X轴的正向不同，则要勾选此选项"},
            new Param<bool>("反转Y轴", false, "ReverseY"){ Helper="如果Y方向位移台的示数增加方向和规定的X轴的正向不同，则要勾选此选项"},
            new Param<bool>("反转角度", false, "ReverseA"){ Helper="如果角度方向位移台的示数增加方向和规定的X轴的正向不同，则要勾选此选项"},
            new Param<double>("角度零点(度)", double.NaN, "AngleStart"){ Helper="当圆柱形磁铁轴向沿X轴时角度位移台的读数"},
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁X轴","","MagnetX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁Y轴","","MagnetY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁Z轴","","MagnetZ")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁角度轴","","MagnetAngle")),
        };

        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>()
        {
            new AdjustedCW(),
            new AutoTrace()
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>() { };
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>() { };
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>() { };

        public override void AfterExpEventWithoutAFM()
        {
            //计算结果
            //找最小值
            var dat = Get2DChartData("误差(errcw1^2+errcw2^2)", "扫描数据").Data;
            double min = double.MaxValue;
            int minxind = -1;
            int minyind = -1;
            for (int i = 0; i < dat.XCounts; i++)
            {
                for (int j = 0; j < dat.YCounts; j++)
                {
                    var value = dat.GetValue(i, j);
                    if (double.IsNaN(value))
                    {
                        continue;
                    }
                    if (value < min)
                    {
                        min = value;
                        minxind = i;
                        minyind = j;
                    }
                }
            }
            if (minxind != -1 && minyind != -1)
            {
                double minx = dat.GetFormattedXData()[minxind];
                double miny = dat.GetFormattedYData()[minyind];
                //计算偏心参数
                OutputParams.Add(new Param<double>("误差最小处X", minx, "MinX"));
                OutputParams.Add(new Param<double>("误差最小处Y", miny, "MinY"));
                MagnetScanTool.GetZeroOffset(GetInputParamValueByName("StartAngle"), GetInputParamValueByName("StartLocX"), GetInputParamValueByName("StartLocY"),
                    GetInputParamValueByName("StartAngle") + 180, minx, miny, GetInputParamValueByName("ReverseX"), GetInputParamValueByName("ReverseY"), GetInputParamValueByName("ReverseA"),
                    GetInputParamValueByName("AngleStart"), out double offx, out double offy);
                OutputParams.Add(new Param<double>("偏心参数X", offx, "OffsetX"));
                OutputParams.Add(new Param<double>("偏心参数Y", offy, "OffsetY"));
            }
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        double cw1 = double.NaN;
        double cw2 = double.NaN;
        public override void ODMRExpWithoutAFM()
        {
            cw1 = double.NaN;
            cw2 = double.NaN;
            //移动位移台到初始位置测量CW谱
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(GetInputParamValueByName("StartLocX"), 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(GetInputParamValueByName("StartLocY"), 10000);
            (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(GetInputParamValueByName("StartLocZ"), 10000);
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(GetInputParamValueByName("StartAngle"), 60000);
            RunSubExperimentBlock(1, true);
            bool result = MagnetScanTool.TotalCWPeaks2(this, 0, out var peaks, out var cs, out var freqs, out var contracts);
            if (result == false)
            {
                throw new Exception("未扫描到完整的CW谱峰");
            }
            cw1 = peaks.Min();
            cw2 = peaks.Max();
            //磁铁转180度
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(GetInputParamValueByName("StartAngle") + 180, 60000);

            CustomScan2DSession<object, object> PointsScanSession = new CustomScan2DSession<object, object>();
            PointsScanSession.ProgressBarMethod = new Action<object, object, double>((d1, d2, v) => { SetProgress(v); });
            PointsScanSession.FirstScanEvent = FirstScanEvent;
            PointsScanSession.ScanEvent = ScanEvent;
            PointsScanSession.StartScanNewLineEvent = ScanEvent;
            PointsScanSession.EndScanNewLineEvent = ScanEvent;
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            PointsScanSession.SetStateMethod = new Action<object, object, Point>((d1, d2, p) =>
            {
                SetExpState("当前位置  X:" + Math.Round(p.X, 5).ToString() + "  Y:" + Math.Round(p.Y, 5).ToString());
            });
            PointsScanSession.BeginScan(D2ScanRange, 0, 100, 0, 0);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                if (D2ScanRange == null)
                {
                    MessageWindow.ShowTipWindow("扫描范围未设置", Window.GetWindow(ParentPage));
                    return false;
                }
                return true;
            }
            return false;
        }

        public override void PreExpEventWithoutAFM()
        {
            D1ChartDatas.Clear();
            D2ChartDatas.Clear();
            double xlo = D2ScanRange.XLo;
            double xhi = D2ScanRange.XHi;
            double ylo = D2ScanRange.YLo;
            double yhi = D2ScanRange.YHi;
            int xcount = D2ScanRange.XCount;
            int ycount = D2ScanRange.YCount;
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "X位置(mm)", YName = "Y位置(mm)", ZName = "CW谱位置1" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "X位置(mm)", YName = "Y位置(mm)", ZName = "CW谱位置2" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "X位置(mm)", YName = "Y位置(mm)", ZName = "CW谱对比度1" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "X位置(mm)", YName = "Y位置(mm)", ZName = "CW谱对比度2" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "X位置(mm)", YName = "Y位置(mm)", ZName = "误差(errcw1^2+errcw2^2)" }) { GroupName = "扫描数据" });
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        private List<object> ScanEvent(object arg1, object arg2, D2ScanRangeBase arg3, Point arg4, List<object> arg5)
        {
            //移动位移台到初始位置测量CW谱
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(arg4.X, 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(arg4.Y, 10000);
            SetExpState("当前位置 X:" + arg4.X.ToString() + "Y: " + arg4.Y.ToString());
            RunSubExperimentBlock(1, true);
            MagnetScanTool.ScanCW2(this, 0, out var peakout1, out var peakout2, out var contrastout1, out var contrastout2, out var freqs, out var contrasts, (double)arg5[0], (double)arg5[1], 10);
            if (peakout1 == 0 || peakout2 == 0)
            {
                bool result = MagnetScanTool.TotalCWPeaks2(this, 0, out var peaks, out var fitcs, out freqs, out contrasts);
                if (result == false)
                {
                    peaks = new List<double>() { double.NaN, double.NaN };
                    contrasts = new List<double>() { double.NaN, double.NaN };
                }
                UpdateCWPlot(arg4, peaks, fitcs, freqs, contrasts);
                if (result == true)
                    return new List<object>() { peaks.Min(), peaks.Max() };
                else
                    return arg5;
            }
            UpdateCWPlot(arg4, new List<double>() { peakout1, peakout2 }, new List<double>() { contrastout1, contrastout2 }, freqs, contrasts);
            return new List<object>() { Math.Min(peakout1, peakout2), Math.Max(peakout1, peakout2) };
        }

        private List<object> FirstScanEvent(object arg1, object arg2, D2ScanRangeBase arg3, Point arg4, List<object> arg5)
        {
            //移动位移台到初始位置测量CW谱
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(arg4.X, 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(arg4.Y, 10000);
            SetExpState("当前位置 X:" + arg4.X.ToString() + "Y: " + arg4.Y.ToString());
            RunSubExperimentBlock(1, true);
            bool result = MagnetScanTool.TotalCWPeaks2(this, 0, out var peaks, out var fitcs, out var freqs, out var contrasts);
            if (result == false) throw new Exception("未扫描到完整的谱峰");
            //刷新绘图
            UpdateCWPlot(arg4, peaks, fitcs, freqs, contrasts);
            return new List<object>() { peaks.Min(), peaks.Max() };
        }

        private void UpdateCWPlot(Point p, List<double> peaks, List<double> fitcs, List<double> freqs, List<double> contrasts)
        {
            D1ChartDatas.Add(new NumricChartData1D("X: " + Math.Round(p.X, 5).ToString() + "  Y: " + Math.Round(p.Y, 5).ToString() + "频率" + p.X, "CW谱数据", ChartDataType.X) { Data = freqs });
            D1ChartDatas.Add(new NumricChartData1D("X: " + Math.Round(p.X, 5).ToString() + "  Y: " + Math.Round(p.Y, 5).ToString() + "对比度" + p.X, "CW谱数据", ChartDataType.Y) { Data = contrasts });
            int indx = D2ScanRange.GetNearestXIndex(p.X);
            int indy = D2ScanRange.GetNearestYIndex(p.Y);
            Get2DChartData("CW谱位置1", "扫描数据").Data.SetValue(indx, indy, peaks.Min());
            Get2DChartData("CW谱位置2", "扫描数据").Data.SetValue(indx, indy, peaks.Max());
            Get2DChartData("CW谱对比度1", "扫描数据").Data.SetValue(indx, indy, fitcs[peaks.FindIndex(x => x == peaks.Min())]);
            Get2DChartData("CW谱对比度2", "扫描数据").Data.SetValue(indx, indy, fitcs[peaks.FindIndex(x => x == peaks.Max())]);
            Get2DChartData("误差(errcw1^2+errcw2^2)", "扫描数据").Data.SetValue(indx, indy, Math.Pow(cw1 - peaks.Min(), 2) + Math.Pow(cw2 - peaks.Max(), 2));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
    }
}
