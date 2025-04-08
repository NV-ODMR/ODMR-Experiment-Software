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
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他.磁场调节.通用方法;
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
    class MagnetFieldScanner : ODMRExperimentWithoutAFM
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = true;

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        public override string ODMRExperimentName { get; set; } = "磁场方向遍历扫描";
        public override string ODMRExperimentGroupName { get; set; } = "磁场定位";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<string>("定位结果文件名","","LocFileName"),
        };

        MagnetLocParams Ps = null;

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
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        public override void ODMRExpWithoutAFM()
        {
            CustomScan2DSession<object, object> PointsScanSession = new CustomScan2DSession<object, object>();
            PointsScanSession.ProgressBarMethod = new Action<object, object, double>((d1, d2, v) => { SetProgress(v); });
            PointsScanSession.FirstScanEvent = FirstScanEvent;
            PointsScanSession.ScanEvent = ScanEvent;
            PointsScanSession.StartScanNewLineEvent = ScanEvent;
            PointsScanSession.EndScanNewLineEvent = ScanEvent;
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            PointsScanSession.SetStateMethod = new Action<object, object, Point>((d1, d2, p) =>
            {
                SetExpState("当前磁场角度  θ:" + Math.Round(p.X, 5).ToString() + "  ψ:" + Math.Round(p.Y, 5).ToString());
            });
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
            //读取参数
            Ps = MagnetLocParams.ReadFromFile(GetInputParamValueByName("LocFileName"));
            D1ChartDatas.Clear();
            D2ChartDatas.Clear();
            double xlo = D2ScanRange.XLo;
            double xhi = D2ScanRange.XHi;
            double ylo = D2ScanRange.YLo;
            double yhi = D2ScanRange.YHi;
            int xcount = D2ScanRange.XCount;
            int ycount = D2ScanRange.YCount;
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "CW谱位置1" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "CW谱位置2" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "CW谱对比度1" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "CW谱对比度2" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "垂直轴磁场(Gauss)" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "沿轴磁场(Gauss)" }) { GroupName = "扫描数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "磁场与NV轴夹角(度)" }) { GroupName = "扫描数据" });
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        private List<object> ScanEvent(object arg1, object arg2, D2ScanRangeBase arg3, Point arg4, List<object> arg5)
        {
            //移动位移台
            MagnetScanTool.CalculatepPredictLoc(Ps, Ps.ZLoc, arg4.X, arg4.Y, out var x, out var y, out var z, out var angle);
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(x, 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(y, 10000);
            (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(z, 10000);
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(angle, 60000);
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
            //移动位移台
            MagnetScanTool.CalculatepPredictLoc(Ps, Ps.ZLoc, arg4.X, arg4.Y, out var x, out var y, out var z, out var angle);
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(x, 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(y, 10000);
            (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(z, 10000);
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(angle, 60000);
            RunSubExperimentBlock(1, true);
            bool result = MagnetScanTool.TotalCWPeaks2(this, 0, out var peaks, out var fitcs, out var freqs, out var contrasts);
            if (result == false) throw new Exception("未扫描到完整的谱峰");
            //刷新绘图
            UpdateCWPlot(arg4, peaks, fitcs, freqs, contrasts);
            return new List<object>() { peaks.Min(), peaks.Max() };
        }

        private void UpdateCWPlot(Point p, List<double> peaks, List<double> fitcs, List<double> freqs, List<double> contrasts)
        {
            D1ChartDatas.Add(new NumricChartData1D("θ: " + Math.Round(p.X, 5).ToString() + "  ψ: " + Math.Round(p.Y, 5).ToString() + "频率" + p.X, "CW谱数据", ChartDataType.X) { Data = freqs });
            D1ChartDatas.Add(new NumricChartData1D("θ: " + Math.Round(p.X, 5).ToString() + "  ψ: " + Math.Round(p.Y, 5).ToString() + "对比度" + p.X, "CW谱数据", ChartDataType.Y) { Data = contrasts });
            int indx = D2ScanRange.GetNearestXIndex(p.X);
            int indy = D2ScanRange.GetNearestYIndex(p.Y);
            Get2DChartData("CW谱位置1", "扫描数据").Data.SetValue(indx, indy, peaks.Min());
            Get2DChartData("CW谱位置2", "扫描数据").Data.SetValue(indx, indy, peaks.Max());
            Get2DChartData("CW谱对比度1", "扫描数据").Data.SetValue(indx, indy, fitcs[peaks.FindIndex(x => x == peaks.Min())]);
            Get2DChartData("CW谱对比度2", "扫描数据").Data.SetValue(indx, indy, fitcs[peaks.FindIndex(x => x == peaks.Max())]);
            MagnetScanTool.CalculateB(Ps.D, peaks[0], peaks[1], out var bp, out var bv, out var b);
            Get2DChartData("垂直轴磁场(Gauss)", "扫描数据").Data.SetValue(indx, indy, bv);
            Get2DChartData("沿轴磁场(Gauss)", "扫描数据").Data.SetValue(indx, indy, bp);
            Get2DChartData("磁场与NV轴夹角(度)", "扫描数据").Data.SetValue(indx, indy, Math.Atan2(bv, bp));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>()
            {new KeyValuePair<string, Action>("导入定位文件",LoadFile) };
        }

        private void LoadFile()
        {
            try
            {
                var ps = MagnetLocParams.ReadFromExplorer(out string filepath);

                MessageWindow.ShowTipWindow("导入完成", Window.GetWindow(ParentPage));
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("要打开的文件不是支持的类型", Window.GetWindow(ParentPage));
            }
        }
    }
}
