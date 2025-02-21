using CodeHelper;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

                SequenceChannelData channeldata = new SequenceChannelData((SequenceChannel)Enum.Parse(typeof(SequenceChannel), names[i]));

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

            var datanames = Channels.Select(x => Enum.GetName(x.ChannelInd.GetType(), x.ChannelInd)).ToList();
            obj.WriteStringData("ChannelNames", datanames);
            foreach (var item in Channels)
            {
                string chname = Enum.GetName(item.ChannelInd.GetType(), item.ChannelInd);
                var peaknames = item.Peaks.Select(x => x.PeakName.ToString()).ToList();
                var peakvalues = item.Peaks.Select(x => Enum.GetName(typeof(WaveValues), x.WaveValue)).ToList();
                var peaksteps = item.Peaks.Select(x => x.Step.ToString()).ToList();
                var peakspans = item.Peaks.Select(x => x.PeakSpan.ToString()).ToList();
                obj.WriteStringData("ChannelName→" + chname + "→" + "PeakNames", peaknames);
                obj.WriteStringData("ChannelName→" + chname + "→" + "PeakValues", peakvalues);
                obj.WriteStringData("ChannelName→" + chname + "→" + "Spans", peakspans);
                obj.WriteStringData("ChannelName→" + chname + "→" + "Steps", peaksteps);
            }

            //保存到目标文件夹
            DirectoryInfo info = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequences"));
            obj.SaveToFile(Path.Combine(info.FullName, Name.ToString()));
        }


        /// <summary>
        /// 从文件中读取序列，读不到则报错
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static SequenceDataAssemble ReadFromSequenceName(string name)
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Sequences", name.Replace(".userdat", "") + ".userdat")))
            {
                throw new Exception("找不到序列文件:" + name);
            }
            var obj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "Sequences", name.Replace(".userdat", "") + ".userdat"));
            return ReadFromFile(obj);
        }

        /// <summary>
        /// 转换成PB指令
        /// </summary>
        /// <returns></returns>
        public List<CommandBase> AddToCommandLine(List<CommandBase> InputCommand, out string ComandInformation)
        {
            ComandInformation = "";

            InputCommand.Add(new LoopStartCommandLine(LoopCount));
            int loopstartind = InputCommand.Count - 1;

            //找1脉冲的时间点
            HashSet<int> OneTimes = new HashSet<int>();
            foreach (var wave in Channels)
            {
                int time = 0;
                foreach (var peak in wave.Peaks)
                {
                    if (peak.WaveValue == WaveValues.One)
                    {
                        OneTimes.Add(time);
                        time += peak.PeakSpan;
                        OneTimes.Add(time);
                    }
                    else
                    {
                        time += peak.PeakSpan;
                    }
                }
            }
            //时间从低到高排序
            var sortedTimes = OneTimes.ToList();
            sortedTimes.Sort();
            for (int j = 0; j < sortedTimes.Count - 1; j++)
            {
                List<int> HighChannelIndexes = new List<int>();
                foreach (var ch in Channels)
                {
                    if (ch.IsWaveOne(sortedTimes[j], sortedTimes[j + 1]))
                    {
                        HighChannelIndexes.Add((int)ch.ChannelInd);
                    }
                }
                CommandLine singlecommand = new CommandLine(HighChannelIndexes, sortedTimes[j + 1] - sortedTimes[j]);
                //打印指令结果
                ComandInformation += "Channels:";
                foreach (var item in HighChannelIndexes)
                {
                    ComandInformation += item.ToString() + "\t";
                }
                ComandInformation += "\t";
                ComandInformation += "TimeSpan:" + (sortedTimes[j + 1] - sortedTimes[j]).ToString() + "\n";
                InputCommand.Add(singlecommand);
            }

            InputCommand.Add(new LoopEndCommandLine(loopstartind));

            return InputCommand;
        }

        /// <summary>
        /// 改变特殊序列名的脉冲长度
        /// </summary>
        public void ChangeWaveSegSpan(string name, int spanvalue)
        {
            foreach (var ch in Channels)
            {
                foreach (var wave in ch.Peaks)
                {
                    if (wave.PeakName == name)
                    {
                        wave.PeakSpan = spanvalue;
                    }
                }
            }
        }

        /// <summary>
        /// 改变特殊序列名的脉冲步进长度
        /// </summary>
        public void ChangeWaveSegStep(string name, int stepvalue)
        {
            foreach (var ch in Channels)
            {
                foreach (var wave in ch.Peaks)
                {
                    if (wave.PeakName == name)
                    {
                        wave.Step = stepvalue;
                    }
                }
            }
        }
    }
}
