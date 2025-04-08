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
    class LockInHahnEcho : LockInExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        public override string ODMRExperimentName { get; set; } = "锁相HahnEcho";

        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("待测信号频率(MHz)",1000,"SignalFreq"),
            new Param<int>("测量次数",1000,"LoopCount"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"),
            new Param<bool>("测量单点对比度",false,"SingleContrast"),
            new Param<int>("对比度Rabi测量循环次数",10000,"ContrastRabiLoopCount"),
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
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();

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
        double LowContrast = double.NaN;
        double HiContrast = double.NaN;

        public override void ODMRExpWithoutAFM()
        {
            contrast = double.NaN;
            Sig = double.NaN;
            Ref = double.NaN;
            LowContrast = double.NaN;
            HiContrast = double.NaN;
            int Loop = GetInputParamValueByName("LoopCount");
            //设置HahnEchoTime
            //XPi脉冲时间
            int xLength = GlobalPulseParams.GetGlobalPulseLength("PiX");
            double signaltime = 1e+3 / GetInputParamValueByName("SignalFreq");
            int echotime = (int)((signaltime - xLength) / 2);
            GlobalPulseParams.SetGlobalPulseLength("SpinEchoTime", echotime);
            for (int i = 0; i < Loop; i++)
            {
                CurrentLoop = i;
                SetExpState("当前轮数:" + CurrentLoop.ToString() + "对比度:" + Math.Round(contrast, 5).ToString());
                double lowcontrast = double.NaN;
                double hicontrast = double.NaN;
                if (GetInputParamValueByName("SingleContrast"))
                {
                    //测量Rabi得到对比度
                    GlobalPulseParams.SetGlobalPulseLength("RabiTime", GlobalPulseParams.GetGlobalPulseLength("PiX"));
                    PulsePhotonPack rabipack = DoPulseExp("Rabi", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("ContrastRabiLoopCount"), 8, GetInputParamValueByName("TimeOut"));
                    GlobalPulseParams.SetGlobalPulseLength("RabiTime", GlobalPulseParams.GetGlobalPulseLength("2PiX"));
                    PulsePhotonPack rabipack2 = DoPulseExp("Rabi", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("ContrastRabiLoopCount"), 8, GetInputParamValueByName("TimeOut"));
                    double signalcountX = rabipack.GetPhotonsAtIndex(0).Sum();
                    double refcountX = rabipack.GetPhotonsAtIndex(1).Sum();
                    hicontrast = (signalcountX - refcountX) / refcountX;
                    signalcountX = rabipack2.GetPhotonsAtIndex(0).Sum();
                    refcountX = rabipack2.GetPhotonsAtIndex(1).Sum();
                    lowcontrast = (signalcountX - refcountX) / refcountX;

                    if (double.IsNaN(LowContrast))
                    {
                        LowContrast = lowcontrast;
                    }
                    else
                    {
                        LowContrast = (LowContrast * (CurrentLoop - 1) + lowcontrast) / CurrentLoop;
                    }
                    if (double.IsNaN(HiContrast))
                    {
                        HiContrast = hicontrast;
                    }
                    else
                    {
                        HiContrast = (HiContrast * (CurrentLoop - 1) + hicontrast) / CurrentLoop;
                    }
                }
                PulsePhotonPack pack = DoLockInPulseExp("LockInHahnEcho", GetInputParamValueByName("RFFrequency"), GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SignalFreq"), GetInputParamValueByName("SeqLoopCount"), 4,
                    GetInputParamValueByName("TimeOut"));
                int sig = pack.GetPhotonsAtIndex(0).Sum();
                int reference = pack.GetPhotonsAtIndex(1).Sum();

                double tempcontrast = 1;
                try
                {
                    tempcontrast = (sig - reference) / (double)reference;
                }
                catch (Exception)
                {
                }

                if (double.IsNaN(contrast))
                {
                    contrast = tempcontrast;
                }
                else
                {
                    contrast = (contrast * (CurrentLoop - 1) + tempcontrast) / CurrentLoop;
                }
                if (double.IsNaN(Sig))
                {
                    Sig = sig;
                }
                else
                {
                    Sig = (Sig * (CurrentLoop - 1) + sig) / CurrentLoop;
                }
                if (double.IsNaN(Ref))
                {
                    Ref = reference;
                }
                else
                {
                    Ref = (Ref * (CurrentLoop - 1) + reference) / CurrentLoop;
                }
                JudgeThreadEndOrResumeAction?.Invoke();
            }
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开微波
            RFSourceInfo RF = GetDeviceByName("RFSource") as RFSourceInfo;
            RF.Device.IsRFOutOpen = true;
        }

        public override void AfterExpEventWithoutAFM()
        {
            //设置输出
            OutputParams.Add(new Param<double>("信号光子计数", Sig, "SignalCount"));
            OutputParams.Add(new Param<double>("参考光子计数", Ref, "ReferenceCount"));
            OutputParams.Add(new Param<double>("对比度", contrast, "Contrast"));
            if (GetInputParamValueByName("SingleContrast"))
            {
                OutputParams.Add(new Param<double>("参考对比度最小值", LowContrast, "RefLowContrast"));
                OutputParams.Add(new Param<double>("参考对比度最大值", HiContrast, "RefHiContrast"));
            }
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
