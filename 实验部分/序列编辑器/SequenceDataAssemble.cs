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
    public class SequenceDataAssemble
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
                var peaktriggers = obj.ExtractString("ChannelName→" + names[i] + "→" + "IsTrigger").Select(x => bool.Parse(x)).ToList();

                SequenceChannelData channeldata = new SequenceChannelData((SequenceChannel)Enum.Parse(typeof(SequenceChannel), names[i]));

                for (int j = 0; j < peaknames.Count; j++)
                {
                    channeldata.Peaks.Add(new SequenceWaveSeg(peaknames[j], peakspans[j], (WaveValues)Enum.Parse(typeof(WaveValues), peakvalues[j]), channeldata, peaktriggers[j]));
                }

                assem.Channels.Add(channeldata);
            }
            return assem;
        }

        /// <summary>
        /// 写文件,出错则报错
        /// </summary>
        /// <param name="obj"></param>
        public void WriteToFile()
        {
            //检查序列文件
            CheckCommandFormat();
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
                var ispeaktrigger = item.Peaks.Select(x => x.IsTriggerCommand.ToString()).ToList();
                var peakspans = item.Peaks.Select(x => x.PeakSpan.ToString()).ToList();
                obj.WriteStringData("ChannelName→" + chname + "→" + "PeakNames", peaknames);
                obj.WriteStringData("ChannelName→" + chname + "→" + "PeakValues", peakvalues);
                obj.WriteStringData("ChannelName→" + chname + "→" + "Spans", peakspans);
                obj.WriteStringData("ChannelName→" + chname + "→" + "IsTrigger", ispeaktrigger);
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

            InputCommand.Add(new LoopStartCommandLine(LoopCount - 1));
            int loopstartind = InputCommand.Count - 1;

            //找1脉冲的时间点
            HashSet<int> OneTimes = new HashSet<int>();
            foreach (var wave in Channels)
            {
                int time = 0;
                foreach (var peak in wave.Peaks)
                {
                    if (peak.WaveValue == WaveValues.One && peak.PeakSpan != 0)
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
        /// 检查格式,出错则报错
        /// </summary>
        /// <returns></returns>
        public void CheckCommandFormat()
        {
            List<int> starttimes = new List<int>();
            List<int> endtimes = new List<int>();
            //Trigger指令必须搭配TriggerWait指令使用
            foreach (var item in Channels)
            {
                var trigs = item.Peaks.Where(x => x.IsTriggerCommand).Select(x => item.Peaks.IndexOf(x) - 1);
                foreach (var ind in trigs)
                {
                    if (ind == -1 || item.Peaks[ind].PeakName != "TriggerWait")
                    {
                        throw new Exception("Trigger指令必须搭配TriggerWait指令使用,TriggerWait指令必须加在Trigger指令前");
                    }
                }
            }
            foreach (var item in Channels)
            {
                starttimes.AddRange(item.Peaks.Where(x => x.IsTriggerCommand).Select(x =>
                {
                    item.GetSegTime(x, out int start, out int end);
                    return start;
                }));
                endtimes.AddRange(item.Peaks.Where(x => x.IsTriggerCommand).Select(x =>
                {
                    item.GetSegTime(x, out int start, out int end);
                    return end;
                }));
            }
            HashSet<int> sortedstarttimes = new HashSet<int>(starttimes);
            HashSet<int> sortedendtimes = new HashSet<int>(endtimes);
            if (sortedendtimes.Count * Channels.Count != starttimes.Count || sortedendtimes.Count * Channels.Count != endtimes.Count)
            {
                throw new Exception("Trigger指令在每个通道中必须具有相同的起始时间和终止时间");
            }
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
    }
}
