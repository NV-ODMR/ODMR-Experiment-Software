using CodeHelper;
using Controls.Charts;
using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static HardWares.示波器.汉泰.OscilloScope;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ODMR_Lab.实验部分.序列编辑器
{
    public class SingleChannelData
    {
        public string SequenceName { get; set; } = "";

        public List<NumricDataSeries> ChannelWaveDatas { get; set; } = new List<NumricDataSeries>();

        /// <summary>
        /// 单通道序列数据
        /// </summary>
        private List<SequenceSegBase> SingleSequenceData = new List<SequenceSegBase>();
        /// <summary>
        /// 单通道序列通道数据
        /// </summary>
        private List<object> SingleSequenceChannelData = new List<object>();

        public List<KeyValuePair<SequenceChannel, Color>> Channels { get; private set; } = new List<KeyValuePair<SequenceChannel, Color>>();

        public SingleChannelData()
        {
        }

        public SingleChannelData(List<SequenceSegBase> data, List<object> channels)
        {
            SingleSequenceData = data;
            SingleSequenceChannelData = channels;
            ValidateChannelList();
        }

        public void AddSeg(SingleSequenceWaveSeg seg, List<SequenceChannel> channels)
        {
            SingleSequenceData.Add(seg);
            SingleSequenceChannelData.Add(channels);
            ValidateChannelList();
        }
        public void AddSeg(GroupSequenceWaveSeg seg, List<KeyValuePair<int, SequenceChannel>> channels)
        {
            SingleSequenceData.Add(seg);
            SingleSequenceChannelData.Add(channels.Select(x => new KeyValuePair<string, SequenceChannel>(seg.GroupCollection[x.Key].Key, x.Value)).ToList());
            ValidateChannelList();
        }

        public void InsertSeg(int index, GroupSequenceWaveSeg seg, List<KeyValuePair<string, SequenceChannel>> channels)
        {
            SingleSequenceData.Insert(index, seg);
            SingleSequenceChannelData.Insert(index, channels.Select(x => new KeyValuePair<string, SequenceChannel>(x.Key, x.Value)).ToList());
            ValidateChannelList();
        }

        public void InsertSeg(int index, SingleSequenceWaveSeg seg, List<SequenceChannel> channels)
        {
            SingleSequenceData.Insert(index, seg);
            SingleSequenceChannelData.Insert(index, channels);
            ValidateChannelList();
        }

        public void RemoveSeg(SequenceSegBase seg)
        {
            SingleSequenceChannelData.RemoveAt(SingleSequenceData.IndexOf(seg));
            SingleSequenceData.Remove(seg);
            ValidateChannelList();
        }

        public int GetSegIndex(SequenceSegBase seg)
        {
            return SingleSequenceData.IndexOf(seg);
        }

        public void RemoveSegAt(int index)
        {
            SingleSequenceChannelData.RemoveAt(index);
            SingleSequenceData.RemoveAt(index);
            ValidateChannelList();
        }

        private List<SingleSequenceWaveSeg> GetChannelExpandedPeaks(SequenceChannel channel)
        {
            List<SingleSequenceWaveSeg> result = new List<SingleSequenceWaveSeg>();
            for (int i = 0; i < SingleSequenceData.Count; i++)
            {
                if (SingleSequenceData[i] is SingleSequenceWaveSeg)
                {
                    var seg = SingleSequenceData[i].Copy() as SingleSequenceWaveSeg;
                    seg.WaveValue = (SingleSequenceChannelData[i] as List<SequenceChannel>).Contains(channel) ? WaveValues.One : WaveValues.Zero;
                    result.Add(seg);
                }
                if (SingleSequenceData[i] is GroupSequenceWaveSeg)
                {
                    try
                    {
                        var channelcontent = (SingleSequenceChannelData[i] as List<KeyValuePair<string, SequenceChannel>>).First(x => x.Value == channel);
                        var seqs = (SingleSequenceData[i] as GroupSequenceWaveSeg).GroupCollection.First(x => x.Key == channelcontent.Key).Value.Select(x => x.Copy() as SingleSequenceWaveSeg).ToList();
                        result.AddRange(seqs);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return result;
        }

        public void UpdateGlobalPulsesLength()
        {
            foreach (var item in SingleSequenceData)
            {
                item.UpdateGlobalPulse();
            }
        }

        public void UpdatePlotData()
        {
            ChannelWaveDatas.Clear();
            int channelind = 0;
            foreach (var item in Channels)
            {
                double offset = 1.1 * channelind;
                int timex = 0;
                NumricDataSeries data = new NumricDataSeries(Enum.GetName(item.Key.GetType(), item.Key)) { LineColor = item.Value, LineThickness = 3 };
                var peaks = GetChannelExpandedPeaks(item.Key);
                foreach (var peak in peaks)
                {
                    if (peak.WaveValue == WaveValues.Zero)
                    {
                        data.X.Add(timex);
                        data.Y.Add(offset);
                        data.X.Add(timex + peak.PeakSpan);
                        data.Y.Add(offset);
                    }
                    if (peak.WaveValue == WaveValues.One)
                    {
                        data.X.Add(timex);
                        data.Y.Add(1 + offset);
                        data.X.Add(timex + peak.PeakSpan);
                        data.Y.Add(1 + offset);
                    }
                    timex += peak.PeakSpan;
                }
                ChannelWaveDatas.Add(data);
                ++channelind;
            }
            return;
        }

        /// <summary>
        /// 提取所有通道
        /// </summary>
        /// <returns></returns>
        private List<SequenceChannel> ExtractChannels(List<object> channelIDs)
        {
            List<SequenceChannel> result = new List<SequenceChannel>();
            foreach (var item in channelIDs)
            {
                if (item is List<SequenceChannel>)
                {
                    result.AddRange(item as List<SequenceChannel>);
                }
                if (item is List<KeyValuePair<string, SequenceChannel>>)
                {
                    result.AddRange((item as List<KeyValuePair<string, SequenceChannel>>).Select(x => x.Value).ToList());
                }
            }
            result = result.ToHashSet().ToList();
            result.Sort();
            return result;
        }

        private void ValidateChannelList()
        {
            var keys = Channels.Select(y => y.Key);
            var totalchannels = ExtractChannels(SingleSequenceChannelData);
            var channelstoadd = totalchannels.Where(x => !keys.Contains(x)).ToList();
            Channels.AddRange(channelstoadd.Select(x => new KeyValuePair<SequenceChannel, Color>(x, ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF2E2E2E")))).ToList());
        }

        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="channel"></param>
        public void AddChannel(SequenceChannel channel)
        {
            AddChannelID(SingleSequenceData, SingleSequenceChannelData, channel, out SingleSequenceChannelData);
            ValidateChannelList();
            if (Channels.Count == 0)
                Channels.Add(new KeyValuePair<SequenceChannel, Color>(channel, ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF2E2E2E"))));
        }

        /// <summary>
        /// 移除通道
        /// </summary>
        /// <param name="channel"></param>
        public void RemoveChannel(SequenceChannel channel)
        {
            DeleteChannelID(SingleSequenceChannelData, channel, out SingleSequenceChannelData);
            ValidateChannelList();
            Channels.Remove(Channels.First(x => x.Key == channel));
        }

        /// <summary>
        /// 从 channelIDs:通道序号信息中删除指定的通道序号
        /// </summary>
        /// <param name="channelIDs"></param>
        /// <param name="deletedchannelIDs"></param>
        private void DeleteChannelID(List<object> channelIDs, SequenceChannel channelToDelete, out List<object> deletedchannelIDs)
        {
            deletedchannelIDs = new List<object>();
            foreach (var channel in channelIDs)
            {
                if (channel is List<SequenceChannel>)
                {
                    deletedchannelIDs.Add((channel as List<SequenceChannel>).Where(x => x != channelToDelete).ToList());
                }
                if (channel is List<KeyValuePair<string, SequenceChannel>>)
                {
                    deletedchannelIDs.Add((channel as List<KeyValuePair<string, SequenceChannel>>).Where(x => x.Value != channelToDelete).ToList());
                }
            }
        }

        /// <summary>
        /// 从 channelIDs:通道序号信息中增加指定的通道序号
        /// </summary>
        /// <param name="channelIDs"></param>
        /// <param name="deletedchannelIDs"></param>
        private void AddChannelID(List<SequenceSegBase> segs, List<object> channelIDs, SequenceChannel channelToAdd, out List<object> addedchannelIDs)
        {
            addedchannelIDs = new List<object>();
            foreach (var channel in channelIDs)
            {
                if (channel is List<SequenceChannel>)
                {
                    var copy = (channel as List<SequenceChannel>).Select(x => x).ToList();
                    if (copy.Where(x => x == channelToAdd).Count() == 0)
                        copy.Add(channelToAdd);
                    addedchannelIDs.Add(copy);
                }
                if (channel is List<KeyValuePair<string, SequenceChannel>>)
                {
                    var copy = (channel as List<KeyValuePair<string, SequenceChannel>>).Select(x => x).ToList();
                    if (copy.Where(x => x.Value == channelToAdd).Count() == 0)
                        copy.Add(new KeyValuePair<string, SequenceChannel>((segs[channelIDs.IndexOf(channel)] as GroupSequenceWaveSeg).GroupCollection[0].Key, channelToAdd));
                    addedchannelIDs.Add(copy);
                }
            }
        }

        #region 转换部分
        /// <summary>
        /// 转换成格式单通道格式，转换未完成则报错,
        /// 返回结果:
        /// segs:对应的单通道序列
        /// channelIDs:通道序号信息:
        /// 对于单脉冲，返回一个List<SequenceChannel>类型，包含所有波形为1的通道序号
        /// 对于组合脉冲，返回一个List<KeyValuePair<string,SequenceChannel>>类型，包含脉冲组合中通道和通道序号的对应关系
        /// </summary>
        /// <param name="segs"></param>
        /// <param name="channelIDs"></param>
        public void ReadFromSequenceAssemble(SequenceDataAssemble assemble)
        {
            SingleSequenceData.Clear();
            SingleSequenceChannelData.Clear();
            Channels.Clear();

            Channels = assemble.Channels.Select(x => new KeyValuePair<SequenceChannel, Color>(x.ChannelInd, ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF2E2E2E")))).ToList();
            if (assemble.Channels.Count == 0)
            {
                return;
            }
            assemble.CheckChannelFormat();
            var counts = assemble.Channels.Select(x => x.Peaks.Count).ToHashSet();
            if (counts.Count > 1) throw new Exception("存在脉冲数不相同的通道，无法转换成单通道格式");
            if (counts.ElementAt(0) == 0)
            {
                return;
            }

            int count = counts.ElementAt(0);
            for (int i = 0; i < count; i++)
            {
                var waves = assemble.Channels.Select(x => x.Peaks[i]);
                var peaknames = waves.Select(x => x.PeakName).ToHashSet();
                var peaklengths = waves.Select(x => x.PeakSpan).ToHashSet();
                var peakistrigger = waves.Select(x => x.IsTrigger()).ToHashSet();
                var peaktypes = waves.Select(x => x.GetType().FullName).ToHashSet();
                if (peaknames.Count > 1 || peaktypes.Count > 1 || peaklengths.Count > 1 || peakistrigger.Count > 1)
                    throw new Exception("存在对应位置脉冲不相同的通道，无法转换成单通道格式");
                if (waves.ElementAt(0) is SingleSequenceWaveSeg)
                {
                    SingleSequenceData.Add(new SingleSequenceWaveSeg(peaknames.ElementAt(0), peaklengths.ElementAt(0), WaveValues.Zero, null, peakistrigger.ElementAt(0)));
                    var onechannels = waves.Where(x => x.IsWaveOne()).Select(x => x.ParentChannel.ChannelInd).ToList();
                    SingleSequenceChannelData.Add(onechannels);
                }
                else
                {
                    var group = waves.ElementAt(0).Copy() as GroupSequenceWaveSeg;
                    SingleSequenceData.Add(group);
                    //获取通道对应关系
                    SingleSequenceChannelData.Add(waves.Select((x, ind) => { var g = x as GroupSequenceWaveSeg; return new KeyValuePair<string, SequenceChannel>(g.GroupCollection[g.SelectChnnelInd].Key, assemble.Channels[ind].ChannelInd); }).ToList());
                }
            }

            ValidateChannelList();
            return;
        }

        public SequenceDataAssemble GenerateSequenceAssemble()
        {
            SequenceDataAssemble assemble = new SequenceDataAssemble();
            assemble.Name = SequenceName;
            //新建通道
            foreach (var item in Channels)
            {
                var singlechannel = new SequenceChannelData(item.Key);
                for (int i = 0; i < SingleSequenceData.Count; i++)
                {
                    if (SingleSequenceData[i] is SingleSequenceWaveSeg)
                    {
                        var seg = (SingleSequenceData[i] as SingleSequenceWaveSeg).Copy() as SingleSequenceWaveSeg;
                        seg.WaveValue = (SingleSequenceChannelData[i] as List<SequenceChannel>).Contains(item.Key) ? WaveValues.One : WaveValues.Zero;
                        singlechannel.Peaks.Add(seg);
                    }
                    if (SingleSequenceData[i] is GroupSequenceWaveSeg)
                    {
                        var seg = (SingleSequenceData[i] as GroupSequenceWaveSeg).Copy() as GroupSequenceWaveSeg;
                        var cannellist = SingleSequenceChannelData[i] as List<KeyValuePair<string, SequenceChannel>>;
                        seg.SelectChnnelInd = seg.GroupCollection.FindIndex(x => x.Key == cannellist.Find((y) => y.Value == item.Key).Key);
                        singlechannel.Peaks.Add(seg);
                    }
                }
                assemble.Channels.Add(singlechannel);
            }
            return assemble;
        }
        #endregion

        public List<SequenceChannel> GetSinglePulseChannels(SingleSequenceWaveSeg seg)
        {
            int index = SingleSequenceData.IndexOf(seg);
            if (index == -1) return new List<SequenceChannel>();
            return SingleSequenceChannelData[index] as List<SequenceChannel>;
        }

        public void AddSinglePulseChannel(SingleSequenceWaveSeg seg, SequenceChannel channel)
        {
            if (!Channels.Select(x => x.Key).Contains(channel)) return;
            int index = SingleSequenceData.IndexOf(seg);
            if (index == -1) return;
            var list = SingleSequenceChannelData[index] as List<SequenceChannel>;
            list.Add(channel);
            list = list.Distinct().ToList();
            return;
        }

        public void DeleteSinglePulseChannel(SingleSequenceWaveSeg seg, SequenceChannel channel)
        {
            if (!Channels.Select(x => x.Key).Contains(channel)) return;
            int index = SingleSequenceData.IndexOf(seg);
            if (index == -1) return;
            var list = SingleSequenceChannelData[index] as List<SequenceChannel>;
            list.Remove(channel);
            list = list.Distinct().ToList();
            return;
        }

        public Color GetChannelColor(SequenceChannel channel)
        {
            return Channels.Where(x => x.Key == channel).ElementAt(0).Value;
        }

        public List<KeyValuePair<string, SequenceChannel>> GetGroupPulseChannelRelations(GroupSequenceWaveSeg seg)
        {
            int index = SingleSequenceData.IndexOf(seg);
            if (index == -1) return new List<KeyValuePair<string, SequenceChannel>>();
            return SingleSequenceChannelData[index] as List<KeyValuePair<string, SequenceChannel>>;
        }

        public void SetGroupPulseChannelRelations(GroupSequenceWaveSeg seg, List<KeyValuePair<string, SequenceChannel>> relations)
        {
            int index = SingleSequenceData.IndexOf(seg);
            if (index == -1) return;
            SingleSequenceChannelData[index] = relations;
        }

        public List<SequenceSegBase> GetSequences()
        {
            return SingleSequenceData;
        }
    }
}
