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
using ODMR_Lab.基本窗口;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class LockInHahnEcho : LockInExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override string ODMRExperimentName { get; set; } = "锁相HahnEcho";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "锁相信号触发板卡的序列进行输出,在此基础上进行的HahnEcho自旋回波实验.用到的序列文件名:LockInHahnEcho";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("锁相信号频率(MHz)",1,"SignalFreq"){ Helper="输出锁相信号通道的频率,仅用于确定信号的积累时间,不参与仪器参数的设置"},
            new Param<int>("测量次数",1000,"LoopCount"){Helper="重复测量次数" },
            new Param<int>("序列循环次数",1000,"SeqLoopCount"){Helper="扫描每个点时板卡序列的内部循环次数" },
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"){ Helper="每个时间点扫描的时间上限,超时则跳过此点" },
            new Param<bool>("单次实验前打开信号",false,"OpenSignalBeforeExp"){ Helper = "当选择此选项时,在进行此实验之前会使控制锁相信号的继电器打开,实验结束后则会关闭" },
            new Param<bool>("每点重新确定微波频率",false,"ConfirmCW"){ Helper = "当选择此选项时,在进行HahnEcho实验之前会先扫描CW谱来确定共振频率,具体的扫描参数由子实验确定" },
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
                new AdjustedCW()
            };
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;

        public override bool PreConfirmProcedure()
        {
            return true;
        }

        private int CurrentLoop = 0;

        public override void ODMRExpWithoutAFM()
        {
            //设置HahnEchoTime
            //XPi脉冲时间
            int xLength = GlobalPulseParams.GetGlobalPulseLength("PiX");
            double signaltime = 1e+3 / GetInputParamValueByName("SignalFreq");
            int echotime = (int)((signaltime - xLength) / 2);

            MultiScan1DSession<object> Session = new MultiScan1DSession<object>();
            Session.FirstScanEvent = ScanEvent;
            Session.ScanEvent = ScanEvent;
            Session.ScanSource = null;
            Session.PlotEvent = PlotEvent;
            Session.ProgressBarMethod = new Action<object, double>((obj, v) =>
            {
                SetProgress(v);
            });
            Session.SetStateMethod = new Action<object, int, double>((obj, loop, v) =>
            {
                SetExpState("当前扫描轮数:" + loop.ToString());
            });
            D1NumricListScanRange range = new D1NumricListScanRange(new List<double>() { 1 });

            Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            Session.BeginScan(GetInputParamValueByName("LoopCount"), MultiScanType.正向扫描, range, 0, 100);
        }

        List<MultiLoopScanData> listdata = null;

        private void PlotEvent(List<MultiLoopScanData> list)
        {
            listdata = list;
        }

        private List<object> ScanEvent(object device, D1NumricScanRangeBase range, double locvalue, int currrentloop, List<Tuple<string, string, double, MultiLoopDataProcessBase>> outputparams, List<object> inputParams)
        {
            PulsePhotonPack pack = null;

            #region 点1(1/2pi Y)
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiY"));

            pack = DoLockInPulseExp("CMPGY", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4,
                GetInputParamValueByName("TimeOut"), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq); }));

            double sigY = pack.GetPhotonsAtIndex(0).Sum();
            double referenceY = pack.GetPhotonsAtIndex(1).Sum();

            if (sigY == 0) sigY = double.NaN;
            if (referenceY == 0) referenceY = double.NaN;

            double tempcontrastY = double.NaN;
            try
            {
                tempcontrastY = (sigY - referenceY) / (double)referenceY;
            }
            catch (Exception)
            {
            }
            #endregion
            JudgeThreadEndOrResumeAction?.Invoke();
            #region 点2(3/2pi Y)
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("3HalfPiY"));

            pack = DoLockInPulseExp("CMPGY", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4,
                GetInputParamValueByName("TimeOut"), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq); }));

            double sigY3 = pack.GetPhotonsAtIndex(0).Sum();
            double referenceY3 = pack.GetPhotonsAtIndex(1).Sum();

            if (sigY3 == 0) sigY3 = double.NaN;
            if (referenceY3 == 0) referenceY3 = double.NaN;

            double tempcontrastY3 = double.NaN;
            try
            {
                tempcontrastY3 = (sigY3 - referenceY3) / (double)referenceY3;
            }
            catch (Exception)
            {
            }
            #endregion
            JudgeThreadEndOrResumeAction?.Invoke();
            #region 点3(1/2pi X)
            GlobalPulseParams.SetGlobalPulseLength("CustomXLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiX"));

            pack = DoLockInPulseExp("CMPGX", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4,
                GetInputParamValueByName("TimeOut"), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq); }));

            double sigX = pack.GetPhotonsAtIndex(0).Sum();
            double referenceX = pack.GetPhotonsAtIndex(1).Sum();

            if (sigX == 0) sigX = double.NaN;
            if (referenceX == 0) referenceX = double.NaN;

            double tempcontrastX = double.NaN;
            try
            {
                tempcontrastX = (sigX - referenceX) / (double)referenceX;
            }
            catch (Exception)
            {
            }
            #endregion
            JudgeThreadEndOrResumeAction?.Invoke();
            #region 点2(3/2pi Y)
            GlobalPulseParams.SetGlobalPulseLength("CustomXLength", GlobalPulseParams.GetGlobalPulseLength("3HalfPiX"));

            pack = DoLockInPulseExp("CMPGX", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SeqLoopCount"), 4,
                GetInputParamValueByName("TimeOut"), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq); }));

            double sigX3 = pack.GetPhotonsAtIndex(0).Sum();
            double referenceX3 = pack.GetPhotonsAtIndex(1).Sum();

            if (sigX3 == 0) sigX3 = double.NaN;
            if (referenceX3 == 0) referenceX3 = double.NaN;

            double tempcontrastX3 = double.NaN;
            try
            {
                tempcontrastX3 = (sigX3 - referenceX3) / (double)referenceX3;
            }
            catch (Exception)
            {
            }
            #endregion
            JudgeThreadEndOrResumeAction?.Invoke();

            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("PI/2 Y对比度", "对比度数据", tempcontrastY, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("3PI/2 Y对比度", "对比度数据", tempcontrastY3, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("PI/2 X对比度", "对比度数据", tempcontrastX, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("3PI/2 X对比度", "对比度数据", tempcontrastX3, new StandardDataProcess()));

            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("PI/2 Y 平均光子数", "荧光数据", referenceY, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("PI/2 Y 信号光子数", "荧光数据", sigY, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("3PI/2 Y 平均光子数", "荧光数据", referenceY3, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("3PI/2 Y 信号光子数", "荧光数据", sigY3, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("PI/2 X 平均光子数", "荧光数据", referenceX, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("PI/2 X 信号光子数", "荧光数据", sigX, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("3PI/2 X 平均光子数", "荧光数据", referenceX3, new StandardDataProcess()));
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("3PI/2 X 信号光子数", "荧光数据", sigX3, new StandardDataProcess()));

            return new List<object>();
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
                    ///Pi/2 脉冲的位置
                    var halfpiys = ch.Peaks.Where((x) => x.PeakName == "CustomYLength" || x.PeakName == "CustomXLength").Select((x) => ch.Peaks.IndexOf(x)).ToList();
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

        double OriginSignalAmplitude { get; set; } = 0;
        double cwpeak = double.NaN;
        public override void PreLockInExpEventWithoutAFM()
        {
            //打开微波
            SignalGeneratorChannelInfo RF = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            RF.Device.IsOutOpen = true;

            cwpeak = double.NaN;
            //如果要重新确定CW谱
            if (GetInputParamValueByName("ConfirmCW") == true)
            {
                var exp = RunSubExperimentBlock(0, true);
                double peak = exp.GetOutPutParamValueByDescription("谱峰位置1");
                if (peak > 2500 && peak < 3100)
                {
                    var dev = GetRFSource();
                    (dev as SignalGeneratorChannelInfo).Device.Frequency = peak;
                    cwpeak = peak;
                }
                OutputParams.Add(new Param<double>("谱峰位置", peak, "CWPeakLoc"));
            }

            if (GetInputParamValueByName("OpenSignalBeforeExp") == true)
            {
                //修改信号源强度
                (GetSignalSwitch() as SwitchInfo).Device.IsOpen = true;
                Thread.Sleep(2000);
            }
        }

        public override void AfterLockInExpEventWithoutAFM()
        {
            double cY = MultiLoopScanData.GetAverageData(listdata, "PI/2 Y对比度", "对比度数据")[0];
            double cY3 = MultiLoopScanData.GetAverageData(listdata, "3PI/2 Y对比度", "对比度数据")[0];
            double cX = MultiLoopScanData.GetAverageData(listdata, "PI/2 X对比度", "对比度数据")[0];
            double cX3 = MultiLoopScanData.GetAverageData(listdata, "3PI/2 X对比度", "对比度数据")[0];
            //设置输出
            OutputParams.Add(new Param<double>("PI/2 Y对比度", cY, "ContrastY") { GroupName = "对比度数据" });
            OutputParams.Add(new Param<double>("3PI/2 Y对比度", cY3, "ContrastY3") { GroupName = "对比度数据" });
            OutputParams.Add(new Param<double>("PI/2 X对比度", cX, "ContrastX") { GroupName = "对比度数据" });
            OutputParams.Add(new Param<double>("3PI/2 X对比度", cX3, "ContrastX3") { GroupName = "对比度数据" });
            OutputParams.Add(new Param<double>("对比度差值", cY3 - cY, "DetCon") { GroupName = "对比度数据" });
            OutputParams.Add(new Param<double>("归一化对比度[(3pi/2Y-pi/2Y)/(pi/2X-3pi/2X)]", (cY3 - cY) / (cX - cX3), "NContrast") { GroupName = "对比度数据" });
            OutputParams.Add(new Param<double>("还原相位(度)", Math.Atan2(cY3 - cY, cX - cX3) * 180 / Math.PI, "Phase") { GroupName = "对比度数据" });

            OutputParams.Add(new Param<double>("PI/2 Y 信号光子计数", MultiLoopScanData.GetAverageData(listdata, "PI/2 Y 信号光子数", "荧光数据")[0], "SignalCountY") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("PI/2 Y 参考光子计数", MultiLoopScanData.GetAverageData(listdata, "PI/2 Y 平均光子数", "荧光数据")[0], "ReferenceCountY") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("3PI/2 Y 信号光子计数", MultiLoopScanData.GetAverageData(listdata, "3PI/2 Y 信号光子数", "荧光数据")[0], "SignalCountY3") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("3PI/2 Y 参考光子计数", MultiLoopScanData.GetAverageData(listdata, "3PI/2 Y 平均光子数", "荧光数据")[0], "ReferenceCountY3") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("PI/2 X 信号光子计数", MultiLoopScanData.GetAverageData(listdata, "PI/2 X 信号光子数", "荧光数据")[0], "SignalCountX") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("PI/2 X 参考光子计数", MultiLoopScanData.GetAverageData(listdata, "PI/2 X 平均光子数", "荧光数据")[0], "ReferenceCountX") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("3PI/2 X 信号光子计数", MultiLoopScanData.GetAverageData(listdata, "3PI/2 X 信号光子数", "荧光数据")[0], "SignalCountX3") { GroupName = "荧光数据" });
            OutputParams.Add(new Param<double>("3PI/2 X 参考光子计数", MultiLoopScanData.GetAverageData(listdata, "3PI/2 X 平均光子数", "荧光数据")[0], "ReferenceCountX3") { GroupName = "荧光数据" });

            if (GetInputParamValueByName("OpenSignalBeforeExp") == true)
            {
                //修改信号源强度
                (GetSignalSwitch() as SwitchInfo).Device.IsOpen = false;
            }
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            var res = new List<KeyValuePair<string, Action>>();
            res.Add(new KeyValuePair<string, Action>("触发信号参数设置", SetTrigger));
            return res;
        }

        private void SetTrigger()
        {
            var dic = new Dictionary<string, string>();
            dic.Add("幅度(V)", "");
            dic.Add("偏置(V)", "");
            dic.Add("频率(MHz)", "");
            MessageWindow win = null;
            App.Current.Dispatcher.Invoke(() =>
            {
                ParamInputWindow pwin = new ParamInputWindow("触发信号参数设置");
                pwin.Owner = Window.GetWindow(ParentPage);
                pwin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dic = pwin.ShowDialog(dic);
                win = new MessageWindow("参数设置", "正在设置参数...", MessageBoxButton.OK, false, false);
                win.Owner = Window.GetWindow(ParentPage);
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.Show();
            });
            try
            {
                if (dic.Count != 0)
                {
                    GetDevices();
                    //设置待锁相信号频率,幅度,偏置
                    SignalGeneratorChannelInfo signal = GetDeviceByName("SignalChannel") as SignalGeneratorChannelInfo;
                    SignalGeneratorChannelInfo trigger = GetDeviceByName("TriggerChannel") as SignalGeneratorChannelInfo;
                    signal.BeginUse();
                    trigger.BeginUse();

                    double signalFrequency = double.Parse(dic["频率(MHz)"]);
                    double signalAmplitude = double.Parse(dic["偏置(V)"]);
                    double signalOffset = double.Parse(dic["频率(MHz)"]);
                    signal.Device.Frequency = signalFrequency * 1e+6;
                    signal.Device.Amplitude = signalAmplitude;
                    signal.Device.Offset = signalOffset;
                    trigger.Device.Frequency = signalFrequency * 1e+6;
                    signal.EndUse();
                    trigger.EndUse();
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    win.Close();
                    MessageWindow.ShowTipWindow("设置失败\n" + ex.Message, Window.GetWindow(ParentPage));
                });
                return;
            }
            App.Current.Dispatcher.Invoke(() =>
            {
                win.Close();
            });
        }

        public override int GetMaxSeqLoopCount()
        {
            return Math.Max(GetInputParamValueByName("ContrastRabiLoopCount"), GetInputParamValueByName("SeqLoopCount"));
        }

        public override int GetMaxLaserCountPulses()
        {
            return Math.Max(4, 8);
        }
    }
}
