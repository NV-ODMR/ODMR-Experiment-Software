using Controls.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        public List<SequenceSegBase> Peaks { get; set; } = new List<SequenceSegBase>();

        /// <summary>
        ///将所有组合的峰展开后的结果
        /// </summary>
        //public List<SingleSequenceWaveSeg> ExpandedPeaks { get; private set; } = new List<SingleSequenceWaveSeg>();

        public SequenceChannelData(SequenceChannel channel)
        {
            ChannelInd = channel;
            ChannelWaveData = new NumricDataSeries("") { LineThickness = 3, LineColor = CodeHelper.ColorHelper.GenerateHighContrastColor(Colors.Black) };
        }

        public static List<SingleSequenceWaveSeg> GetExpandedPeakArray(IEnumerable<SequenceSegBase> segs)
        {
            List<SingleSequenceWaveSeg> result = new List<SingleSequenceWaveSeg>();
            foreach (var item in segs)
            {
                if (item is SingleSequenceWaveSeg)
                {
                    result.Add(item as SingleSequenceWaveSeg);
                }
                if (item is GroupSequenceWaveSeg)
                {
                    result.AddRange((item as GroupSequenceWaveSeg).GetSelectedChannelSegs());
                }
            }
            return result;
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
                if (timestart >= segstart && timeend <= segend && item.IsWaveOne())
                {
                    return true;
                }
                time += item.PeakSpan;
            }

            return false;
        }

        public void GetSegTime(SequenceSegBase seg, out int timestart, out int timeend)
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

        public List<SequenceSegBase> GetSegFromTime(int timestart, int timeend)
        {
            List<SequenceSegBase> res = new List<SequenceSegBase>();
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
}
