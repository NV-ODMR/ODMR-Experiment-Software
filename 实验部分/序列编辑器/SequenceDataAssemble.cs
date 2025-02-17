using CodeHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.序列编辑器
{
    internal class SequenceDataAssemble
    {
        public string Name { get; set; } = "";

        /// <summary>
        /// 循环次数
        /// </summary>
        public int LoopCount { get; set; } = 0;

        public List<SequenceChannelData> Channels { get; set; } = new List<SequenceChannelData>();

        public SequenceDataAssemble(string name, List<SequenceChannelData> datas)
        {
            Name = name;
            Channels = datas;
        }
        public SequenceDataAssemble()
        {
        }


        /// <summary>
        /// 读取
        /// </summary>
        public static SequenceDataAssemble ReadFromFile(FileObject obj)
        {
            SequenceDataAssemble assem = new SequenceDataAssemble();
            assem.Name = obj.Descriptions["SequenceAssembleName"];
            assem.LoopCount = int.Parse(obj.Descriptions["SequenceAssembleLoopCount"]);
            var names = obj.ExtractString("ChannelNames");
            for (int i = 0; i < names.Count; i++)
            {
                var peaknames = obj.ExtractString("ChannelName→" + names[i] + "→" + "PeakNames");
                var peakvalues = obj.ExtractString("ChannelName→" + names[i] + "→" + "PeakValues");
                var peakspans = obj.ExtractString("ChannelName→" + names[i] + "→" + "Spans").Select(x => int.Parse(x)).ToList();
                var peaksteps = obj.ExtractString("ChannelName→" + names[i] + "→" + "Steps").Select(x => int.Parse(x)).ToList();

                SequenceChannelData channeldata = new SequenceChannelData(names[i]);

                for (int j = 0; j < peaknames.Count; j++)
                {
                    channeldata.Peaks.Add(new SequenceWaveSeg(peaknames[j], peakspans[j], peaksteps[j], (WaveValues)Enum.Parse(typeof(WaveValues), peakvalues[j]), channeldata));
                }

                assem.Channels.Add(channeldata);
            }
            return assem;
        }

        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="obj"></param>
        public void WriteToFile()
        {
            FileObject obj = new FileObject();
            obj.Descriptions.Add("SequenceAssembleName", Name);
            obj.Descriptions.Add("SequenceAssembleLoopCount", LoopCount.ToString());

            var datanames = Channels.Select(x => x.Name).ToList();
            obj.WriteStringData("ChannelNames", datanames);
            foreach (var item in Channels)
            {
                var peaknames = item.Peaks.Select(x => x.PeakName.ToString()).ToList();
                var peakvalues = item.Peaks.Select(x => Enum.GetName(typeof(WaveValues), x.WaveValue)).ToList();
                var peaksteps = item.Peaks.Select(x => x.Step.ToString()).ToList();
                var peakspans = item.Peaks.Select(x => x.PeakSpan.ToString()).ToList();
                obj.WriteStringData("ChannelName→" + item.Name + "→" + "PeakNames", peaknames);
                obj.WriteStringData("ChannelName→" + item.Name + "→" + "PeakValues", peakvalues);
                obj.WriteStringData("ChannelName→" + item.Name + "→" + "Spans", peakspans);
                obj.WriteStringData("ChannelName→" + item.Name + "→" + "Steps", peaksteps);
            }

            //保存到目标文件夹
            DirectoryInfo info = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequences"));
            obj.SaveToFile(Path.Combine(info.FullName, Name.ToString()));
        }
    }
}
