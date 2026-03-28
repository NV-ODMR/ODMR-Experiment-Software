using Controls.Charts;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// 脉冲峰位置
    /// </summary>
    public class SingleSequenceWaveSeg : SequenceSegBase
    {
        /// <summary>
        /// 是否是Trigger指令
        /// </summary>
        public bool IsTriggerCommand { get; set; } = false;

        public WaveValues WaveValue { get; set; } = WaveValues.Zero;

        public SingleSequenceWaveSeg(string peakName, int peakSpan, WaveValues waveValue, SequenceChannelData parentchannel, bool IsTrigger = false)
        {
            PeakName = peakName;
            PeakSpan = peakSpan;
            WaveValue = waveValue;
            ParentChannel = parentchannel;
            IsTriggerCommand = IsTrigger;
        }

        public override SequenceSegBase Copy()
        {
            return new SingleSequenceWaveSeg(PeakName, PeakSpan, WaveValue, ParentChannel, IsTriggerCommand);
        }

        public override bool IsWaveOne()
        {
            return WaveValue == WaveValues.One ? true : false;
        }
        public override bool IsTrigger()
        {
            return IsTriggerCommand;
        }

        public override void UpdateGlobalPulse(bool throwexception = false)
        {
            if (GlobalPulseParams.ExistsInGlobal(PeakName))
                PeakSpan = GlobalPulseParams.GetGlobalPulseLength(PeakName);
            else
            {
                if (throwexception) throw new Exception("全局参数不存在:" + PeakName);
            }
        }
    }

    public enum WaveValues
    {
        Zero = 0,
        One = 1
    }
}
