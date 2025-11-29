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
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class LockInSequenceTest : PulseExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = true;

        public override bool IsDisplayAsExp { get; set; } = true;

        /// <summary>
        /// 所属锁相实验
        /// </summary>
        private LockInExpBase ParentLockInExp { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "锁相序列长度测试";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "锁相信号触发板卡的序列进行输出,在此基础上进行的HahnEcho自旋回波实验,通过改变触发的延迟时间来得到对应的信号变化曲线.用到的序列文件名:LockInHahnEcho";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("自由演化时间(ns)",1000,"EvoTime"),
            new Param<bool>("Pi/2 X脉冲为定值",false,"IsPi2Fixed"),
            new Param<double>("定值时间",100,"FixedTime"),
            new Param<int>("序列循环次数",1,"SeqLoopCount"){ Helper="扫描每个频点时板卡序列的内部循环次数" },
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"){ Helper="每个时间点扫描的时间上限,超时则跳过此点" },
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
            CustomScan2DSession<object, object> PointsScanSession = new CustomScan2DSession<object, object>();
            PointsScanSession.ProgressBarMethod = new Action<object, object, double>((d1, d2, v) => { SetProgress(v); });
            PointsScanSession.FirstScanEvent = ExpScanEvent;
            PointsScanSession.ScanEvent = ExpScanEvent;
            PointsScanSession.StartScanNewLineEvent = ExpScanEvent;
            PointsScanSession.EndScanNewLineEvent = ExpScanEvent;
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            PointsScanSession.SetStateMethod = new Action<object, object, Point>((d1, d2, p) =>
            {
                SetExpState("当前X值:" + Math.Round(p.X, 5).ToString() + "  Y值:" + Math.Round(p.Y, 5).ToString());
            });
            PointsScanSession.BeginScan(D2ScanRange, 0, 100);
        }

        private void HahnEchoExp(out double contrast, out double Sig, out double Ref, bool IsChangePi, int changevalue)
        {
            if (IsChangePi)
            {
                GlobalPulseParams.SetGlobalPulseLength("CustomPiXLength", changevalue);

                GlobalPulseParams.SetGlobalPulseLength("CustomXLength", (int)GetInputParamValueByName("FixedTime"));
            }
            else
            {
                GlobalPulseParams.SetGlobalPulseLength("CustomXLength", changevalue);

                GlobalPulseParams.SetGlobalPulseLength("CustomPiXLength", (int)GetInputParamValueByName("FixedTime"));
            }

            GlobalPulseParams.SetGlobalPulseLength("T2Step", (int)GetInputParamValueByName("EvoTime"));

            PulsePhotonPack pack = null;

            pack = DoPulseExp("HahnEchoTest", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4,
            GetInputParamValueByName("TimeOut"));

            Sig = pack.GetPhotonsAtIndex(0).Sum();
            Ref = pack.GetPhotonsAtIndex(1).Sum();
            contrast = 1;
            try
            {
                contrast = (Sig - Ref) / (double)Ref;
            }
            catch (Exception)
            {
            }
            JudgeThreadEndOrResumeAction?.Invoke();
        }

        private List<object> ExpScanEvent(object arg1, object arg2, D2ScanRangeBase arg3, Point arg4, List<object> arg5)
        {
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", (int)arg4.Y);
            JudgeThreadEndOrResumeAction();
            HahnEchoExp(out double contrast, out double sig, out double reference, GetInputParamValueByName("IsPi2Fixed"), (int)arg4.X);

            int rowx = arg3.GetNearestXIndex(arg4.X);
            int rowy = arg3.GetNearestYIndex(arg4.Y);

            var c0 = Get2DChartData("对比度", "对比度数据").Data;
            var cs = Get2DChartData("信号光子数", "对比度数据").Data;
            var cr = Get2DChartData("参考光子数", "对比度数据").Data;
            c0.SetValue(rowx, rowy, contrast);
            cs.SetValue(rowx, rowy, sig);
            cr.SetValue(rowx, rowy, reference);
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;
            D1ChartDatas.Clear();
            D2ChartDatas.Clear();
            //新建数据集
            double xlo = D2ScanRange.XLo;
            double xhi = D2ScanRange.XHi;
            double ylo = D2ScanRange.YLo;
            double yhi = D2ScanRange.YHi;
            int xcount = D2ScanRange.XCount;
            int ycount = D2ScanRange.YCount;
            string xname = GetInputParamValueByName("IsPi2Fixed") ? "Pi/2脉冲长度" : "Pi脉冲长度";
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = xname, YName = "Y脉冲长度", ZName = "信号光子数" }) { GroupName = "对比度数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = xname, YName = "Y脉冲长度", ZName = "参考光子数" }) { GroupName = "对比度数据" });
            D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(xcount, xlo, xhi, ycount, ylo, yhi) { XName = xname, YName = "Y脉冲长度", ZName = "对比度" }) { GroupName = "对比度数据" });
            UpdatePlotChart();
            return;
        }

        public override void AfterExpEventWithoutAFM()
        {
        }


        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>() {
            };
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

    }
}
