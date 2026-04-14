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

using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Window = System.Windows.Window;
using Controls.Charts;
using System.Windows.Media;
using System.Threading;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;
using HardWares.电源;
using ODMR_Lab.设备部分.其他设备;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class PulsePhaseTestXY8 : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        /// <summary>
        /// 所属锁相实验
        /// </summary>
        private LockInExpBase ParentLockInExp { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "变电压脉冲相位测试实验(XY8)";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "通过改变电控相移器的电压进行扫描,根据pi/2x-pi/2y序列的结果来获取脉冲相位和电压的关系";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("序列循环次数",500000,"SeqLoopCount"){ Helper="扫描每个频点时板卡序列的内部循环次数" },
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<SequenceTypes>("序列类型",SequenceTypes.CMPG,"SequenceType"),
            new Param<int>("序列阶数",1,"Order"),
            new Param<double>("电压起始点(V)",0,"VStart"),
            new Param<double>("电压中止点(V)",5,"VEnd"),
            new Param<int>("电压点数",5,"VCount"),
            new Param<int>("单点超时时间",10000,"TimeOut"){ Helper="每个时间点扫描的时间上限,超时则跳过此点" },
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.电源,new Param<string>("相移器电源","","Power")),
        };

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

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override void ODMRExpWithoutAFM()
        {
            Scan1DSession<object> PointsScanSession = new Scan1DSession<object>();
            PointsScanSession.ProgressBarMethod = new Action<object, double>((d1, v) => { SetProgress(v); });
            PointsScanSession.FirstScanEvent = ScanEvent;
            PointsScanSession.ScanEvent = ScanEvent;
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            PointsScanSession.SetStateMethod = new Action<object, double>((d1, p) =>
            {
                SetExpState("当前电压值:" + Math.Round(p, 5).ToString());
            });
            PointsScanSession.BeginScan(new D1NumricLinearScanRange(GetInputParamValueByName("VStart"), GetInputParamValueByName("VEnd"), GetInputParamValueByName("VCount")), 0, 100);
        }

        private List<object> ScanEvent(object arg1, D1NumricScanRangeBase arg2, double arg3, List<object> arg4)
        {
            //设置电压
            var channel = GetDeviceByName("Power") as PowerChannelInfo;
            channel.Device.Voltage = arg3;

            GlobalPulseParams.SetGlobalPulseLength("CustomXLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiX"));
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiY"));
            JudgeThreadEndOrResumeAction();
            HahnEchoExp(out double contrast1, out double p1, out double contrast2, out double p2);

            var volt = Get1DChartDataSource("电压值(V)", "对比度数据");
            var p1s = Get1DChartDataSource("PiX/2 布居度", "对比度数据");
            var c1s = Get1DChartDataSource("PiX/2 对比度", "对比度数据");
            var p2s = Get1DChartDataSource("3PiX/2 布居度", "对比度数据");
            var c2s = Get1DChartDataSource("3PiX/2 对比度", "对比度数据");

            volt.Add(arg3);
            c1s.Add(contrast1);
            c2s.Add(contrast2);
            p1s.Add(p1);
            p2s.Add(p2);

            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        private void HahnEchoExp(out double contrast1, out double contrast2, out double p1, out double p2)
        {
            string sequencename = "";
            if (GetInputParamValueByName("SequenceType") == SequenceTypes.CMPG)
            {
                sequencename = "CMPGX";
            }
            if (GetInputParamValueByName("SequenceType") == SequenceTypes.XY4)
            {
                sequencename = "XY-4-halfpiX";
            }
            if (GetInputParamValueByName("SequenceType") == SequenceTypes.XY8)
            {
                sequencename = "XY-8-halfpiX-NoDelay";
            }
            if (GetInputParamValueByName("SequenceType") == SequenceTypes.XY8_2)
            {
                sequencename = "XY-8-2-halfpiX";
            }

            PulsePhotonPack pack = null;

            #region Pi/2 X
            GlobalPulseParams.SetGlobalPulseLength("CustomXLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiX"));

            pack = DoPulseExp(sequencename, GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 6,
            GetInputParamValueByName("TimeOut"), new Action<SequenceDataAssemble>((seq) => { ExperimentHelper.SetSequenceCount(seq, GetInputParamValueByName("SequenceType"), GetInputParamValueByName("Order")); }));

            double Sig1 = pack.GetPhotonsAtIndex(0).Sum();
            double Ref1 = pack.GetPhotonsAtIndex(1).Sum();
            double Dark1 = pack.GetPhotonsAtIndex(2).Sum();
            contrast1 = double.NaN;
            p1 = double.NaN;
            try
            {
                contrast1 = (Sig1 - Ref1) / (double)Ref1;
                p1 = (Sig1 - Dark1) / (Ref1 - Dark1);
                if (double.IsInfinity(contrast1)) contrast1 = double.NaN;
                if (double.IsInfinity(p1)) p1 = double.NaN;
            }
            catch (Exception)
            {
            }
            #endregion
            JudgeThreadEndOrResumeAction?.Invoke();

            #region 3Pi/2 X
            GlobalPulseParams.SetGlobalPulseLength("CustomXLength", GlobalPulseParams.GetGlobalPulseLength("3HalfPiX"));

            pack = DoPulseExp(sequencename, GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 6,
            GetInputParamValueByName("TimeOut"), new Action<SequenceDataAssemble>((seq) => { ExperimentHelper.SetSequenceCount(seq, GetInputParamValueByName("SequenceType"), GetInputParamValueByName("Order")); }));

            double Sig2 = pack.GetPhotonsAtIndex(0).Sum();
            double Ref2 = pack.GetPhotonsAtIndex(1).Sum();
            double Dark2 = pack.GetPhotonsAtIndex(2).Sum();
            contrast2 = double.NaN;
            p2 = double.NaN;
            try
            {
                contrast2 = (Sig2 - Ref2) / (double)Ref2;
                p2 = (Sig2 - Dark2) / (Ref2 - Dark2);
                if (double.IsInfinity(contrast2)) contrast2 = double.NaN;
                if (double.IsInfinity(p2)) p2 = double.NaN;
            }
            catch (Exception)
            {
            }
            #endregion
            JudgeThreadEndOrResumeAction?.Invoke();
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;
            D1ChartDatas.Clear();
            D2ChartDatas.Clear();
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("电压值(V)", "对比度数据",ChartDataType.X),
                new NumricChartData1D("PiX/2 布居度", "对比度数据",ChartDataType.Y),
                new NumricChartData1D("PiX/2 对比度", "对比度数据",ChartDataType.Y),
                new NumricChartData1D("3PiX/2 布居度", "对比度数据",ChartDataType.Y),
                new NumricChartData1D("3PiX/2 对比度", "对比度数据",ChartDataType.Y),
            };
            UpdatePlotChart();
            return;
        }

        public override void AfterExpEventWithoutAFM()
        {
        }


        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>()
            {
            };
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

    }
}
