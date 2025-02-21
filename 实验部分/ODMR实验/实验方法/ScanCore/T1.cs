using CodeHelper;
using HardWares.APD;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.板卡;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    /// <summary>
    /// T1扫描核心方法
    /// </summary>
    public class T1 : ScanCoreBase
    {
        /// <summary>
        /// T1单点扫描方法：输入参数：0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
        /// 设备:板卡，光子计数器,微波源
        /// 返回:信号计数,参考信号计数
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            //设置微波
            RFSourceInfo Rf = devices[2] as RFSourceInfo;
            Rf.Device.RFFrequency = (double)InputParams[4];
            Rf.Device.RFAmplitude = (double)InputParams[5];
            //设置序列
            var sequence = SequenceDataAssemble.ReadFromSequenceName("T1");
            sequence.ChangeWaveSegSpan("Pi", (int)InputParams[0]);
            sequence.ChangeWaveSegSpan("T1Step", (int)InputParams[1]);
            sequence.LoopCount = (int)InputParams[2];
            //设置pb
            PulseBlasterInfo pb = devices[0] as PulseBlasterInfo;
            APDInfo apd = devices[1] as APDInfo;
            //设置板卡指令
            List<CommandBase> Lines = new List<CommandBase>();
            pb.Device.SetCommands(sequence.AddToCommandLine(Lines,out string str));//读脉冲,序列写进板卡
            apd.StartTriggerSample(sequence.LoopCount * 8); //apd开始计数,手动数有8个apd脉冲one，xT1 loop次数
            pb.Device.Start();//板卡开始输出
            List<int> ApdResult=apd.GetTriggerSamples((int)InputParams[3]);//apd读取，判断时间
            apd.EndTriggerSample();//停止计数
            pb.Device.End();//关板卡
            //处理数据
            var ApdBefore = ApdResult.Where((v, ind) => ind % 2 == 0);
            var ApdAfter = ApdResult.Where((v, ind) => ind % 2 == 1);
            List<int> ApdCount = new List<int>();
            int Sum0 = 0;
            int Sum1 = 0;
            int Sum2 = 0;
            int Sum3 = 0;
            try
            {
                for (int i = 0; i < ApdAfter.Count(); i++)
                {
                    ApdCount.Add(ApdAfter.ElementAt(i) - ApdBefore.ElementAt(i));
                }
                for (int i = 0; i < ApdCount.Count()/4; i++)
                {
                    Sum0 += ApdCount.ElementAt(4*i);
                    Sum1 += ApdCount.ElementAt(4 * i + 1);
                    Sum2 += ApdCount.ElementAt(4 * i + 2);
                    Sum3 += ApdCount.ElementAt(4 * i + 3);
                }
                double ContrastRef = (Sum0 - Sum1) / Sum1;
                double ContrastSig = (Sum2 - Sum3) / Sum3;
                return new List<object>() { ContrastSig, ContrastRef };
            }
            catch (Exception)
            {
                return new List<object>() {0,0 };
            }
        }
    }
}
