using Controls.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.序列编辑器
{
    internal class SequenceChannelData
    {
        /// <summary>
        /// 序列名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 峰位置，键值为起始时间
        /// </summary>
        public List<SequenceWaveSeg> Peaks { get; set; } = new List<SequenceWaveSeg>();

        public SequenceChannelData(string name)
        {
            Name = name;
            ChannelWaveData = new NumricDataSeries(name) { LineThickness = 3, LineColor = CodeHelper.ColorHelper.GenerateHighContrastColor(Colors.Black) };
        }

        public NumricDataSeries ChannelWaveData { get; set; }

        public bool IsDisplay { get; set; } = false;
    }

    /// <summary>
    /// 脉冲峰位置
    /// </summary>
    internal class SequenceWaveSeg
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
        /// 循环步长
        /// </summary>
        public int Step { get; set; } = 0;

        public WaveValues WaveValue { get; set; } = WaveValues.Zero;

        public SequenceWaveSeg(string peakName, int peakSpan, int step, WaveValues waveValue, SequenceChannelData parentchannel)
        {
            PeakName = peakName;
            PeakSpan = peakSpan;
            Step = step;
            WaveValue = waveValue;
            ParentChannel = parentchannel;
        }
    }

    internal enum WaveValues
    {
        Zero = 0,
        One = 1
    }
}
