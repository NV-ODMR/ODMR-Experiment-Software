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
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
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
    class MagnetFieldScanner_T2 : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = true;

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        public override string ODMRExperimentName { get; set; } = "磁场方向遍历扫描(T2)";
        public override string ODMRExperimentGroupName { get; set; } = "磁场定位";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<string>("定位结果文件名","","LocFileName"),
            new Param<int>("CW/Rabi重新采样间隔",5,"CWGap"),
            new Param<double>("磁铁高度(mm)",13,"MagnetHeight"),
            new Param<int>("微波频率初始值(MHz)",2870,"CWInit"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("T2采样时间",1000,"T2SampleTime"),
            new Param<int>("测量次数",1000,"LoopCount"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),
            new Param<int>("单点超时时间",10000,"TimeOut"),
        };

        MagnetLocParams Ps = null;

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁X轴","","MagnetX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁Y轴","","MagnetY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁Z轴","","MagnetZ")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁角度轴","","MagnetAngle")),
        };

        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>()
        {
            new AutoTrace(),
            new AdjustedCW(),
            new Rabi()
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

        int tempCWCount = 0;
        public override void ODMRExpWithoutAFM()
        {
            tempCWCount = GetInputParamValueByName("CWGap");
            CustomScan2DSession<object, object> PointsScanSession = new CustomScan2DSession<object, object>();
            PointsScanSession.ProgressBarMethod = new Action<object, object, double>((d1, d2, v) => { SetProgress(v); });
            PointsScanSession.FirstScanEvent = StartScanEvent;
            PointsScanSession.ScanEvent = ScanEvent;
            PointsScanSession.StartScanNewLineEvent = StartScanEvent;
            PointsScanSession.EndScanNewLineEvent = ScanEvent;
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            PointsScanSession.SetStateMethod = new Action<object, object, Point>((d1, d2, p) =>
            {
                SetExpState("当前磁场角度  θ:" + Math.Round(p.X, 5).ToString() + "  ψ:" + Math.Round(p.Y, 5).ToString());
            });
            PointsScanSession.BeginScan(D2ScanRange, 0, 100, (double)GetInputParamValueByName("CWInit"));
        }

        private List<object> StartScanEvent(object arg1, object arg2, D2ScanRangeBase arg3, Point arg4, List<object> arg5)
        {
            return ScanEvent(arg1, arg2, arg3, arg4, arg5);
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
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "0态对比度" }) { GroupName = "T2数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "1态对比度" }) { GroupName = "T2数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "0态计数" }) { GroupName = "T2数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "1态计数" }) { GroupName = "T2数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = "方位角θ", YName = "方位角ψ", ZName = "参考计数" }) { GroupName = "T2数据" });
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        private List<object> ScanEvent(object arg1, object arg2, D2ScanRangeBase arg3, Point arg4, List<object> arg5)
        {
            //移动位移台
            MagnetScanTool.CalculatepPredictLoc(Ps, GetInputParamValueByName("MagnetHeight"), arg4.X, arg4.Y, out double x, out double y, out double z, out double angle);
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(x, 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(y, 10000);
            (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(z, 10000);
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(angle, 60000);
            ++tempCWCount;
            if (tempCWCount >= GetInputParamValueByName("CWGap"))
            {
                tempCWCount = 0;
                RunSubExperimentBlock(0, true);
                MagnetScanTool.ScanCW(this, 1, out var peaks, out var cs, out var fvs, out var cvs, (double)arg5[0], 5, 15, false);
                if (peaks.Count == 0)
                {

                }
                else
                {
                    arg5 = new List<object>() { peaks[0] };
                }
                RunSubExperimentBlock(2, true, new List<ParamB>() { new Param<double>("微波频率(MHz)", (double)arg5[0], "RFFrequency") });
            }
            var result = GetT2Data((double)arg5[0]);
            T2Plot(result, arg3.GetNearestXIndex(arg4.X), arg3.GetNearestYIndex(arg4.Y));
            return arg5;
        }


        private List<double> GetT2Data(double frequency)
        {
            GlobalPulseParams.SetGlobalPulseLength("T2Step", GetInputParamValueByName("T2SampleTime"));
            GlobalPulseParams.SetGlobalPulseLength("T2Res", (int)0);
            PulsePhotonPack pack = DoPulseExp("T2", frequency, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 6, GetInputParamValueByName("TimeOut"));

            double signalcount0 = 0;
            double signalcount1 = 0;
            double refcount = 0;

            for (int i = 0; i < GetInputParamValueByName("LoopCount"); i++)
            {
                signalcount0 += pack.GetPhotonsAtIndex(0).Sum();
                signalcount1 += pack.GetPhotonsAtIndex(1).Sum();
                refcount += pack.GetPhotonsAtIndex(2).Sum();
                JudgeThreadEndOrResumeAction?.Invoke();
            }

            double signalcontrast0 = 0;
            double signalcontrast1 = 0;
            try
            {
                signalcontrast0 = (signalcount0 - refcount) / refcount;
                signalcontrast1 = (signalcount1 - refcount) / refcount;
            }
            catch (Exception)
            {
            }

            return new List<double>() { signalcount0, signalcount1, refcount, signalcontrast0, signalcontrast1 };
        }

        private void T2Plot(List<double> data, int rowx, int rowy)
        {
            var c0 = Get2DChartData("0态对比度", "T2数据").Data;
            var c1 = Get2DChartData("1态对比度", "T2数据").Data;
            var coun0 = Get2DChartData("0态计数", "T2数据").Data;
            var coun1 = Get2DChartData("1态计数", "T2数据").Data;
            var refc = Get2DChartData("参考计数", "T2数据").Data;
            c0.SetValue(rowx, rowy, data[3]);
            c1.SetValue(rowx, rowy, data[4]);
            coun0.SetValue(rowx, rowy, data[0]);
            coun1.SetValue(rowx, rowy, data[1]);
            refc.SetValue(rowx, rowy, data[2]);
            UpdatePlotChartFlow(true);
        }


        private void LoadFile()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var ps = MagnetLocParams.ReadFromExplorer(out string filepath);

                    MessageWindow.ShowTipWindow("导入完成", Window.GetWindow(ParentPage));
                    SetInputParamValueByName("LocFileName", filepath);
                }
                catch (Exception)
                {
                    MessageWindow.ShowTipWindow("要打开的文件不是支持的类型", Window.GetWindow(ParentPage));
                }
            });
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>()
            {new KeyValuePair<string, Action>("导入定位文件",LoadFile) };
        }
    }
}
