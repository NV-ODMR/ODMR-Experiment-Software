using Controls.Windows;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.板卡;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.ODMR实验;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验
{
    public abstract class LockInExpBase : ODMRExperimentWithoutAFM
    {
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            /// 设备:板卡，光子计数器,微波源
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("板卡","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.信号发生器通道,new Param<string>("射频信号通道","","RFSource")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.信号发生器通道,new Param<string>("信号源通道","","SignalChannel")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.信号发生器通道,new Param<string>("触发信号通道","","TriggerChannel")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.开关,new Param<string>("信号源开关","","SignalSwitch")),
        };
        /// <summary>
        /// 脉冲实验的输入参数
        /// </summary>
        public abstract List<KeyValuePair<DeviceTypes, Param<string>>> LockInExpDevices { get; set; }


        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            var btns = new List<KeyValuePair<string, Action>>()
            {
                new KeyValuePair<string, Action>("设置全局脉冲参数",SetGlobalParams),
                new KeyValuePair<string, Action>("Delay时长测试",DelayTest)
            };
            btns.AddRange(AddPulseInteractiveButtons());
            return btns;
        }

        protected abstract List<KeyValuePair<string, Action>> AddPulseInteractiveButtons();

        /// <summary>
        /// 获取脉冲实验的光子计数,返回相邻两个计数脉冲之间的计数,失败则报错
        /// </summary>
        /// <param name="rffrequency">微波频率</param>
        /// <param name="rfpower">微波功率(dbm)</param>
        /// <param name="LaserCountPulses">APD触发脉冲数,必须是偶数</param>
        /// <param name="signalFrequency">待测信号频率(MHz)</param>
        /// <param name="signalFrequency">待测信号幅度(V)</param>
        /// <param name="signalFrequency">待测信号偏置(V)</param>
        /// <returns></returns>
        protected PulsePhotonPack DoLockInPulseExp(string sequencename, double rffrequency, double rfpower, double signalFrequency, int sequenceLoopCount, int LaserCount, int timeout)
        {
            //设置微波
            SignalGeneratorChannelInfo channel = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            channel.Device.Frequency = rffrequency;
            channel.Device.Amplitude = rfpower;

            //设置序列
            GlobalPulseParams.SetGlobalPulseLength("TriggerWait", 0);
            var sequence = SequenceDataAssemble.ReadFromSequenceName(sequencename);
            //设置全局参数
            foreach (var item in GlobalPulseParams.GlobalPulseConfigs)
            {
                sequence.ChangeWaveSegSpan(item.PulseName, item.PulseLength);
            }

            ////获取相邻两次Trigger之间的实验时间
            //var triggers = sequence.Channels[0].Peaks.FindAll(x => x.IsTriggerCommand).Select(x => sequence.Channels[0].Peaks.IndexOf(x)).ToList();
            //if (triggers.Count != 0)
            //{
            //    triggers.Add(sequence.Channels[0].Peaks.Count - 1);
            //    for (int i = 0; i < triggers.Count - 1; i++)
            //    {
            //        int time = sequence.Channels[0].GetExpSegTime(triggers[i], triggers[i + 1]);
            //        if (time == 0) continue;
            //        SequenceWaveSeg wave = sequence.Channels[0].Peaks[triggers[i + 1]];
            //        if (i + 1 == triggers.Count - 1)
            //        {
            //            //如果是序列末尾则设置序列开头的等待时间(循环)
            //            wave = sequence.Channels[0].Peaks[triggers[0]];
            //        }
            //        sequence.Channels[0].GetSegTime(wave, out int triggerstart, out int triggerend);
            //        //根据总实验时间计算等待时间
            //        int periodTime = (int)(1e+3 / signalFrequency);
            //        int timeres = time % periodTime;
            //        //检查最终信号是否在低电平内，如果是则延时到高电平内（等待半个周期）
            //        if (timeres < periodTime / 2)
            //        {
            //            //查找所有相同位置的TriggerWait
            //            List<SequenceWaveSeg> triggerwaits = sequence.Channels.Select(x => x.Peaks[x.Peaks.IndexOf(x.GetSegFromTime(triggerstart, triggerend)[0]) - 1]).ToList();
            //            foreach (var item in triggerwaits)
            //            {
            //                item.PeakSpan = (periodTime / 2 - timeres) + periodTime / 4;
            //            }
            //        }
            //        //检查最终信号在高点平内则不等待
            //        if (timeres >= periodTime / 2)
            //        {
            //            //查找所有相同位置的TriggerWait
            //            List<SequenceWaveSeg> triggerwaits = sequence.Channels.Select(x => x.Peaks[x.Peaks.IndexOf(x.GetSegFromTime(triggerstart, triggerend)[0]) - 1]).ToList();
            //            foreach (var item in triggerwaits)
            //            {
            //                if (timeres < periodTime / 2 + periodTime / 8)
            //                    item.PeakSpan = periodTime / 2 + periodTime / 8 - timeres;
            //                else
            //                    item.PeakSpan = 0;
            //            }
            //        }
            //    }
            //}
            sequence.LoopCount = sequenceLoopCount;
            //设置pb
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.StartTriggerSample(sequenceLoopCount * LaserCount);
            //设置板卡指令
            List<CommandBase> Lines = new List<CommandBase>();
            pb.Device.SetCommands(sequence.AddToCommandLine(Lines, out string str));//读脉冲,序列写进板卡
            Thread.Sleep(1000);
            pb.Device.Start();//板卡开始输出
            List<int> ApdResult = apd.GetTriggerSamples(timeout);//apd读取，判断时间
            apd.EndTriggerSample();
            pb.Device.End();//关板卡
            try
            {
                //抽取相邻两个的数组
                List<int> before = ApdResult.Where((x, ind) => ind % 2 == 0).ToList();
                List<int> after = ApdResult.Where((x, ind) => (ind + 1) % 2 == 0).ToList();
                //差值
                List<int> det = after.Zip(before, new Func<int, int, int>((x, y) => x - y)).ToList();
                //按脉冲实验次数分割
                PulsePhotonPack pack = new PulsePhotonPack();
                int index = 0;
                var single = new List<int>();
                for (int j = 0; j < det.Count; j++)
                {
                    single.Add(det[j]);
                    ++index;
                    if (index >= LaserCount / 2)
                    {
                        pack.PulsesPhotons.Add(single);
                        single = new List<int>();
                        index = 0;
                    }
                }
                return pack;
            }
            catch (Exception ex)
            {
                throw new Exception("APD触发脉冲数必须是偶数");
            }
        }

        protected InfoBase GetSignalSwitch()
        {
            return GetDeviceByName("SignalSwitch");
        }

        public override void PreExpEventWithoutAFM()
        {
            //APDInfo apd = GetDeviceByName("APD") as APDInfo;
            //apd.StartTriggerSample(GetMaxSeqLoopCount() * GetMaxLaserCountPulses()); //apd开始计数,手动数有8个apd脉冲one，xT1 loop次数
            PreLockInExpEventWithoutAFM();
        }

        public override void AfterExpEventWithoutAFM()
        {
            AfterLockInExpEventWithoutAFM();
            //APDInfo apd = GetDeviceByName("APD") as APDInfo;
            //apd.EndTriggerSample();//停止计数
        }

        protected InfoBase GetRFSource()
        {
            return GetDeviceByName("RFSource");
        }

        /// <summary>
        /// 在进行锁相试验之前进行的操作
        /// </summary>
        public abstract void PreLockInExpEventWithoutAFM();

        /// <summary>
        /// 在进行锁相试验之后进行的操作
        /// </summary>
        public abstract void AfterLockInExpEventWithoutAFM();


        public abstract int GetMaxSeqLoopCount();

        public abstract int GetMaxLaserCountPulses();

        /// <summary>
        /// 获取脉冲实验的光子计数,返回相邻两个计数脉冲之间的计数,失败则报错
        /// </summary>
        /// <param name="rffrequency">微波频率</param>
        /// <param name="rfpower">微波功率(dbm)</param>
        /// <param name="LaserCountPulses">APD触发脉冲数,必须是偶数</param>>
        /// <returns></returns>
        protected PulsePhotonPack DoPulseExp(string pulsename, double rffrequency, double rfpower, int sequenceLoopCount, int LaserCount, int timeout)
        {
            //设置微波
            SignalGeneratorChannelInfo Rf = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            Rf.Device.Frequency = rffrequency;
            Rf.Device.Amplitude = rfpower;
            //设置序列
            var sequence = SequenceDataAssemble.ReadFromSequenceName(pulsename);
            //设置全局参数
            foreach (var item in GlobalPulseParams.GlobalPulseConfigs)
            {
                sequence.ChangeWaveSegSpan(item.PulseName, item.PulseLength);
            }

            sequence.LoopCount = sequenceLoopCount;
            //设置pb
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.StartTriggerSample(sequenceLoopCount * LaserCount);
            //设置板卡指令
            List<CommandBase> Lines = new List<CommandBase>();
            pb.Device.SetCommands(sequence.AddToCommandLine(Lines, out string str));//读脉冲,序列写进板卡
            Thread.Sleep(50);
            pb.Device.Start();
            List<int> ApdResult = apd.GetTriggerSamples(timeout);//apd读取，判断时间
            apd.EndTriggerSample();
            pb.Device.End();//关板卡
            try
            {
                //抽取相邻两个的数组
                List<int> before = ApdResult.Where((x, ind) => ind % 2 == 0).ToList();
                List<int> after = ApdResult.Where((x, ind) => (ind + 1) % 2 == 0).ToList();
                //差值
                List<int> det = after.Zip(before, new Func<int, int, int>((x, y) => x - y)).ToList();
                //按脉冲实验次数分割
                PulsePhotonPack pack = new PulsePhotonPack();
                int index = 0;
                var single = new List<int>();
                for (int j = 0; j < det.Count; j++)
                {
                    single.Add(det[j]);
                    ++index;
                    if (index >= LaserCount / 2)
                    {
                        pack.PulsesPhotons.Add(single);
                        single = new List<int>();
                        index = 0;
                    }
                }
                return pack;
            }
            catch (Exception ex)
            {
                throw new Exception("APD触发脉冲数必须是偶数");
            }
        }


        #region 交互按钮
        private void SetGlobalParams()
        {
            Dictionary<string, string> pulses = new Dictionary<string, string>();
            App.Current.Dispatcher.Invoke(() =>
            {
                //打开设置界面
                ParamInputWindow win = new ParamInputWindow("全局脉冲长度设置");
                foreach (var item in GlobalPulseParams.GlobalPulseConfigs)
                {
                    pulses.Add(item.PulseName, item.PulseLength.ToString());
                }
                win.Owner = Window.GetWindow(ParentPage);
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                pulses = win.ShowDialog(pulses);
            });
            try
            {
                if (pulses.Count != 0)
                {
                    foreach (var item in pulses)
                    {
                        GlobalPulseParams.GlobalPulseConfigs.Where(x => x.PulseName == item.Key).ElementAt(0).PulseLength = (int)double.Parse(item.Value);
                    }
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        TimeWindow twin = new TimeWindow();
                        twin.Owner = Window.GetWindow(ParentPage);
                        twin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        twin.ShowWindow("设置成功");
                    });
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    MessageWindow.ShowTipWindow("设置未完成\n" + ex.Message, Window.GetWindow(ParentPage));
                });
            }
        }


        SubExpWindow subWindow = null;
        LockInDelay DelayExp = null;
        private void DelayTest()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (DelayExp == null)
                    {
                        DelayExp = new LockInDelay();
                        var subexp = (LockInExpBase)Activator.CreateInstance(GetType());
                        DelayExp.AddSubExp(subexp);
                    }
                    if (subWindow == null)
                    {
                        subWindow = new SubExpWindow("延时时间测试", true);
                    }
                    subWindow.Show(DelayExp);
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("窗口打开失败\n" + ex.Message, Window.GetWindow(this.ParentPage));
                }
            });
        }
        #endregion
    }
}
