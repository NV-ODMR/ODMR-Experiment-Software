using CodeHelper;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
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

        public void UpdateGlobalPulsesLength(bool throwexception = false)
        {
            foreach (var item in Channels)
            {
                foreach (var peak in item.Peaks)
                {
                    peak.UpdateGlobalPulse(throwexception);
                }
            }
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
                var peakspans = obj.ExtractString("ChannelName→" + names[i] + "→" + "Spans");
                var peaktriggers = obj.ExtractString("ChannelName→" + names[i] + "→" + "IsTrigger");

                SequenceChannelData channeldata = new SequenceChannelData((SequenceChannel)Enum.Parse(typeof(SequenceChannel), names[i]));

                for (int j = 0; j < peaktriggers.Count; j++)
                {
                    if (peaktriggers[j] == "SequenceGroup")
                    {
                        GroupSequenceWaveSeg seg = new GroupSequenceWaveSeg();
                        seg.ReadFromFile(peaknames[j]);
                        seg.SelectChnnelInd = int.Parse(peakvalues[j]);
                        channeldata.Peaks.Add(seg);
                    }
                    else
                    {
                        channeldata.Peaks.Add(new SingleSequenceWaveSeg(peaknames[j], int.Parse(peakspans[j]), (WaveValues)Enum.Parse(typeof(WaveValues), peakvalues[j]), channeldata, bool.Parse(peaktriggers[j])));
                    }
                }

                assem.Channels.Add(channeldata);
            }
            return assem;
        }

        /// <summary>
        /// 写文件,出错则报错
        /// </summary>
        /// <param name="obj"></param>
        public void WriteToFile(string path = "")
        {
            //设置全局参数
            foreach (var item in GlobalPulseParams.GlobalPulseConfigs)
            {
                ChangeWaveSegSpan(item.PulseName, item.PulseLength);
            }
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
                var peaknames = item.Peaks.Select(x =>
                {
                    if (x is SingleSequenceWaveSeg)
                    {
                        return (x as SingleSequenceWaveSeg).PeakName;
                    }
                    if (x is GroupSequenceWaveSeg)
                    {
                        return (x as GroupSequenceWaveSeg).PeakName;
                    }
                    return "";
                }).ToList();
                var peakvalues = item.Peaks.Select(x =>
                {
                    if (x is SingleSequenceWaveSeg)
                    {
                        return Enum.GetName(typeof(WaveValues), (x as SingleSequenceWaveSeg).WaveValue);
                    }
                    else
                    {
                        return (x as GroupSequenceWaveSeg).SelectChnnelInd.ToString();
                    }
                }).ToList();
                var ispeaktrigger = item.Peaks.Select(x =>
                {
                    if (x is SingleSequenceWaveSeg)
                    {
                        return (x as SingleSequenceWaveSeg).IsTriggerCommand.ToString();
                    }
                    else
                    {
                        return "SequenceGroup";
                    }
                }).ToList();
                var peakspans = item.Peaks.Select(x =>
                {
                    if (x is SingleSequenceWaveSeg)
                    {
                        return (x as SingleSequenceWaveSeg).PeakSpan.ToString();
                    }
                    else
                    {
                        return "SequenceGroup";
                    }
                }).ToList();

                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "PeakNames"), peaknames);
                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "PeakValues"), peakvalues);
                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "Spans"), peakspans);
                obj.WriteStringData(FileHelper.Combine("→", "ChannelName", chname, "IsTrigger"), ispeaktrigger);
            }

            //保存到目标文件夹
            if (path == "")
            {
                DirectoryInfo info = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequences"));
                obj.SaveToFile(Path.Combine(info.FullName, Name.ToString()));
            }
            else
            {
                obj.SaveToFile(path);
            }
        }


        /// <summary>
        /// 从文件中读取序列，读不到则报错
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static SequenceDataAssemble ReadFromSequenceName(string name)
        {
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Sequences")))
            {
                throw new Exception("找不到序列文件:" + name);
            }
            var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Sequences"), "*", SearchOption.AllDirectories);
            var selectfile = files.Where((x) => Path.GetFileNameWithoutExtension(x) == name);
            if (selectfile.Count() == 0)
            {
                throw new Exception("找不到序列文件:" + name);
            }
            var obj = FileObject.ReadFromFile(selectfile.ElementAt(0));
            return ReadFromFile(obj);
        }

        /// <summary>
        /// 转换成PB指令
        /// </summary>
        /// <returns></returns>
        public List<CommandBase> AddToCommandLine(List<CommandBase> InputCommand, out string ComandInformation)
        {
            ComandInformation = "";
            //判断循环次数,如果次数大于1000000则嵌套
            bool multiloop = false;
            int multiloopind = 0;
            int outerloop = LoopCount / 1000000;
            if (LoopCount >= 1000000)
            {
                multiloop = true;
                InputCommand.Add(new LoopStartCommandLine(outerloop));
                multiloopind = InputCommand.Count - 1;
            }
            if (multiloop == true)
            {
                InputCommand.Add(new LoopStartCommandLine(1000000));
                int loopstartind = InputCommand.Count - 1;

                InputCommand = AddSubCommandLine(InputCommand, out ComandInformation);

                InputCommand.Add(new LoopEndCommandLine(loopstartind));
                InputCommand.Add(new LoopEndCommandLine(multiloopind));
            }
            if (LoopCount % 1000000 != 0)
            {
                InputCommand.Add(new LoopStartCommandLine(LoopCount % 1000000));
                int loopind = InputCommand.Count - 1;

                InputCommand = AddSubCommandLine(InputCommand, out ComandInformation);

                InputCommand.Add(new LoopEndCommandLine(loopind));
            }

            return InputCommand;
        }

        private List<CommandBase> AddSubCommandLine(List<CommandBase> InputCommand, out string ComandInformation)
        {
            ComandInformation = "";
            //找1脉冲以及Trigger指令的时间点
            HashSet<int> OneTimes = new HashSet<int>() { 0 };
            SequenceDataAssemble newassem = new SequenceDataAssemble();
            newassem.Channels = Channels.Select(x => new SequenceChannelData(x.ChannelInd) { Peaks = SequenceChannelData.GetExpandedPeakArray(x.Peaks).Select(y => y as SequenceSegBase).ToList() }).ToList();
            foreach (var wave in newassem.Channels)
            {
                int time = 0;
                var peaks = wave.Peaks;
                foreach (var peak in peaks)
                {
                    if (peak.IsWaveOne() || peak.IsTrigger())
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
                if (newassem.IsTrigger(sortedTimes[j], sortedTimes[j + 1]))
                {
                    TriggerLine trigger = new TriggerLine();
                    ComandInformation += "Trigger\n";
                    InputCommand.Add(trigger);
                }
                else
                {
                    List<int> HighChannelIndexes = new List<int>();
                    foreach (var ch in newassem.Channels)
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
            }
            return InputCommand;
        }

        /// <summary>
        /// 检查格式,出错则报错
        /// </summary>
        /// <returns></returns>
        public void CheckCommandFormat()
        {
            CheckChannelFormat();
            List<int> starttimes = new List<int>();
            List<int> endtimes = new List<int>();
            //Trigger指令必须搭配TriggerWait指令使用
            foreach (var item in Channels)
            {
                var trigs = item.Peaks.Where(x => x.IsTrigger()).Select(x => item.Peaks.IndexOf(x) - 1);
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
                starttimes.AddRange(item.Peaks.Where(x => x.IsTrigger()).Select(x =>
                {
                    item.GetSegTime(x, out int start, out int end);
                    return start;
                }));
                endtimes.AddRange(item.Peaks.Where(x => x.IsTrigger()).Select(x =>
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
        /// 检查通道格式，脉冲组合的起始时间必须相同,同时通道总时间必须相同,,出错则报错
        /// </summary>
        public void CheckChannelFormat()
        {
            var groups = Channels.Select((x) => x.Peaks.Where(s => s is GroupSequenceWaveSeg).ToList()).ToList();
            var counthashset = groups.Select(x => x.Count).ToHashSet();
            if (counthashset.Count > 1) throw new Exception("各个通道的脉冲组合数不相同");
            int count = counthashset.ElementAt(0);
            for (int i = 0; i < count; i++)
            {
                var indexedgroups = groups.Select((x) => x.ElementAt(i));
                var segsbeforegroup = indexedgroups.Select((x, ind) => Channels[ind].Peaks.GetRange(0, Channels[ind].Peaks.IndexOf(x)));
                var totaltimes = segsbeforegroup.Select((x) => x.Count == 0 ? 0 : x.Sum(p => p.PeakSpan)).ToHashSet();
                if (totaltimes.Count > 1) throw new Exception("同一脉冲组合各个通道的起始时间必须相同");
            }
            var times = Channels.Select(x => SequenceChannelData.GetExpandedPeakArray(x.Peaks).Sum(y => y.PeakSpan)).ToHashSet();
            if (times.Count > 1) throw new Exception("各个通道的总时间必须相同");
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
        /// 指定时间段是否是Trigger指令
        /// </summary>
        /// <returns></returns>
        public bool IsTrigger(int starttime, int endtime)
        {
            bool res = false;
            foreach (var item in Channels)
            {
                try
                {
                    res |= item.GetSegFromTime(starttime, endtime)[0].IsTrigger();
                }
                catch (Exception)
                {
                    res |= false;
                }
            }
            return res;
        }
    }
}
