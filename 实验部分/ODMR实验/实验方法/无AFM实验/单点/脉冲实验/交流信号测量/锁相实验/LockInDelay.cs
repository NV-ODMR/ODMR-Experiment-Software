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
using System.Threading;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class LockInDelay : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool IsDisplayAsExp { get; set; } = false;

        /// <summary>
        /// 所属锁相实验
        /// </summary>
        private LockInExpBase ParentLockInExp { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "锁相Delay测试";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("起点时间(ns)",20,"StartTime"),
            new Param<int>("Delay点数",1000,"DelayCount"),
            new Param<double>("终点时间(ns)",1000,"EndTime"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private int CurrentLoop = 0;

        double contrast = double.NaN;
        double Sig = double.NaN;
        double Ref = double.NaN;

        public override void ODMRExpWithoutAFM()
        {
            Scan1DSession<object> ScanDelayTime = new Scan1DSession<object>();
            ScanDelayTime.ProgressBarMethod = new Action<object, double>((device, v) => { SetProgress(v); });
            ScanDelayTime.ScanEvent = ExpScanEvent;
            ScanDelayTime.FirstScanEvent = ExpScanEvent;
            ScanDelayTime.ScanSource = new object();
            ScanDelayTime.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            ScanDelayTime.SetStateMethod = new Action<object, double>((device, v) => { SetExpState("当前扫描时间:" + Math.Round(v, 5).ToString()); });
            ScanDelayTime.BeginScan(new D1NumricLinearScanRange(GetInputParamValueByName("StartTime"), GetInputParamValueByName("EndTime"), GetInputParamValueByName("DelayCount")),
                0, 100);
        }

        private List<object> ExpScanEvent(object arg1, D1NumricScanRangeBase arg2, double arg3, List<object> arg4)
        {
            GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", (int)arg3);
            var exp = RunSubExperimentBlock(0, false);
            var time = Get1DChartDataSource("时间(ns)", "Delay测试数据");
            time.Add(arg3);
            foreach (var item in exp.OutputParams)
            {
                List<double> source = Get1DChartDataSource(item.Description, "Delay测试数据");
                if (source == null)
                {
                    var data = new NumricChartData1D(item.Description, "Delay测试数据", ChartDataType.Y);
                    D1ChartDatas.Add(data);
                    source = data.Data;
                }
                try
                {
                    source.Add((double)item.RawValue);
                }
                catch (Exception e)
                {
                    source.Add(double.NaN);
                }
            }
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorInfo RF = GetDeviceByName("RFSource") as SignalGeneratorInfo;
            RF.Device.IsRFOutOpen = true;
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("时间(ns)","Delay测试数据",ChartDataType.X),
            };
            UpdatePlotChart();
            Show1DChartData("对比度数据", "时间(ns)", "对比度");
            return;
        }

        public override void AfterExpEventWithoutAFM()
        {
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
    }
}
