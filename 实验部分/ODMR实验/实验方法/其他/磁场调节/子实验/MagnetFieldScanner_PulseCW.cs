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
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲CW谱扫描;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他.磁场调节.子实验
{
    /// <summary>
    /// 磁场扫描
    /// </summary>
    class MagnetFieldScanner_PulseCW : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        public override string ODMRExperimentName { get; set; } = "磁场方向遍历扫描(Theta-脉冲谱)";
        public override string ODMRExperimentGroupName { get; set; } = "磁场定位";

        public override string Description { get; set; } = "";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<string>("定位结果文件名","","LocFileName"),
            new Param<int>("CW/Rabi重新采样间隔",5,"CWGap"),
            new Param<double>("磁铁高度(mm)",13.5,"MagnetHeight"),
            new Param<double>("方位角Phi位置",0,"PhiLoc"),
            new Param<double>("Theta下限",0,"DownThetaLoc"),
            new Param<double>("Theta上限",0,"UpThetaLoc"),
            new Param<double>("Theta扫描步长",0.1,"ThetaStep"),
            new Param<int>("微波频率初始值(MHz)",2870,"CWInit"),
            new Param<bool>("CW谱扫描反向",false,"CWRev"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<double>("脉冲谱峰扫描宽度(MHz)",10,"PulseScanWidth"),
            new Param<double>("脉冲谱峰扫描步长(MHz)",0.1,"PulseScanStep"),
            new Param<int>("序列循环次数",100000,"SeqLoopCount"),
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

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
                new AutoTrace(),
                new 无AFM.点实验.CW谱扫描.AdjustedCW(),
                new Rabi(),
                new 无AFM.点实验.脉冲CW谱扫描.TotalCW()
            };
        }

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
            Scan1DSession<object> PointsScanSession = new Scan1DSession<object>();
            PointsScanSession.ProgressBarMethod = new Action<object, double>((d1, v) => { SetProgress(v); });
            PointsScanSession.FirstScanEvent = StartScanEvent;
            PointsScanSession.ScanEvent = ScanEvent;
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            PointsScanSession.SetStateMethod = new Action<object, double>((d1, p) =>
            {
                SetExpState("当前磁场角度  θ:" + Math.Round(p, 5).ToString() + "  ψ:" + Math.Round(GetInputParamValueByName("PhiLoc"), 5).ToString());
            });
            PointsScanSession.BeginScan(new D1NumricLinearScanRange(GetInputParamValueByName("DownThetaLoc"), GetInputParamValueByName("UpThetaLoc"), GetInputParamValueByName("ThetaStep")), 0, 100, (double)GetInputParamValueByName("CWInit"));
        }

        private List<object> StartScanEvent(object arg1, D1ScanRangeBase arg3, double arg4, List<object> arg5)
        {
            return ScanEvent(arg1, arg3, arg4, arg5);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
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
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        private List<object> ScanEvent(object arg1, D1ScanRangeBase arg3, double arg4, List<object> arg5)
        {
            //移动位移台
            MagnetScanTool.CalculatepPredictLoc(Ps, GetInputParamValueByName("MagnetHeight"), arg4, GetInputParamValueByName("PhiLoc"), out double x, out double y, out double z, out double angle);
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(x, 10000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(y, 10000);
            (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(z, 10000);
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(angle, 60000);
            ++tempCWCount;
            if (tempCWCount >= GetInputParamValueByName("CWGap"))
            {
                tempCWCount = 0;
                //RunSubExperimentBlock(0, true);
                MagnetScanTool.ScanCW(this, 1, out List<double> peaks, out List<double> cs, out List<double> fvs, out List<double> cvs, (double)arg5[0], 5, 10, GetInputParamValueByName("CWRev"));
                if (peaks.Count == 0)
                {

                }
                else
                {
                    arg5 = new List<object>() { peaks[0] };
                }
                RunSubExperimentBlock(2, true, new List<ParamB>() { new Param<double>("微波频率(MHz)", (double)arg5[0], "RFFrequency") });
            }
            var exp = RunSubExperimentBlock(3, true, new List<ParamB>() {
                    new Param<double>("频率起始点(MHz)",(double)arg5[0]-GetInputParamValueByName("PulseScanWidth")/2,"RFFreqLo"),
                    new Param<double>("频率中止点(MHz)",(double)arg5[0]+GetInputParamValueByName("PulseScanWidth")/2,"RFFreqHi"),
                    new Param<double>("扫描步长(MHz)",GetInputParamValueByName("PulseScanStep"),"RFStep")
                });
            var freq = exp.Get1DChartDataSource("频率", "对比度数据");
            var cons = exp.Get1DChartDataSource("对比度1", "对比度数据");
            if (Get1DChartData("频率", "对比度数据") == null)
            {
                D1ChartDatas.Add(new NumricChartData1D("频率", "对比度数据", ChartDataType.X) { Data = freq });
            }
            D1ChartDatas.Add(new NumricChartData1D("对比度[θ=" + Math.Round(arg4, 3).ToString() + "]", "对比度数据", ChartDataType.Y) { Data = cons });
            UpdatePlotChart();
            UpdatePlotChartFlow(false);
            return arg5;
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
