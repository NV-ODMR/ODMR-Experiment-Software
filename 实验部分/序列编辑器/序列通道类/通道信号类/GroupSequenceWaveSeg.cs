using CodeHelper;
using Controls.Charts;
using NetTopologySuite.Utilities;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static HardWares.示波器.汉泰.OscilloScope;

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// 脉冲峰位置
    /// </summary>
    public class GroupSequenceWaveSeg : SequenceSegBase
    {
        public override int PeakSpan
        {
            get
            {
                try
                {
                    return GroupCollection.First().Value.Sum(x => x.PeakSpan);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            set { return; }
        }


        public List<KeyValuePair<string, List<SingleSequenceWaveSeg>>> GroupCollection = new List<KeyValuePair<string, List<SingleSequenceWaveSeg>>>();

        public int SelectChnnelInd { get; set; } = 0;

        public List<SingleSequenceWaveSeg> GetSelectedChannelSegs()
        {
            try
            {
                return GroupCollection[SelectChnnelInd].Value;
            }
            catch (Exception)
            {
                return new List<SingleSequenceWaveSeg>();
            }
        }

        public void ReadFromFile(string SequenceName)
        {
            GroupCollection.Clear();
            if (SequenceName == "") return;
            FileObject obj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "SequenceGroup", SequenceName + ".userdat"));
            PeakName = obj.Descriptions["GroupName"];
            var names = obj.ExtractString("ChannelNames");
            for (int i = 0; i < names.Count; i++)
            {
                var peaknames = obj.ExtractString("ChannelName→" + names[i] + "→" + "PeakNames").ToList();
                var peakvalues = obj.ExtractString("ChannelName→" + names[i] + "→" + "PeakValues").Select(x => (WaveValues)Enum.Parse(typeof(WaveValues), x)).ToList();
                var peakspans = obj.ExtractString("ChannelName→" + names[i] + "→" + "Spans").Select(x => int.Parse(x)).ToList();
                var peaktriggers = obj.ExtractString("ChannelName→" + names[i] + "→" + "IsTrigger").Select(x => bool.Parse(x)).ToList();

                List<SingleSequenceWaveSeg> channeldata = new List<SingleSequenceWaveSeg>();

                for (int j = 0; j < peaktriggers.Count; j++)
                {
                    channeldata.Add(new SingleSequenceWaveSeg(peaknames[j], peakspans[j], peakvalues[j], null, peaktriggers[j]));
                }

                GroupCollection.Add(new KeyValuePair<string, List<SingleSequenceWaveSeg>>(names[i], channeldata));
            }
        }

        public void WriteToFile()
        {
            FileObject obj = new FileObject();
            obj.Descriptions.Add("GroupName", PeakName);

            var datanames = GroupCollection.Select(x => x.Key).ToList();
            var hashset = datanames.ToHashSet();
            if (datanames.Count != hashset.Count)
            {
                throw new Exception("对于待保存的序列组，任意两个通道的名称不能相同");
            }

            PeakName = obj.Descriptions["GroupName"];

            obj.WriteStringData("ChannelNames", datanames);

            foreach (var item in GroupCollection)
            {
                string chname = item.Key;
                var peaknames = item.Value.Select(x =>
                {
                    return (x).PeakName;
                }).ToList();
                var peakvalues = item.Value.Select(x =>
                {
                    return Enum.GetName(typeof(WaveValues), (x).WaveValue);
                }).ToList();
                var ispeaktrigger = item.Value.Select(x =>
                {
                    return (x as SingleSequenceWaveSeg).IsTriggerCommand.ToString();
                }).ToList();
                var peakspans = item.Value.Select(x =>
                {
                    return (x).PeakSpan.ToString();
                }).ToList();

                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "PeakNames"), peaknames);
                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "PeakValues"), peakvalues);
                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "Spans"), peakspans);
                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "IsTrigger"), ispeaktrigger);

                if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, "SequenceGroup")) == false)
                {
                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "SequenceGroup"));
                }

                obj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "SequenceGroup", PeakName + ".userdat"));
            }


        }

        public SequenceDataAssemble ConvertToAssemble()
        {
            SequenceDataAssemble assem = new SequenceDataAssemble();
            assem.Name = PeakName;
            int index = 0;
            foreach (var item in GroupCollection)
            {
                var channel = new SequenceChannelData((SequenceChannel)index);
                channel.Peaks.AddRange(item.Value.Select(x => { var s = x.Copy() as SingleSequenceWaveSeg; s.ParentChannel = channel; return s; }));
                assem.Channels.Add(channel);
                ++index;
            }
            return assem;
        }

        public static List<string> EnumerateGroupNames()
        {
            List<string> res = new List<string>();
            if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, "SequenceGroup")) == false)
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "SequenceGroup"));
            }
            var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "SequenceGroup"), "*", SearchOption.AllDirectories);
            foreach (var item in files)
            {
                try
                {
                    res.Add(FileObject.ReadDescription(item)["GroupName"]);
                }
                catch (Exception)
                {

                }
            }
            return res;
        }

        public override SequenceSegBase Copy()
        {
            GroupSequenceWaveSeg seg = new GroupSequenceWaveSeg();
            seg.SelectChnnelInd = SelectChnnelInd;
            seg.PeakName = PeakName;
            foreach (var item in GroupCollection)
            {
                seg.GroupCollection.Add(new KeyValuePair<string, List<SingleSequenceWaveSeg>>(item.Key, item.Value.Select(x => x.Copy() as SingleSequenceWaveSeg).ToList()));
            }
            return seg;
        }

        public void AttachToChannel(SequenceChannelData channel)
        {
            foreach (var item in GroupCollection)
            {
                foreach (var ele in item.Value)
                {
                    ele.ParentChannel = channel;
                }
            }
        }

        public override bool IsWaveOne()
        {
            var res = GroupCollection[SelectChnnelInd].Value.Select(x => x.WaveValue).ToHashSet();
            if (res.Count == 1 && res.ElementAt(0) == WaveValues.One) return true;
            return false;
        }

        public override bool IsTrigger()
        {
            var res = GroupCollection[SelectChnnelInd].Value.Select(x => x.IsTriggerCommand).ToHashSet();
            if (res.Count == 1 && res.ElementAt(0) == true) return true;
            return false;
        }

        public override void UpdateGlobalPulse(bool throwexception = false)
        {
            var collections = GroupCollection;
            for (int i = 0; i < collections.Count; i++)
            {
                collections[i] = new KeyValuePair<string, List<SingleSequenceWaveSeg>>(collections[i].Key, collections[i].Value.Select(x =>
                {
                    if (GlobalPulseParams.ExistsInGlobal(x.PeakName))
                        x.PeakSpan = GlobalPulseParams.GetGlobalPulseLength(x.PeakName);
                    else
                    {
                        if (throwexception) throw new Exception("全局参数不存在:" + x.PeakName);
                    }
                    return x;
                }).ToList());
            }
        }
    }
}
