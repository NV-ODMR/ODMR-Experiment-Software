using Controls.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.序列编辑器
{
    public class SequenceChannelData
    {
        /// <summary>
        /// 序列名称
        /// </summary>
        public SequenceChannel ChannelInd { get; set; } = SequenceChannel.None;

        /// <summary>
        /// 峰位置，键值为起始时间
        /// </summary>
        public List<SequenceWaveSeg> Peaks { get; set; } = new List<SequenceWaveSeg>();

        public SequenceChannelData(SequenceChannel channel)
        {
            ChannelInd = channel;
            ChannelWaveData = new NumricDataSeries("") { LineThickness = 3, LineColor = CodeHelper.ColorHelper.GenerateHighContrastColor(Colors.Black) };
        }

        public NumricDataSeries ChannelWaveData { get; set; }

        public bool IsDisplay { get; set; } = false;

        /// <summary>
        /// 判断在给定时间段内是否是1脉冲
        /// </summary>
        /// <param name="timestart"></param>
        /// <param name="timeend"></param>
        /// <param name="loopcount"></param>
        /// <returns></returns>
        public bool IsWaveOne(int timestart, int timeend)
        {
            int time = 0;
            foreach (var item in Peaks)
            {
                int segstart = time;
                int segend = time + item.PeakSpan;
                if (timestart >= segstart && timeend <= segend && item.WaveValue == WaveValues.One)
                {
                    return true;
                }
                time += item.PeakSpan;
            }

            return false;
        }

        public void GetSegTime(SequenceWaveSeg seg, out int timestart, out int timeend)
        {
            int time = 0;
            for (int i = 0; i < Peaks.Count; i++)
            {
                if (Peaks[i] == seg)
                {
                    timestart = time;
                    timeend = time + Peaks[i].PeakSpan;
                    return;
                }
                time += Peaks[i].PeakSpan;
            }
            timestart = -1;
            timeend = -1;
            return;
        }

        public List<SequenceWaveSeg> GetSegFromTime(int timestart, int timeend)
        {
            List<SequenceWaveSeg> res = new List<SequenceWaveSeg>();
            int time = 0;
            for (int i = 0; i < Peaks.Count; i++)
            {
                int start = time;
                int end = time + Peaks[i].PeakSpan;
                time += Peaks[i].PeakSpan;
                if (start >= timestart && end <= timeend && Peaks[i].PeakSpan != 0) 
                    res.Add(Peaks[i]);
            }
            return res;
        }

        /// <summary>
        /// 获取两个指定波形之间的时间(不含endSegInd)
        /// </summary>
        /// <param name="startSeg"></param>
        /// <param name="endSeg"></param>
        /// <returns></returns>
        public int GetExpSegTime(int startSegInd, int endSegInd)
        {
            int time = 0;
            for (int i = startSegInd; i < endSegInd; i++)
            {
                time += Peaks[i].PeakSpan;
            }
            return time;
        }
    }

    /// <summary>
    /// 脉冲峰位置
    /// </summary>
    public class SequenceWaveSeg
    {
        public SequenceChannelData ParentChannel { get; set; } = null;

        /// <summary>
        /// 脉冲名称
        /// </summary>
        public string PeakName { get; set; } = "";
        /// <summary>
        /// 起始脉冲时间
        /// </summary>
        public int PeakSpan { get; set; } = 0;

        /// <summary>
        /// 是否是Trigger指令
        /// </summary>
        public bool IsTriggerCommand { get; set; } = false;

        public WaveValues WaveValue { get; set; } = WaveValues.Zero;

        public SequenceWaveSeg(string peakName, int peakSpan, WaveValues waveValue, SequenceChannelData parentchannel, bool IsTrigger = false)
        {
            PeakName = peakName;
            PeakSpan = peakSpan;
            WaveValue = waveValue;
            ParentChannel = parentchannel;
            IsTriggerCommand = IsTrigger;
        }
    }

    public enum WaveValues
    {
        Zero = 0,
        One = 1
    }
}
