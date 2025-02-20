using CodeHelper;
using HardWares.APD;
using HardWares.板卡;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
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
        /// T1单点扫描方法：输入参数：Pi脉冲长度(整数),T1间隔长度(整数),采样循环次数(整数)
        /// 设备:板卡，光子计数器
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            var sequence = SequenceDataAssemble.ReadFromSequenceName("T1");
            sequence.ChangeWaveSegSpan("Pi", (int)InputParams[0]);
            sequence.ChangeWaveSegSpan("T1Step", (int)InputParams[1]);
            sequence.LoopCount = (int)InputParams[2];
            PulseBlasterInfo pb = devices[0] as PulseBlasterInfo;
            APDInfo apd = devices[1] as APDInfo;
            //设置板卡指令
            pb.Device.SetCommands(sequence.ConvertToCommandLine(out string str));
            apd.StartTriggerSample(2);

            return new List<object>();
        }
    }
}
