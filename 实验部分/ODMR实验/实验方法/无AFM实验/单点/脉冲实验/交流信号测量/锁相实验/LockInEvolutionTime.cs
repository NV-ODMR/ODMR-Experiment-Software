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
    class LockInEvolutionTime : LockInExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        /// <summary>
        /// 所属锁相实验
        /// </summary>
        private LockInExpBase ParentLockInExp { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "锁相变演化时间测试";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "锁相信号触发板卡的序列进行输出,在此基础上进行的HahnEcho自旋回波实验,通过改变触发的延迟时间来得到对应的信号变化曲线.用到的序列文件名:LockInHahnEcho";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("起点时间(ns)",20,"StartTime"),
            new Param<int>("点数",20,"DelayCount"),
            new Param<double>("终点时间(ns)",1000,"EndTime"),
            new Param<double>("Delay时间(ns)",1000,"DelayTime"),
            new Param<int>("测量轮数",1,"LoopCount"){ Helper="每个时间点的重复测量次数" },
            new Param<MultiScanType>("测量循环类型",MultiScanType.正向扫描,"ScanType"){ Helper="重复测量每个时间点的方式" },
            new Param<int>("序列循环次数",1,"SeqLoopCount"){ Helper="扫描每个频点时板卡序列的内部循环次数" },
            new Param<double>("锁相信号频率(MHz)",1,"SignalFreq"){ Helper="输出锁相信号通道的频率,仅用于确定信号的积累时间,不参与仪器参数的设置" },
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"){ Helper="每个时间点扫描的时间上限,超时则跳过此点" },
            new Param<bool>("单次实验前打开信号",false,"OpenSignalBeforeExp") { Helper = "当选择此选项时,在进行此实验之前会使控制锁相信号的继电器打开,实验结束后则会关闭" },
            new Param<int>("序列阶数",1,"SequenceCount"){ Helper = "选择需要积累多少个周期的信号" },
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> LockInExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
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

        public override bool IsAFMSubExperiment { get; protected set; } = true;

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
            MultiScan1DSession<object> Session = new MultiScan1DSession<object>();
            Session.FirstScanEvent = ExpScanEvent;
            Session.ScanEvent = ExpScanEvent;
            Session.ScanSource = null;
            Session.PlotEvent = PlotEvent;
            Session.ProgressBarMethod = new Action<object, double>((obj, v) =>
            {
                SetProgress(v);
            });
            Session.SetStateMethod = new Action<object, int, double>((obj, loop, v) =>
            {
                SetExpState("当前扫描轮数:" + loop.ToString() + ",时间点: " + Math.Round(v, 5).ToString());
            });
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("StartTime"), GetInputParamValueByName("EndTime"), GetInputParamValueByName("DelayCount"));

            Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            Session.BeginScan(GetInputParamValueByName("LoopCount"), GetInputParamValueByName("ScanType"), range, 0, 100);
        }

        private void PlotEvent(List<MultiLoopScanData> list)
        {
            (Get1DChartData("时间(ns)", "对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "时间(ns)", "对比度数据");
            (Get1DChartData("信号光子数", "对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "信号光子数", "对比度数据");
            (Get1DChartData("参考光子数", "对比度数据") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "参考光子数", "对比度数据");
            var signal = MultiLoopScanData.GetSumData(list, "信号光子数", "对比度数据");
            var refe = MultiLoopScanData.GetSumData(list, "参考光子数", "对比度数据");
            var cont = signal.Zip(refe, new Func<double, double, double>((s, r) => { return (s - r) / r; }));
            (Get1DChartData("对比度", "对比度数据") as NumricChartData1D).Data = cont.ToList();

            (Get1DChartData("时间(ns)", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetAverageData(list, "时间(ns)", "对比度数据");
            (Get1DChartData("信号光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "信号光子数", "对比度数据");
            (Get1DChartData("参考光子数", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "参考光子数", "对比度数据");
            (Get1DChartData("对比度", "方差") as NumricChartData1D).Data = MultiLoopScanData.GetSigmaData(list, "对比度", "对比度数据");

            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        private void HahnEchoExp(out double contrast, out double Sig, out double Ref)
        {
            GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", (int)GetInputParamValueByName("DelayTime"));

            PulsePhotonPack pack = null;

            //设置HahnEchoTime
            //XPi脉冲时间
            int xLength = GlobalPulseParams.GetGlobalPulseLength("PiX");
            double signaltime = 1e+3 / GetInputParamValueByName("SignalFreq");

            int echotime = (int)((signaltime - xLength) / 2);

            int order = GetInputParamValueByName("SequenceCount");

            if (order != 1)
                echotime = (int)(signaltime / 2 - xLength);

            pack = DoLockInPulseExp("CMPG",GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4,
            GetInputParamValueByName("TimeOut"), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq); }));

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

        private void SetSequenceCount(SequenceDataAssemble obj)
        {
            //传入参数为读取到的序列
            //查找X:pi脉冲的通道
            SequenceChannel ind = SequenceChannel.None;
            foreach (var ch in obj.Channels)
            {
                var pilist = ch.Peaks.Where(x => x.PeakName == "PiX");
                if (pilist.Count() != 0)
                {
                    if (pilist.ElementAt(0).WaveValue == WaveValues.One)
                    {
                        ind = ch.ChannelInd;
                    }
                }
            }
            //为每个通道添加对应阶数的序列
            int order = GetInputParamValueByName("SequenceCount");
            if (order > 1)
            {
                int det = order - 1;
                int pix = GlobalPulseParams.GetGlobalPulseLength("PiX");
                int spintime = GlobalPulseParams.GetGlobalPulseLength("SpinEchoTime");
                foreach (var ch in obj.Channels)
                {
                    ///Pi/2 Y脉冲的位置
                    var halfpiys = ch.Peaks.Where((x) => x.PeakName == "HalfPiY").Select((x) => ch.Peaks.IndexOf(x)).ToList();
                    halfpiys.Sort();
                    halfpiys.Reverse();
                    int signalch = halfpiys.Last();
                    foreach (var item in halfpiys)
                    {
                        List<SequenceWaveSeg> segs = new List<SequenceWaveSeg>();
                        for (int i = 0; i < det; i++)
                        {
                            segs.Add(new SequenceWaveSeg("PiX", pix, (ch.ChannelInd == ind && item == signalch) ? WaveValues.One : WaveValues.Zero, ch));
                            segs.Add(new SequenceWaveSeg("SpinEchoTime", spintime, WaveValues.Zero, ch));
                            segs.Add(new SequenceWaveSeg("PiX", pix, (ch.ChannelInd == ind && item == signalch) ? WaveValues.One : WaveValues.Zero, ch));
                            segs.Add(new SequenceWaveSeg("SpinEchoTime", spintime, WaveValues.Zero, ch));
                        }
                        ch.Peaks.InsertRange(item, segs);
                    }
                }
            }

        }
        private List<object> ExpScanEvent(object arg1, D1NumricScanRangeBase arg2, double arg3, int currrentloop, List<Tuple<string, string, double, MultiLoopDataProcessBase>> outputparams, List<object> arg4)
        {
            GlobalPulseParams.SetGlobalPulseLength("SpinEchoTime", (int)arg3);
            JudgeThreadEndOrResumeAction();
            HahnEchoExp(out double contrast, out double sig, out double reference);

            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("时间(ns)", "对比度数据", arg3, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("信号光子数", "对比度数据", sig, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("参考光子数", "对比度数据", reference, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("对比度", "对比度数据", contrast, new StandardDataProcess()));

            return new List<object>();
        }

        public override void PreLockInExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("时间(ns)","对比度数据",ChartDataType.X),
                new NumricChartData1D("对比度","对比度数据",ChartDataType.Y),
                new NumricChartData1D("信号光子数","对比度数据",ChartDataType.Y),
                new NumricChartData1D("参考光子数","对比度数据",ChartDataType.Y),

                new NumricChartData1D("时间(ns)","方差",ChartDataType.X),
                new NumricChartData1D("对比度","方差",ChartDataType.Y),
                new NumricChartData1D("信号光子数","方差",ChartDataType.Y),
                new NumricChartData1D("参考光子数","方差",ChartDataType.Y)
            };
            UpdatePlotChart();
            Show1DChartData("Delay测试数据", "时间(ns)", "对比度数据");
            if (GetInputParamValueByName("OpenSignalBeforeExp") == true)
            {
                var dev = GetSignalSwitch() as SwitchInfo;
                dev.Device.IsOpen = true;
                Thread.Sleep(2000);
            }
            return;
        }

        public override void AfterLockInExpEventWithoutAFM()
        {
            if (GetInputParamValueByName("OpenSignalBeforeExp") == true)
            {
                var dev = GetSignalSwitch() as SwitchInfo;
                dev.Device.IsOpen = false;
            }
        }


        private double DelayFitFunc(double x, double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            double d = ps[3];
            return a * Math.Cos(2 * Math.PI / b * (x - c)) + d;
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>() {
                new ParentPlotDataPack("时间(ns)", "对比度数据", ChartDataType.X, Get1DChartDataSource("时间(ns)", "对比度数据"), false),
                new ParentPlotDataPack("对比度曲线", "对比度数据", ChartDataType.Y, Get1DChartDataSource("对比度", "对比度数据"), true),
                new ParentPlotDataPack("时间(ns)", "单点Delay曲线方差", ChartDataType.X, Get1DChartDataSource("时间(ns)", "方差"), false),
                new ParentPlotDataPack("对比度曲线方差", "单点Delay曲线方差", ChartDataType.Y, Get1DChartDataSource("对比度", "方差"), true)
            };
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override int GetMaxSeqLoopCount()
        {
            return GetInputParamValueByName("SeqLoopCount");
        }

        public override int GetMaxLaserCountPulses()
        {
            return 4;
        }
    }
}
