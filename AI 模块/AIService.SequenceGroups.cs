using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ODMR_Lab.实验部分.序列编辑器;
using CodeHelper;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 脉冲组合管理
    /// 安全设计：
    /// - 用户序列组（原始 .userdat 文件）只读，不可修改或删除
    /// - AI 生成的序列组可以添加、修改和删除，通过 AI_SequenceGroups.json 清单追踪
    /// - AI 序列组文件名以 "AI_" 前缀标记，同时在清单中记录元数据
    /// </summary>
    public partial class AIService
    {
        #region 脉冲组合管理

        // AI 序列组清单文件路径
        private static string AISequenceGroupManifestPath
        {
            get { return Path.Combine(Environment.CurrentDirectory, "SequenceGroup", "AI_SequenceGroups.json"); }
        }

        /// <summary>
        /// 读取 AI 序列组清单
        /// </summary>
        private List<AISequenceGroupEntry> LoadAISequenceGroupManifest()
        {
            var path = AISequenceGroupManifestPath;
            if (!File.Exists(path))
                return new List<AISequenceGroupEntry>();
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<AISequenceGroupEntry>>(json);
                return list ?? new List<AISequenceGroupEntry>();
            }
            catch
            {
                return new List<AISequenceGroupEntry>();
            }
        }

        /// <summary>
        /// 保存 AI 序列组清单
        /// </summary>
        private void SaveAISequenceGroupManifest(List<AISequenceGroupEntry> entries)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(entries,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AISequenceGroupManifestPath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 获取 SequenceGroup 目录下所有序列组文件
        /// </summary>
        private List<string> GetAllSequenceGroupFiles()
        {
            var groupDir = Path.Combine(Environment.CurrentDirectory, "SequenceGroup");
            if (!Directory.Exists(groupDir))
                return new List<string>();
            return Directory.GetFiles(groupDir, "*.userdat", SearchOption.AllDirectories)
                .ToList();
        }

        [AiCommand("list-sequence-groups", "列出所有脉冲组合，区分用户序列组和 AI 生成序列组",
            "无参数。返回 name(序列组名)/isAI(是否AI生成)/path(相对路径)/createdAt(创建时间)")]
        private string ListSequenceGroups(Dictionary<string, string> args)
        {
            var aiManifest = LoadAISequenceGroupManifest();
            var aiNames = new HashSet<string>(aiManifest.Select(e => e.GroupName));
            var groupDir = Path.Combine(Environment.CurrentDirectory, "SequenceGroup");
            var allFiles = GetAllSequenceGroupFiles();

            var result = allFiles.Select(f =>
            {
                string relPath = f.Substring(groupDir.Length + 1); // 去掉 SequenceGroup\ 前缀
                string name = Path.GetFileNameWithoutExtension(f);
                bool isAI = aiNames.Contains(name);
                var aiEntry = aiManifest.FirstOrDefault(e => e.GroupName == name);
                return new
                {
                    name,
                    isAI,
                    path = relPath,
                    description = aiEntry != null ? aiEntry.Description : "",
                    createdAt = aiEntry != null ? aiEntry.CreatedAt : "",
                    channelCount = aiEntry != null ? aiEntry.ChannelCount : 0
                };
            }).ToList();

            int userCount = result.Count(x => !x.isAI);
            int aiCount = result.Count(x => x.isAI);

            return Ok(new
            {
                total = result.Count,
                userGroups = userCount,
                aiGroups = aiCount,
                groups = result,
                hint = "用户序列组(isAI=false)只读不可删除；AI序列组(isAI=true)可用 delete-sequence-group 删除"
            });
        }

        [AiCommand("read-sequence-group", "读取脉冲组合的结构和内容（只读）",
            "name=<序列组文件名，不含扩展名>。返回序列组名/各通道的脉冲段详情")]
        private string ReadSequenceGroup(Dictionary<string, string> args)
        {
            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数（序列组文件名，不含 .userdat 后缀）");

            try
            {
                var group = new GroupSequenceWaveSeg();
                group.ReadFromFile(name);

                var channels = new List<object>();
                foreach (var ch in group.GroupCollection)
                {
                    var peaks = new List<object>();
                    int totalTime = 0;
                    
                    foreach (var p in ch.Value)
                    {
                        peaks.Add(new
                        {
                            peakName = p.PeakName,
                            span = p.PeakSpan,
                            waveValue = Enum.GetName(typeof(WaveValues), p.WaveValue),
                            isTrigger = p.IsTriggerCommand
                        });
                        totalTime += p.PeakSpan;
                    }

                    channels.Add(new
                    {
                        channelName = ch.Key,
                        peakCount = peaks.Count,
                        totalTime_ns = totalTime,
                        peaks
                    });
                }

                // 判断是否是 AI 序列组
                var aiManifest = LoadAISequenceGroupManifest();
                bool isAI = aiManifest.Any(e => e.GroupName == group.PeakName);

                return Ok(new
                {
                    name = group.PeakName,
                    isAI,
                    channelCount = group.GroupCollection.Count,
                    channels,
                    hint = "此为只读操作；序列组文件内容不可通过 AI 指令修改"
                });
            }
            catch (Exception ex)
            {
                return Err("读取序列组文件失败：" + ex.Message);
            }
        }

        [AiCommand("create-sequence-group", "创建 AI 生成的脉冲组合文件（直接复制 .userdat 文件到 SequenceGroup 目录）",
            "name=<序列组名，不含扩展名> filePath=<.userdat 文件的完整路径> desc=<描述,可选> confirm=true(安全模式下必需)。AI 助手先用 userdat_tool 生成 .userdat 文件，然后传入路径。序列组名不可与已有文件重名")]
        private string CreateSequenceGroup(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "创建 AI 序列组文件");
            if (block != null) return block;

            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数");

            string filePath = GetArg(args, "filePath");
            if (string.IsNullOrEmpty(filePath))
                return Err("缺少 filePath 参数（.userdat 文件的完整路径）");

            // 检查源文件是否存在
            if (!File.Exists(filePath))
                return Err("指定的 .userdat 文件不存在: " + filePath);

            string desc = GetArg(args, "desc", "");

            // 检查是否重名
            var allFiles = GetAllSequenceGroupFiles();
            if (allFiles.Any(f => Path.GetFileNameWithoutExtension(f) == name))
                return Err("序列组名 '" + name + "' 已存在，不可与已有序列组重名");

            try
            {
                // 目标路径
                var groupDir = Path.Combine(Environment.CurrentDirectory, "SequenceGroup");
                if (!Directory.Exists(groupDir))
                    Directory.CreateDirectory(groupDir);
                var destPath = Path.Combine(groupDir, name + ".userdat");

                // 复制文件
                File.Copy(filePath, destPath, true);

                // 尝试读取文件获取通道数（用于清单记录）
                int channelCount = 0;
                try
                {
                    var group = new GroupSequenceWaveSeg();
                    group.ReadFromFile(name);
                    channelCount = group.GroupCollection.Count;
                }
                catch
                {
                    // 读取失败不影响创建，只是清单中通道数为 0
                }

                // 更新 AI 清单
                var aiManifest = LoadAISequenceGroupManifest();
                aiManifest.Add(new AISequenceGroupEntry
                {
                    GroupName = name,
                    Description = desc,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ChannelCount = channelCount
                });
                SaveAISequenceGroupManifest(aiManifest);

                Log("创建 AI 序列组文件: " + name + " (从 " + filePath + ")", LogLevel.Info);
                return Ok(new
                {
                    name,
                    sourceFile = filePath,
                    destPath = Path.Combine("SequenceGroup", name + ".userdat"),
                    channels = channelCount,
                    description = desc,
                    hint = "AI 序列组已创建并注册到清单，可用 delete-sequence-group 删除"
                });
            }
            catch (Exception ex)
            {
                return Err("创建序列组文件失败：" + ex.Message);
            }
        }

        [AiCommand("delete-sequence-group", "删除 AI 生成的脉冲组合文件（仅限 AI 序列组，用户序列组不可删除）",
            "name=<序列组名，不含扩展名> confirm=true(安全模式下必需)。仅可删除 AI 生成的序列组")]
        private string DeleteSequenceGroup(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "删除 AI 序列组文件");
            if (block != null) return block;

            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数");

            // 检查是否是 AI 序列组
            var aiManifest = LoadAISequenceGroupManifest();
            var aiEntry = aiManifest.FirstOrDefault(e => e.GroupName == name);
            if (aiEntry == null)
                return Err("'" + name + "' 不是 AI 生成的序列组，用户序列组不可删除");

            // 查找文件
            var groupDir = Path.Combine(Environment.CurrentDirectory, "SequenceGroup");
            var files = Directory.GetFiles(groupDir, name + ".userdat", SearchOption.AllDirectories);
            if (files.Length == 0)
                return Err("找不到序列组文件: " + name);

            try
            {
                // 删除文件
                File.Delete(files[0]);

                // 从清单中移除
                aiManifest.Remove(aiEntry);
                SaveAISequenceGroupManifest(aiManifest);

                Log("删除 AI 序列组文件: " + name, LogLevel.Info);
                return Ok(new
                {
                    name,
                    deletedFile = files[0].Substring(Environment.CurrentDirectory.Length + 1),
                    hint = "AI 序列组已删除"
                });
            }
            catch (Exception ex)
            {
                return Err("删除序列组文件失败：" + ex.Message);
            }
        }

        [AiCommand("update-sequence-group", "更新 AI 生成的脉冲组合文件（仅限 AI 序列组，直接复制 .userdat 文件）",
            "name=<序列组名，不含扩展名> filePath=<.userdat 文件的完整路径> confirm=true(安全模式下必需)。仅可更新 AI 生成的序列组")]
        private string UpdateSequenceGroup(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "更新 AI 序列组文件");
            if (block != null) return block;

            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name))
                return Err("缺少 name 参数");

            string filePath = GetArg(args, "filePath");
            if (string.IsNullOrEmpty(filePath))
                return Err("缺少 filePath 参数（.userdat 文件的完整路径）");

            // 检查源文件是否存在
            if (!File.Exists(filePath))
                return Err("指定的 .userdat 文件不存在: " + filePath);

            // 检查是否是 AI 序列组
            var aiManifest = LoadAISequenceGroupManifest();
            var aiEntry = aiManifest.FirstOrDefault(e => e.GroupName == name);
            if (aiEntry == null)
                return Err("'" + name + "' 不是 AI 生成的序列组，用户序列组不可修改");

            try
            {
                // 目标路径
                var groupDir = Path.Combine(Environment.CurrentDirectory, "SequenceGroup");
                var destPath = Path.Combine(groupDir, name + ".userdat");

                // 复制文件（覆盖原文件）
                File.Copy(filePath, destPath, true);

                // 更新清单中的元数据
                int channelCount = 0;
                try
                {
                    var group = new GroupSequenceWaveSeg();
                    group.ReadFromFile(name);
                    channelCount = group.GroupCollection.Count;
                    aiEntry.ChannelCount = channelCount;
                }
                catch
                {
                    // 读取失败不影响更新
                }
                SaveAISequenceGroupManifest(aiManifest);

                Log("更新 AI 序列组文件: " + name + " (从 " + filePath + ")", LogLevel.Info);
                return Ok(new
                {
                    name,
                    sourceFile = filePath,
                    destPath = Path.Combine("SequenceGroup", name + ".userdat"),
                    channels = channelCount,
                    hint = "AI 序列组已更新"
                });
            }
            catch (Exception ex)
            {
                return Err("更新序列组文件失败：" + ex.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// AI 序列组清单条目
    /// </summary>
    public class AISequenceGroupEntry
    {
        public string GroupName { get; set; } = "";
        public string Description { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public int ChannelCount { get; set; } = 0;
    }
}
