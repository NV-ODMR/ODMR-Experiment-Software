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
    class CounterWindow : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override string ODMRExperimentName { get; set; } = "光子收集窗口测试";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("光子采集时间起始值(ns)",20,"Countermin"),
            new Param<int>("光子采集时间中止值(ns)",100,"Countermax"),
            new Param<int>("光子采集时间点数(ns)",20,"Counterpoints"),
            new Param<int>("窗口宽度(ns)",20,"WindowWidth"),
            new Param<int>("测量次数",1000,"LoopCount"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
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

        public List<object> FirstScanEvent(object device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            return ScanEvent(device, range, locvalue, inputParams);
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
        public List<object> ScanEvent(object device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            //设置窗口时间
            GlobalPulseParams.SetGlobalPulseLength("CountWindowTime", (int)locvalue);
            //设置窗口宽度
            GlobalPulseParams.SetGlobalPulseLength("CounterWindowWidth", GetInputParamValueByName("WindowWidth"));

            PulsePhotonPack photonpack = DoPulseExp("CounterWindow", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4, GetInputParamValueByName("TimeOut"));

            double signalcount = photonpack.GetPhotonsAtIndex(0).Sum();
            double refcount = photonpack.GetPhotonsAtIndex(1).Sum();

            int ind = range.GetNearestFormalIndex(locvalue);

            var contrfreq = Get1DChartDataSource("采集起始时间(ns)", "收集光子数据");
            var signal = Get1DChartDataSource("亮态平均光子数", "收集光子数据");
            var reference = Get1DChartDataSource("暗态平均光子数", "收集光子数据");

            if (ind >= contrfreq.Count)
            {
                contrfreq.Add(locvalue);
                signal.Add(signalcount);
                reference.Add(refcount);
            }
            else
            {
                signal[ind] = (signal[ind] * CurrentLoop + signalcount) / (CurrentLoop + 1);
                reference[ind] = (reference[ind] * CurrentLoop + refcount) / (CurrentLoop + 1);
            }
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override void ODMRExpWithoutAFM()
        {
            int Loop = GetInputParamValueByName("LoopCount");
            double progressstep = 100 / Loop;
            for (int i = 0; i < Loop; i++)
            {
                CurrentLoop = i;
                Scan1DSession<object> Session = new Scan1DSession<object>();
                Session.FirstScanEvent = FirstScanEvent;
                Session.ScanEvent = ScanEvent;
                Session.ScanSource = null;
                Session.ProgressBarMethod = new Action<object, double>((obj, v) =>
                {
                    SetProgress(v);
                });
                Session.SetStateMethod = new Action<object, double>((obj, v) =>
                {
                    SetExpState("当前扫描轮数:" + i.ToString() + "采集起始时间: " + Math.Round(v, 5).ToString());
                });

                D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("Countermin"), GetInputParamValueByName("Countermax"), GetInputParamValueByName("Counterpoints"));

                Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
                Session.BeginScan(range, progressstep * i, progressstep * (i + 1));
            }
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;

            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("采集起始时间(ns)","收集光子数据",ChartDataType.X),
                new NumricChartData1D("亮态平均光子数","收集光子数据",ChartDataType.Y),
                new NumricChartData1D("暗态平均光子数","收集光子数据",ChartDataType.Y),
            };
            UpdatePlotChart();

            Show1DChartData("收集光子数据", "采集起始时间(ns)", "亮态平均光子数", "暗态平均光子数");
        }

        public override void AfterExpEventWithoutAFM()
        {
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = false;
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            List<ParentPlotDataPack> PlotData = new List<ParentPlotDataPack>();
            return PlotData;
        }
    }
}
