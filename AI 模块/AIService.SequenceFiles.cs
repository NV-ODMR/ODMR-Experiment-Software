using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ODMR_Lab.实验部分.序列编辑器;
using CodeHelper;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 序列文件管理
    /// 安全设计：
    /// - 用户序列（原始 .userdat 文件）只读，不可修改或删除
    /// - AI 生成的序列可以添加和删除，通过 AI_Sequences.json 清单追踪
    /// - AI 序列文件名以 "AI_" 前缀标记，同时在清单中记录元数据
    /// </summary>
    public partial class AIService
    {
        #region 序列文件管理

        // AI 序列清单文件路径
        private static string AISequenceManifestPath
        {
            get { return Path.Combine(Environment.CurrentDirectory, "Sequences", "AI_Sequences.json"); }
        }

        /// <summary>
        /// 读取 AI 序列清单
        /// </summary>
        private List<AISequenceEntry> LoadAISequenceManifest()
        {
            var path = AISequenceManifestPath;
            if (!File.Exists(path))
                return new List<AISequenceEntry>();
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<AISequenceEntry>>(json);
                return list ?? new List<AISequenceEntry>();
            }
            catch
            {
                return new List<AISequenceEntry>();
            }
        }

        /// <summary>
        /// 保存 AI 序列清单
        /// </summary>
        private void SaveAISequenceManifest(List<AISequenceEntry> entries)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(entries,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AISequenceManifestPath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 获取 Sequences 目录下所有序列文件（递归）
        /// </summary>
        private List<string> GetAllSequenceFiles()
        {
            var seqDir = Path.Combine(Environment.CurrentDirectory, "Sequences");
            if (!Directory.Exists(seqDir))
                return new List<string>();
            return Directory.GetFiles(seqDir, "*.userdat", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f) != "GlobalPulses.userdat") // 排除全局脉冲表
                .ToList();
        }

        [AiCommand("list-sequence-files", "列出所有序列文件，区分用户序列和 AI 生成序列",
            "无参数。返回 name(序列名)/isAI(是否AI生成)/path(相对路径)/createdAt(创建时间)")]
        private string ListSequenceFiles(Dictionary<string, string> args)
        {
            var aiManifest = LoadAISequenceManifest();
            var aiNames = new HashSet<string>(aiManifest.Select(e => e.SequenceName));
            var seqDir = Path.Combine(Environment.CurrentDirectory, "Sequences");
            var allFiles = GetAllSequenceFiles();

            var result = allFiles.Select(f =>
            {
                string relPath = f.Substring(seqDir.Length + 1); // 去掉 Sequences\ 前缀
                string name = Path.GetFileNameWithoutExtension(f);
                bool isAI = aiNames.Contains(name);
                var aiEntry = aiManifest.FirstOrDefault(e => e.SequenceName == name);
                return new
                {
                    name,
                    isAI,
                    path = relPath,
                    description = aiEntry != null ? aiEntry.Description : "",
                    createdAt = aiEntry != null ? aiEntry.CreatedAt : "",
                    channels = aiEntry != null ? aiEntry.ChannelCount : 0
                };
            }).ToList();

            int userCount = result.Count(x => !x.isAI);
            int aiCount = result.Count(x => x.isAI);

            return Ok(new
            {
                total = result.Count,
                userSequences = userCount,
                aiSequences = aiCount,
                sequences = result,
                hint = "用户序列(isAI=false)只读不可删除；AI序列(isAI=true)可用 delete-sequence-file 删除"
            });
        }

        [AiCommand("read-sequence-file", "读取序列文件的结构和内容（只读）",
            "name=<序列文件名，不含扩展名>。返回序列名/循环次数/各通道的脉冲段详情")]
        private string ReadSequenceFile(Dictionary<string, string> args)
        {
            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数（序列文件名，不含 .userdat 后缀）");

            try
            {
                var assem = SequenceDataAssemble.ReadFromSequenceName(name);

                var channels = new List<object>();
                foreach (var ch in assem.Channels)
                {
                    string chName = Enum.GetName(typeof(SequenceChannel), ch.ChannelInd);
                    var peaks = new List<object>();
                    foreach (var p in ch.Peaks)
                    {
                        if (p is SingleSequenceWaveSeg)
                        {
                            var single = (SingleSequenceWaveSeg)p;
                            peaks.Add(new
                            {
                                type = "single",
                                peakName = single.PeakName,
                                span = single.PeakSpan,
                                waveValue = Enum.GetName(typeof(WaveValues), single.WaveValue),
                                isTrigger = single.IsTriggerCommand
                            });
                        }
                        else if (p is GroupSequenceWaveSeg)
                        {
                            var group = (GroupSequenceWaveSeg)p;
                            peaks.Add(new
                            {
                                type = "group",
                                peakName = group.PeakName,
                                selectChannelInd = group.SelectChnnelInd
                            });
                        }
                    }

                    int totalTime = ch.Peaks.Sum(p => p.PeakSpan);

                    channels.Add(new
                    {
                        channel = chName,
                        peakCount = peaks.Count,
                        totalTime_ns = totalTime,
                        peaks
                    });
                }

                // 计算序列总时间
                int seqTotalTime = assem.Channels.Count > 0
                    ? assem.Channels[0].Peaks.Sum(p => p.PeakSpan)
                    : 0;

                // 判断是否是 AI 序列
                var aiManifest = LoadAISequenceManifest();
                bool isAI = aiManifest.Any(e => e.SequenceName == assem.Name);

                return Ok(new
                {
                    name = assem.Name,
                    isAI,
                    loopCount = assem.LoopCount,
                    channelCount = assem.Channels.Count,
                    totalTime_ns = seqTotalTime,
                    channels,
                    hint = "此为只读操作；序列文件内容不可通过 AI 指令修改"
                });
            }
            catch (Exception ex)
            {
                return Err("读取序列文件失败：" + ex.Message);
            }
        }

        [AiCommand("create-sequence-file", "创建 AI 生成的序列文件（保存到 Sequences 目录）",
            "name=<序列名,不含扩展名> content=<JSON格式的序列内容> desc=<描述,可选> confirm=true(安全模式下必需)。序列名不可与已有文件重名")]
        private string CreateSequenceFile(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "创建 AI 序列文件");
            if (block != null) return block;

            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数");

            string content = GetArg(args, "content");
            if (string.IsNullOrEmpty(content))
                return Err("缺少 content 参数（JSON 格式的序列内容）");

            string desc = GetArg(args, "desc", "");

            // 检查是否重名
            var allFiles = GetAllSequenceFiles();
            if (allFiles.Any(f => Path.GetFileNameWithoutExtension(f) == name))
                return Err("序列名 '" + name + "' 已存在，不可与已有序列重名");

            try
            {
                // 解析 JSON 内容
                var seqData = System.Text.Json.JsonSerializer.Deserialize<AISequenceContent>(content);
                if (seqData == null || seqData.Channels == null || seqData.Channels.Count == 0)
                    return Err("序列内容解析失败或通道为空");

                // 构建 SequenceDataAssemble
                var assem = new SequenceDataAssemble();
                assem.Name = name;
                assem.LoopCount = seqData.LoopCount > 0 ? seqData.LoopCount : 1;

                foreach (var chData in seqData.Channels)
                {
                    SequenceChannel chInd;
                    if (!Enum.TryParse(chData.Channel, out chInd))
                        return Err("无效通道名: " + chData.Channel + "。可用: Ch_2, Ch_3, Ch_4, Ch_5");

                    var channelData = new SequenceChannelData(chInd);

                    foreach (var peakData in chData.Peaks)
                    {
                        if (peakData.Type == "group")
                        {
                            var group = new GroupSequenceWaveSeg();
                            group.PeakName = peakData.PeakName;
                            group.SelectChnnelInd = peakData.SelectChannelInd;
                            channelData.Peaks.Add(group);
                        }
                        else
                        {
                            WaveValues waveVal;
                            if (!Enum.TryParse(peakData.WaveValue, out waveVal))
                                return Err("无效波形值: " + peakData.WaveValue);

                            var seg = new SingleSequenceWaveSeg(
                                peakData.PeakName,
                                peakData.Span,
                                waveVal,
                                channelData,
                                peakData.IsTrigger
                            );
                            channelData.Peaks.Add(seg);
                        }
                    }

                    assem.Channels.Add(channelData);
                }

                // 检查格式
                assem.CheckCommandFormat();

                // 保存文件
                assem.WriteToFile();

                // 更新 AI 清单
                var aiManifest = LoadAISequenceManifest();
                aiManifest.Add(new AISequenceEntry
                {
                    SequenceName = name,
                    Description = desc,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ChannelCount = assem.Channels.Count
                });
                SaveAISequenceManifest(aiManifest);

                Log("创建 AI 序列文件: " + name, LogLevel.Info);
                return Ok(new
                {
                    name,
                    path = Path.Combine("Sequences", name + ".userdat"),
                    channels = assem.Channels.Count,
                    loopCount = assem.LoopCount,
                    description = desc,
                    hint = "AI 序列已创建并注册到清单，可用 delete-sequence-file 删除"
                });
            }
            catch (Exception ex)
            {
                return Err("创建序列文件失败：" + ex.Message);
            }
        }

        [AiCommand("delete-sequence-file", "删除 AI 生成的序列文件（仅限 AI 序列，用户序列不可删除）",
            "name=<序列名,不含扩展名> confirm=true(安全模式下必需)。仅可删除 AI 生成的序列")]
        private string DeleteSequenceFile(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "删除 AI 序列文件");
            if (block != null) return block;

            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数");

            // 检查是否是 AI 序列
            var aiManifest = LoadAISequenceManifest();
            var aiEntry = aiManifest.FirstOrDefault(e => e.SequenceName == name);
            if (aiEntry == null)
                return Err("'" + name + "' 不是 AI 生成的序列，用户序列不可删除");

            // 查找文件
            var seqDir = Path.Combine(Environment.CurrentDirectory, "Sequences");
            var files = Directory.GetFiles(seqDir, name + ".userdat", SearchOption.AllDirectories);
            if (files.Length == 0)
                return Err("找不到序列文件: " + name);

            try
            {
                // 删除文件
                File.Delete(files[0]);

                // 从清单中移除
                aiManifest.Remove(aiEntry);
                SaveAISequenceManifest(aiManifest);

                Log("删除 AI 序列文件: " + name, LogLevel.Info);
                return Ok(new
                {
                    name,
                    deletedFile = files[0].Substring(Environment.CurrentDirectory.Length + 1),
                    hint = "AI 序列已删除"
                });
            }
            catch (Exception ex)
            {
                return Err("删除序列文件失败：" + ex.Message);
            }
        }


        [AiCommand("validate-sequence", "验证序列文件的格式正确性，返回详细的错误信息",
            "name=<序列文件名，不含扩展名>。返回验证结果和所有检测到的错误")]
        private string ValidateSequence(Dictionary<string, string> args)
        {
            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数（序列文件名，不含 .userdat 后缀）");

            var errors = new List<string>();
            var warnings = new List<string>();
            bool success = false;
            SequenceDataAssemble assem = null;

            // 第 1 步：尝试读取序列文件
            try
            {
                assem = SequenceDataAssemble.ReadFromSequenceName(name);
                if (assem == null)
                {
                    errors.Add("序列文件读取失败：返回的对象为空");
                    return Ok(new
                    {
                        name,
                        success = false,
                        errors,
                        warnings,
                        hint = "序列文件无法读取，请检查文件是否存在且格式正确"
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add("读取序列文件失败：" + ex.Message);
                if (ex.InnerException != null)
                    errors.Add("内部异常：" + ex.InnerException.Message);
                
                return Ok(new
                {
                    name,
                    success = false,
                    errors,
                    warnings,
                    hint = "序列文件读取失败，请检查文件是否存在且格式正确"
                });
            }

            // 第 2 步：基本信息验证
            if (string.IsNullOrEmpty(assem.Name))
                warnings.Add("序列名称为空");
            
            if (assem.LoopCount <= 0)
                warnings.Add("循环次数为 " + assem.LoopCount + "，建议设置为正整数");

            if (assem.Channels == null || assem.Channels.Count == 0)
            {
                errors.Add("序列没有包含任何通道");
                return Ok(new
                {
                    name,
                    success = false,
                    errors,
                    warnings,
                    hint = "序列必须至少包含一个通道"
                });
            }

            // 第 3 步：检查每个通道的脉冲段
            foreach (var ch in assem.Channels)
            {
                string chName = Enum.GetName(typeof(SequenceChannel), ch.ChannelInd);
                
                if (ch.Peaks == null || ch.Peaks.Count == 0)
                {
                    errors.Add("通道 " + chName + " 没有任何脉冲段");
                    continue;
                }

                int totalSpan = 0;
                foreach (var peak in ch.Peaks)
                {
                    if (peak.PeakSpan <= 0)
                    {
                        errors.Add("通道 " + chName + " 中的脉冲段 '" + peak.PeakName + "' 的时间长度为 " + peak.PeakSpan + "，必须为正整数");
                    }
                    totalSpan += peak.PeakSpan;

                    // 检查脉冲组合
                    if (peak is GroupSequenceWaveSeg)
                    {
                        var group = (GroupSequenceWaveSeg)peak;
                        if (string.IsNullOrEmpty(group.PeakName))
                        {
                            errors.Add("通道 " + chName + " 中的脉冲组合名称为空");
                        }
                    }
                    else if (peak is SingleSequenceWaveSeg)
                    {
                        var single = (SingleSequenceWaveSeg)peak;
                        if (string.IsNullOrEmpty(single.PeakName))
                        {
                            errors.Add("通道 " + chName + " 中的脉冲段名称为空");
                        }
                    }
                }

                // 记录通道总时间
                warnings.Add("通道 " + chName + " 总时间：" + totalSpan + " ns");
            }

            // 第 4 步：更新全局脉冲长度（可能抛出异常）
            try
            {
                assem.UpdateGlobalPulsesLength(true); // throwexception = true
            }
            catch (Exception ex)
            {
                errors.Add("更新全局脉冲长度失败：" + ex.Message);
                if (ex.InnerException != null)
                    errors.Add("内部异常：" + ex.InnerException.Message);
            }

            // 第 5 步：检查通道格式（CheckChannelFormat）
            try
            {
                assem.CheckChannelFormat();
            }
            catch (Exception ex)
            {
                errors.Add("通道格式检查失败：" + ex.Message);
                if (ex.InnerException != null)
                    errors.Add("内部异常：" + ex.InnerException.Message);
            }

            // 第 6 步：检查命令格式（CheckCommandFormat）
            try
            {
                assem.CheckCommandFormat();
            }
            catch (Exception ex)
            {
                errors.Add("命令格式检查失败：" + ex.Message);
                if (ex.InnerException != null)
                    errors.Add("内部异常：" + ex.InnerException.Message);
            }

            // 第 7 步：尝试转换为 PB 指令（可能暴露更多问题）
            try
            {
                var commands = new List<CommandBase>();
                string commandInfo;
                assem.AddToCommandLine(commands, out commandInfo);
                
                if (commands.Count == 0)
                {
                    warnings.Add("转换后的 PB 指令列表为空");
                }
                else
                {
                    warnings.Add("成功转换为 " + commands.Count + " 条 PB 指令");
                }
            }
            catch (Exception ex)
            {
                errors.Add("转换为 PB 指令失败：" + ex.Message);
                if (ex.InnerException != null)
                    errors.Add("内部异常：" + ex.InnerException.Message);
            }

            // 判断是否成功
            success = errors.Count == 0;

            return Ok(new
            {
                name,
                success,
                errorCount = errors.Count,
                warningCount = warnings.Count,
                errors,
                warnings,
                sequenceInfo = new
                {
                    loopCount = assem.LoopCount,
                    channelCount = assem.Channels.Count,
                    channels = assem.Channels.Select(ch => new
                    {
                        channel = Enum.GetName(typeof(SequenceChannel), ch.ChannelInd),
                        peakCount = ch.Peaks.Count,
                        totalTime_ns = ch.Peaks.Sum(p => p.PeakSpan)
                    }).ToList()
                },
                hint = success 
                    ? "序列格式验证通过，可以正常使用" 
                    : "序列格式验证失败，请修复上述错误后重试"
            });
        }

        #endregion
    }

    /// <summary>
    /// AI 序列清单条目
    /// </summary>
    public class AISequenceEntry
    {
        public string SequenceName { get; set; } = "";
        public string Description { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public int ChannelCount { get; set; } = 0;
    }

    /// <summary>
    /// AI 创建序列时的 JSON 内容格式
    /// </summary>
    public class AISequenceContent
    {
        public int LoopCount { get; set; } = 1;
        public List<AIChannelData> Channels { get; set; } = new List<AIChannelData>();
    }

    public class AIChannelData
    {
        public string Channel { get; set; } = "";
        public List<AIPeakData> Peaks { get; set; } = new List<AIPeakData>();
    }

    public class AIPeakData
    {
        public string Type { get; set; } = "single"; // "single" 或 "group"
        public string PeakName { get; set; } = "";
        public int Span { get; set; } = 0;
        public string WaveValue { get; set; } = "Zero";
        public bool IsTrigger { get; set; } = false;
        public int SelectChannelInd { get; set; } = 0; // 仅 group 类型使用
    }
}
