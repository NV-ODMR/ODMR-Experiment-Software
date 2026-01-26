using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;
using ODMR_Lab.设备部分.电源;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲CW谱扫描
{
    public abstract class PulseCWBase : PulseExpBase
    {
        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
            };
        }

        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.电源,new Param<string>("相移器电源","","Power")),
        };

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private List<object> FirstScanEvent(SignalGeneratorInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            return ScanEvent(device, range, locvalue, inputParams);
        }

        protected abstract int GetEvoTime();

        protected abstract double GetV90();

        protected abstract double GetV270();

        private List<object> ScanEvent(SignalGeneratorInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {

            SetExpState("当前扫描轮数:" + loopcount.ToString() + " 当前频率:" + locvalue.ToString());
            GlobalPulseParams.SetGlobalPulseLength("T2Step", GetEvoTime());
            GlobalPulseParams.SetGlobalPulseLength("CustomXLength", 30);

            var channel = GetDeviceByName("Power") as PowerChannelInfo;

            #region 一阶序列
            channel.Channel.Voltage = GetV90();
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiY"));
            PulsePhotonPack pack = DoPulseExp("PulseCW", locvalue, GetRFPower(), GetPulseLoopCount(), 4, GetPointTimeout(), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq, 1); }));
            double signalc = pack.GetPhotonsAtIndex(0).Sum();
            double referencec = pack.GetPhotonsAtIndex(1).Sum();
            double contrast1 = 0;
            try
            {
                contrast1 = (signalc - referencec) / referencec;
            }
            catch (Exception)
            {
            }

            //channel.Channel.Voltage = GetV270();
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("3HalfPiY"));
            pack = DoPulseExp("PulseCW", locvalue, GetRFPower(), GetPulseLoopCount(), 4, GetPointTimeout(),sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq, 1); }));
            signalc = pack.GetPhotonsAtIndex(0).Sum();
            referencec = pack.GetPhotonsAtIndex(1).Sum();
            double contrast2 = 0;
            try
            {
                contrast2 = (signalc - referencec) / referencec;
            }
            catch (Exception)
            {
            }
            #endregion

            #region 二阶序列
            channel.Channel.Voltage = GetV90();
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("HalfPiY"));
            pack = DoPulseExp("PulseCW", locvalue, GetRFPower(), GetPulseLoopCount(), 4, GetPointTimeout(), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq, 2); }));
            signalc = pack.GetPhotonsAtIndex(0).Sum();
            referencec = pack.GetPhotonsAtIndex(1).Sum();
            double contrast21 = 0;
            try
            {
                contrast21 = (signalc - referencec) / referencec;
            }
            catch (Exception)
            {
            }

            //channel.Channel.Voltage = GetV270();
            GlobalPulseParams.SetGlobalPulseLength("CustomYLength", GlobalPulseParams.GetGlobalPulseLength("3HalfPiY"));
            pack = DoPulseExp("PulseCW", locvalue, GetRFPower(), GetPulseLoopCount(), 4, GetPointTimeout(), sequenceAction: new Action<SequenceDataAssemble>((seq) => { SetSequenceCount(seq, 2); }));
            signalc = pack.GetPhotonsAtIndex(0).Sum();
            referencec = pack.GetPhotonsAtIndex(1).Sum();
            double contrast22 = 0;
            try
            {
                contrast22 = (signalc - referencec) / referencec;
            }
            catch (Exception)
            {
            }
            #endregion

            int ind = range.GetNearestFormalIndex(locvalue);
            var contrfreq = Get1DChartDataSource("频率", "CW对比度数据");

            var signal = Get1DChartDataSource("对比度差值 一阶", "CW对比度数据");
            var signal1 = Get1DChartDataSource("对比度Pi/2 一阶", "CW对比度数据");
            var signal2 = Get1DChartDataSource("对比度3Pi/2 一阶", "CW对比度数据");

            var signal2det = Get1DChartDataSource("对比度差值 二阶", "CW对比度数据");
            var signal21 = Get1DChartDataSource("对比度Pi/2 二阶", "CW对比度数据");
            var signal22 = Get1DChartDataSource("对比度3Pi/2 二阶", "CW对比度数据");

            var florfreq = Get1DChartDataSource("频率", "CW荧光计数");
            var count = Get1DChartDataSource("信号总计数", "CW荧光计数");
            var sigcount = Get1DChartDataSource("参考信号总计数", "CW荧光计数");

            if (ind >= contrfreq.Count)
            {
                contrfreq.Add(locvalue);
                florfreq.Add(locvalue);

                signal1.Add(contrast1);
                signal2.Add(contrast2);
                signal.Add(contrast1 - contrast2);

                signal21.Add(contrast21);
                signal22.Add(contrast22);
                signal2det.Add(contrast21 - contrast22);

                count.Add(signalc);
                sigcount.Add(referencec);
            }
            else
            {
                signal[ind] = (signal[ind] * loopcount + contrast1 - contrast2) / (loopcount + 1);
                signal1[ind] = (signal1[ind] * loopcount + contrast1) / (loopcount + 1);
                signal2[ind] = (signal2[ind] * loopcount + contrast2) / (loopcount + 1);

                signal2det[ind] = (signal2det[ind] * loopcount + contrast21 - contrast22) / (loopcount + 1);
                signal21[ind] = (signal21[ind] * loopcount + contrast21) / (loopcount + 1);
                signal22[ind] = (signal22[ind] * loopcount + contrast22) / (loopcount + 1);

                count[ind] = (count[ind] * loopcount + signalc) / (loopcount + 1);
                sigcount[ind] = (sigcount[ind] * loopcount + referencec) / (loopcount + 1);
            }

            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        private void SetSequenceCount(SequenceDataAssemble obj, int order)
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

            #region 为每个通道添加对应阶数的序列
            if (order > 1)
            {
                int det = order - 1;
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
                            segs.Add(new SequenceWaveSeg("PiX", GlobalPulseParams.GetGlobalPulseLength("PiX"), (ch.ChannelInd == ind && item == signalch) ? WaveValues.One : WaveValues.Zero, ch));
                            segs.Add(new SequenceWaveSeg("T2Step", GlobalPulseParams.GetGlobalPulseLength("T2Step"), WaveValues.Zero, ch));
                            segs.Add(new SequenceWaveSeg("PiX", GlobalPulseParams.GetGlobalPulseLength("PiX"), (ch.ChannelInd == ind && item == signalch) ? WaveValues.One : WaveValues.Zero, ch));
                            segs.Add(new SequenceWaveSeg("T2Step", GlobalPulseParams.GetGlobalPulseLength("T2Step"), WaveValues.Zero, ch));
                        }
                        ch.Peaks.InsertRange(item, segs);
                    }
                }
            }
            #endregion

        }
        public abstract List<double> GetScanFrequences();

        protected override void InnerRead(FileObject fobj)
        {
        }

        protected override void InnerWrite(FileObject obj)
        {
        }

        public override void PreExpEventWithoutAFM()
        {
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("频率","CW对比度数据",ChartDataType.X),

                new NumricChartData1D("对比度差值 一阶","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("对比度Pi/2 一阶","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("对比度3Pi/2 一阶","CW对比度数据",ChartDataType.Y),

                new NumricChartData1D("对比度差值 二阶","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("对比度Pi/2 二阶","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("对比度3Pi/2 二阶","CW对比度数据",ChartDataType.Y),

                new NumricChartData1D("频率","CW荧光计数",ChartDataType.X),
                new NumricChartData1D("信号总计数","CW荧光计数",ChartDataType.Y),
                new NumricChartData1D("参考信号总计数","CW荧光计数",ChartDataType.Y),
            };
            UpdatePlotChart();
            Show1DChartData("CW对比度数据", "频率", "对比度");
        }

        public override void AfterExpEventWithoutAFM()
        {
            SetOutput();
        }

        /// <summary>
        /// 获取循环次数
        /// </summary>
        /// <returns></returns>
        protected abstract int GetLoopCount();

        /// <summary>
        /// 获取循环次数
        /// </summary>
        /// <returns></returns>
        protected abstract int GetPulseLoopCount();

        /// <summary>
        /// 获取单点超时时间
        /// </summary>
        /// <returns></returns>
        protected abstract int GetPointTimeout();

        /// <summary>
        /// 获取微波功率
        /// </summary>
        /// <returns></returns>
        protected abstract double GetRFPower();

        /// <summary>
        /// 扫描完成后设置输出结果
        /// </summary>
        protected abstract void SetOutput();


        private int loopcount = 0;
        public override void ODMRExpWithoutAFM()
        {
            SignalGeneratorChannelInfo inf = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            inf.Device.IsOutOpen = true;
            for (int i = 0; i < GetLoopCount(); i++)
            {
                loopcount = i;
                Scan1DSession<SignalGeneratorInfo> session = new Scan1DSession<SignalGeneratorInfo>();
                session.FirstScanEvent = FirstScanEvent;
                session.ScanEvent = ScanEvent;
                var dev = GetDeviceByName("RFSource") as SignalGeneratorInfo;
                session.ScanSource = dev;
                session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
                session.ProgressBarMethod = new Action<SignalGeneratorInfo, double>((sour, v) =>
                {
                    SetProgress(v);
                });
                session.SetStateMethod = new Action<SignalGeneratorInfo, double>((sour, v) =>
                {
                    SetExpState("CW谱扫描,当前频率:" + Math.Round(v, 5).ToString());
                });
                session.BeginScan(new D1NumricListScanRange(GetScanFrequences()), 0, 100);
            }
        }

        protected List<double> GetFrequences()
        {
            return (Get1DChartData("频率", "CW对比度数据") as NumricChartData1D).Data;
        }

        protected List<double> GetContracts()
        {
            return (Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D).Data; ;
        }

        protected List<double> GetReferenceCounts()
        {
            return (Get1DChartData("参考信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
        }

        protected List<double> GetSignalCounts()
        {
            return (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
        }
    }
}
