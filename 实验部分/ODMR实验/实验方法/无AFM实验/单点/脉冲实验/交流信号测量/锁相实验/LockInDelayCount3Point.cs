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
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.设备部分.其他设备;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验
{
    class LockInDelayCount3Point : LockInExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override bool IsDisplayAsExp { get; set; } = true;

        /// <summary>
        /// 所属锁相实验
        /// </summary>
        private LockInExpBase ParentLockInExp { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "三点锁相Delay光子数测量";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override string Description { get; set; } = "锁相信号触发板卡的序列进行输出,在此基础上对NV发出的光子进行采样,通过改变触发的延迟时间来得到对应的光子数变化曲线.用到的序列文件名:DelayCountTest";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("测量轮数",1,"LoopCount"){ Helper="每个时间点的重复测量次数" },
            new Param<int>("单序列采样次数",1,"SingleLoopSampleCount"),
            new Param<double>("点1Delay时间",1000,"Delay1"),
            new Param<double>("点2Delay时间",1500,"Delay2"),
            new Param<double>("点3Delay时间",2000,"Delay3"),
            new Param<MultiScanType>("测量循环类型",MultiScanType.正向扫描,"ScanType"){ Helper="重复测量每个时间点的方式" },
            new Param<int>("序列循环次数",100000,"SeqLoopCount"){ Helper="扫描每个频点时板卡序列的内部循环次数" },
            new Param<int>("光子数采样时间(ns)",50,"CountSampleTime"),
            new Param<int>("单点超时时间",10000,"TimeOut"){ Helper="每个时间点扫描的时间上限,超时则跳过此点" },
            new Param<bool>("实验时打开信号",false,"OpenSignal"){ Helper="" },
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
            if (GetInputParamValueByName("OpenSignal"))
            {
                (GetSignalSwitch() as SwitchInfo).Device.IsOpen = true;
            }

            Statics.Clear();

            GlobalPulseParams.SetGlobalPulseLength("DelayCountSampleTime", GetInputParamValueByName("CountSampleTime"));

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
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("Delay1"), GetInputParamValueByName("Delay3"), 3);

            Session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            Session.LoopEndEvent = new Action<int>((ind) => { LoopEndMethod?.Invoke(); });
            Session.BeginScan(GetInputParamValueByName("LoopCount"), GetInputParamValueByName("ScanType"), range, 0, 100);
        }

        double light1 = double.NaN;

        double light2 = double.NaN;

        double light3 = double.NaN;

        private void PlotEvent(List<MultiLoopScanData> list)
        {
            try
            {
                light1 = MultiLoopScanData.GetAverageData(list, "Delay:" + "光子数", "Delay测试数据")[0];
            }
            catch (Exception)
            {
            }
            try
            {
                light2 = MultiLoopScanData.GetAverageData(list, "Delay:" + "光子数", "Delay测试数据")[1];
            }
            catch (Exception)
            {
            }
            try
            {
                light3 = MultiLoopScanData.GetAverageData(list, "Delay:" + "光子数", "Delay测试数据")[2];
            }
            catch (Exception)
            {
            }
        }

        List<KeyValuePair<double, List<KeyValuePair<int, int>>>> Statics = new List<KeyValuePair<double, List<KeyValuePair<int, int>>>>();

        private void CountExp(double arg, out double Count, out double statisticCount)
        {
            int counts = GetInputParamValueByName("SingleLoopSampleCount");
            Count = 0;
            PulsePhotonPack pack = DoLockInPulseExp("DelayCountTest", 2870, -20, GetInputParamValueByName("SeqLoopCount"), 2 * counts,
             GetInputParamValueByName("TimeOut"), new Action<SequenceDataAssemble>((seq) => { SetCount(seq); }));
            //var packstatic = Enumerable.Range(0, counts).Select(x => pack.GetPhotonStatic(x, 10000)).ToList();
            try
            {
                var photons = Enumerable.Range(0, counts).Select(x => pack.GetPhotonsAtIndex(x).Sum()).ToList();
                Count = photons.Where(x => x != 0).Sum() / photons.Where(x => x != 0).Count();
            }
            catch (Exception)
            {
                Count = double.NaN;
            }
            statisticCount = 0;
            pack = null;
            GC.Collect();
            JudgeThreadEndOrResumeAction?.Invoke();
            if (Count == 0) Count = double.NaN;
        }

        private void SetCount(SequenceDataAssemble assem)
        {
            int samplecounts = GetInputParamValueByName("SingleLoopSampleCount");
            int gruptime = 0;
            foreach (var item in assem.Channels)
            {
                for (int i = 0; i < samplecounts - 1; i++)
                {
                    var peak = item.Peaks.Where(x => x.PeakName == "锁相脉冲读取序列").ElementAt(0);
                    gruptime = peak.PeakSpan;
                    item.Peaks.Add(peak.Copy());
                }
            }
            GlobalPulseParams.SetGlobalPulseLength("LockInSequenceDuty", 2000 - (gruptime - GlobalPulseParams.GetGlobalPulseLength("LockInSequenceDuty")));
            return;
        }
        private List<object> ExpScanEvent(object arg1, D1NumricScanRangeBase arg2, double arg3, int currrentloop, List<Tuple<string, string, double, MultiLoopDataProcessBase>> outputparams, List<object> arg4)
        {
            GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", (int)arg3);
            JudgeThreadEndOrResumeAction();
            CountExp(arg3, out double count, out double statisticCount);
            outputparams.Add(new Tuple<string, string, double, MultiLoopDataProcessBase>("Delay:" + "光子数", "Delay测试数据", count, new StandardDataProcess()));
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
            };
            UpdatePlotChart();
            return;
        }

        public override void AfterLockInExpEventWithoutAFM()
        {
            if (GetInputParamValueByName("OpenSignal"))
            {
                (GetSignalSwitch() as SwitchInfo).Device.IsOpen = false;
            }

            OutputParams.Add(new Param<double>("Delay:" + GetInputParamValueByName("Delay1").ToString() + "光子数", light1, "Delay1"));
            OutputParams.Add(new Param<double>("Delay:" + GetInputParamValueByName("Delay2").ToString() + "光子数", light2, "Delay2"));
            OutputParams.Add(new Param<double>("Delay:" + GetInputParamValueByName("Delay3").ToString() + "光子数", light3, "Delay3"));

        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>() { };
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
            return 2;
        }
    }
}
