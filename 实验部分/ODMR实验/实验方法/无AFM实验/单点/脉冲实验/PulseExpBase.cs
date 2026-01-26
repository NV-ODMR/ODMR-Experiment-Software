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

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验
{
    public abstract class PulseExpBase : ODMRExperimentWithoutAFM
    {
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            /// 设备:板卡，光子计数器,微波源
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("板卡","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.信号发生器通道,new Param<string>("射频信号通道","","RFSource")),
        };
        /// <summary>
        /// 脉冲实验的输入参数
        /// </summary>
        public abstract List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; }

        public PulseExpBase()
        {
            AddDevicesToList(PulseExpDevices);
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            var btns = new List<KeyValuePair<string, Action>>()
            {
                new KeyValuePair<string, Action>("设置全局脉冲参数",SetGlobalParams)
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
        /// <param name="LaserCountPulses">APD触发脉冲数,必须是偶数</param>>
        /// <returns></returns>
        protected PulsePhotonPack DoPulseExp(string pulsename, double rffrequency, double rfpower, int loopcount, int LaserCountPulses, int timeout, Action<SequenceDataAssemble> sequenceAction = null)
        {
            //设置微波
            SignalGeneratorChannelInfo Rf = GetDeviceByName("RFSource") as SignalGeneratorChannelInfo;
            Rf.Device.Frequency = rffrequency;
            Rf.Device.Amplitude = rfpower;
            Rf.Device.Frequency = rffrequency;
            Rf.Device.Amplitude = rfpower;
            //设置序列
            var sequence = SequenceDataAssemble.ReadFromSequenceName(pulsename);
            sequenceAction?.Invoke(sequence);
            //设置全局参数
            foreach (var item in GlobalPulseParams.GlobalPulseConfigs)
            {
                sequence.ChangeWaveSegSpan(item.PulseName, item.PulseLength);
            }

            sequence.LoopCount = loopcount;
            //设置pb
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            //设置板卡指令
            List<CommandBase> Lines = new List<CommandBase>();
            pb.Device.SetCommands(sequence.AddToCommandLine(Lines, out string str));//读脉冲,序列写进板卡
            apd.StartTriggerSample(sequence.LoopCount * LaserCountPulses); //apd开始计数,手动数有8个apd脉冲one，xT1 loop次数
            Thread.Sleep(50);
            pb.Device.Start();
            List<int> ApdResult = apd.GetTriggerSamples(timeout);//apd读取，判断时间
            apd.EndTriggerSample();//停止计数
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
                    if (index >= LaserCountPulses / 2)
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
        #endregion
    }
}
