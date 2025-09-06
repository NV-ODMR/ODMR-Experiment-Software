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
            new Param<double>("锁相信号频率(MHz)",1,"SignalFreq"),
            new Param<int>("测量次数",1000,"LoopCount"),
            new Param<int>("序列循环次数",1000,"SeqLoopCount"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude"),
            new Param<int>("单点超时时间",10000,"TimeOut"),
            new Param<bool>("测量单点对比度",false,"SingleContrast"),
            new Param<int>("对比度Rabi测量循环次数",10000,"ContrastRabiLoopCount"),
            new Param<bool>("单次实验前打开信号",false,"OpenSignalBeforeExp"),
            new Param<bool>("每点重新确定微波频率",false,"ConfirmCW"),
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

        double contrast = double.NaN;
        double Sig = double.NaN;
        double Ref = double.NaN;
        double contrast2 = double.NaN;
        double Sig2 = double.NaN;
        double Ref2 = double.NaN;
        double LowContrast = double.NaN;
        double HiContrast = double.NaN;
        double NormalizedContrast = double.NaN;
        double NormalizedContrast1 = double.NaN;
        double NormalizedContrast2 = double.NaN;
        double sigma = double.NaN;
        double sigma2 = double.NaN;
        List<double> HistData = new List<double>();
        List<double> HistData2 = new List<double>();

        public override void ODMRExpWithoutAFM()
        {
            contrast = double.NaN;
            Sig = double.NaN;
            Ref = double.NaN;
            contrast2 = double.NaN;
            Sig2 = double.NaN;
            Ref2 = double.NaN;
            HiContrast = double.NaN;
            LowContrast = double.NaN;
            NormalizedContrast1 = double.NaN;
            NormalizedContrast2 = double.NaN;
            sigma = double.NaN;
            sigma2 = double.NaN;
            HistData.Clear();
            HistData2 = new List<double>();
            int Loop = GetInputParamValueByName("LoopCount");
            //设置HahnEchoTime
            //XPi脉冲时间
            int xLength = GlobalPulseParams.GetGlobalPulseLength("PiX");
            int delay = GlobalPulseParams.GetGlobalPulseLength("TriggerExpStartDelay");
            double signaltime = 1e+3 / GetInputParamValueByName("SignalFreq");
            int echotime = (int)((signaltime - xLength) / 2);
            GlobalPulseParams.SetGlobalPulseLength("SpinEchoTime", echotime);
            for (int i = 0; i < Loop; i++)
            {
                CurrentLoop = i;
                SetExpState("当前轮数:" + CurrentLoop.ToString() + "对比度:" + Math.Round(contrast, 5).ToString());
                #region 点1
                GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", delay);
                PulsePhotonPack pack = DoLockInPulseExp("LockInHahnEcho", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SignalFreq"), GetInputParamValueByName("SeqLoopCount"), 4,
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
                HistData.Add(contrast);
                //方差
                sigma = Math.Sqrt(HistData.Select(x => Math.Pow(x - HistData.Average(), 2)).Sum() / (CurrentLoop + 1));
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
                #endregion
                #region 点2
                GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", delay + (int)(1.0 / GetInputParamValueByName("SignalFreq") * 500));
                pack = DoLockInPulseExp("LockInHahnEcho", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("SignalFreq"), GetInputParamValueByName("SeqLoopCount"), 4,
                    GetInputParamValueByName("TimeOut"));
                sig = pack.GetPhotonsAtIndex(0).Sum();
                reference = pack.GetPhotonsAtIndex(1).Sum();

                tempcontrast = 1;
                try
                {
                    tempcontrast = (sig - reference) / (double)reference;
                }
                catch (Exception)
                {
                }

                if (double.IsNaN(contrast2))
                {
                    contrast2 = tempcontrast;
                }
                else
                {
                    contrast2 = (contrast2 * (CurrentLoop - 1) + tempcontrast) / CurrentLoop;
                }
                HistData2.Add(contrast2);
                //方差
                sigma2 = Math.Sqrt(HistData2.Select(x => Math.Pow(x - HistData2.Average(), 2)).Sum() / (CurrentLoop + 1));
                if (double.IsNaN(Sig2))
                {
                    Sig2 = sig;
                }
                else
                {
                    Sig2 = (Sig2 * (CurrentLoop - 1) + sig) / CurrentLoop;
                }
                if (double.IsNaN(Ref2))
                {
                    Ref2 = reference;
                }
                else
                {
                    Ref2 = (Ref2 * (CurrentLoop - 1) + reference) / CurrentLoop;
                }
                #endregion

                GlobalPulseParams.SetGlobalPulseLength("TriggerExpStartDelay", delay);

                JudgeThreadEndOrResumeAction?.Invoke();
                double rabicontrast = double.NaN;
                if (GetInputParamValueByName("SingleContrast"))
                {
                    //测量Rabi得到对比度
                    GlobalPulseParams.SetGlobalPulseLength("RabiTime", GlobalPulseParams.GetGlobalPulseLength("PiX"));
                    PulsePhotonPack rabipack = DoPulseExp("Rabi", double.IsNaN(cwpeak) ? GetInputParamValueByName("RFFrequency") : cwpeak, GetInputParamValueByName("RFAmplitude"), GetInputParamValueByName("ContrastRabiLoopCount"), 8, GetInputParamValueByName("TimeOut"));
                    double signalcount1 = rabipack.GetPhotonsAtIndex(0).Sum();
                    double refcount = rabipack.GetPhotonsAtIndex(1).Sum();
                    //double refcount = rabipack.GetPhotonsAtIndex(2).Sum();
                    LowContrast = (signalcount1 - refcount) / refcount;
                    //hicontrast = (signalcount0 - refcount) / refcount;

                    //if (double.IsNaN(HiContrast))
                    //{
                    //    HiContrast = hicontrast;
                    //}
                    //else
                    //{
                    //    HiContrast = (HiContrast * (CurrentLoop - 1) + hicontrast) / CurrentLoop;
                    //}
                    //if (double.IsNaN(LowContrast))
                    //{
                    //    LowContrast = lowcontrast;
                    //}
                    //else
                    //{
                    //    LowContrast = (LowContrast * (CurrentLoop - 1) + lowcontrast) / CurrentLoop;
                    //}
                    if (contrast < LowContrast)
                    {
                        NormalizedContrast1 = 0;
                        JudgeThreadEndOrResumeAction?.Invoke();
                    }
                    else
                    {
                        NormalizedContrast1 = (contrast - LowContrast) / Math.Abs(LowContrast);
                        JudgeThreadEndOrResumeAction?.Invoke();
                    }
                    if (contrast2 < LowContrast)
                    {
                        NormalizedContrast2 = 0;
                        JudgeThreadEndOrResumeAction?.Invoke();
                    }
                    else
                    {
                        NormalizedContrast2 = (contrast2 - LowContrast) / Math.Abs(LowContrast);
                        JudgeThreadEndOrResumeAction?.Invoke();
                    }
                    //if (contrast < HiContrast)
                    //{
                    //    NormalizedContrast = 1;
                    //    JudgeThreadEndOrResumeAction?.Invoke();
                    //    continue;
                    //}
                    //NormalizedContrast = (contrast - LowContrast) / (HiContrast - LowContrast);
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
            //设置输出
            OutputParams.Add(new Param<double>("信号光子计数", Sig, "SignalCount"));
            OutputParams.Add(new Param<double>("参考光子计数", Ref, "ReferenceCount"));
            OutputParams.Add(new Param<double>("对比度", contrast, "Contrast"));
            OutputParams.Add(new Param<double>("对比度标准差", sigma, "Error"));
            OutputParams.Add(new Param<double>("信号光子计数2", Sig2, "SignalCount2"));
            OutputParams.Add(new Param<double>("参考光子计数2", Ref2, "ReferenceCount2"));
            OutputParams.Add(new Param<double>("对比度2", contrast2, "Contrast2"));
            OutputParams.Add(new Param<double>("对比度标准差2", sigma2, "Error2"));
            OutputParams.Add(new Param<double>("对比度差值", contrast2 - contrast, "Det"));
            if (GetInputParamValueByName("SingleContrast"))
            {
                OutputParams.Add(new Param<double>("Rabi对比度", LowContrast, "RabiContrast"));
                OutputParams.Add(new Param<double>("归一化对比度1", NormalizedContrast1, "NomContrast1"));
                OutputParams.Add(new Param<double>("归一化对比度2", NormalizedContrast2, "NomContrast2"));
                OutputParams.Add(new Param<double>("归一化对比度差值", NormalizedContrast1 - NormalizedContrast2, "NomContrastDet"));
            }
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
