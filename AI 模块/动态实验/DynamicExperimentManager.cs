using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ODMR_Lab;
using ODMR_Lab.ODMR实验;

namespace ODMRLab.AI.DynamicExperiments
{
    /// <summary>
    /// 动态实验管理器 - 管理AI实验的创建、编译、审核、加载
    /// </summary>
    public class DynamicExperimentManager
    {
        private static DynamicExperimentManager _instance;
        public static DynamicExperimentManager Instance => _instance ?? (_instance = new DynamicExperimentManager());

        /// <summary>
        /// 存储根目录
        /// </summary>
        public string StorageBasePath { get; private set; }
        
        /// <summary>
        /// 待审核目录
        /// </summary>
        public string PendingPath => Path.Combine(StorageBasePath, "pending");
        
        /// <summary>
        /// 已批准目录
        /// </summary>
        public string ApprovedPath => Path.Combine(StorageBasePath, "approved");
        
        /// <summary>
        /// 已拒绝目录
        /// </summary>
        public string RejectedPath => Path.Combine(StorageBasePath, "rejected");
        
        /// <summary>
        /// 清单文件路径
        /// </summary>
        public string ManifestPath => Path.Combine(StorageBasePath, "manifest.json");

        /// <summary>
        /// 实验清单
        /// </summary>
        public List<AIExperimentInfo> Experiments { get; private set; } = new List<AIExperimentInfo>();

        /// <summary>
        /// 已加载的动态实验实例
        /// </summary>
        private Dictionary<string, ODMRExpObject> _loadedExperiments = new Dictionary<string, ODMRExpObject>();

        /// <summary>
        /// 实验状态变更事件
        /// </summary>
        public event Action<string> ExperimentStatusChanged;

        private DynamicExperimentManager()
        {
            // 初始化存储路径
            StorageBasePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Experiments", "AI"
            );
            
            EnsureDirectoriesExist();
            LoadManifest();
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(StorageBasePath);
            Directory.CreateDirectory(PendingPath);
            Directory.CreateDirectory(ApprovedPath);
            Directory.CreateDirectory(RejectedPath);
        }

        /// <summary>
        /// 加载清单文件
        /// </summary>
        private void LoadManifest()
        {
            try
            {
                if (File.Exists(ManifestPath))
                {
                    var json = File.ReadAllText(ManifestPath, Encoding.UTF8);
                    var manifest = JsonSerializer.Deserialize<ManifestData>(json);
                    if (manifest?.Experiments != null)
                    {
                        Experiments = manifest.Experiments;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageLogger.LogError($"加载实验清单失败: {ex.Message}");
                Experiments = new List<AIExperimentInfo>();
            }
        }

        /// <summary>
        /// 保存清单文件（内部方法）
        /// </summary>
        internal void SaveManifestInternal()
        {
            SaveManifest();
        }

        /// <summary>
        /// 保存清单文件
        /// </summary>
        private void SaveManifest()
        {
            try
            {
                var manifest = new ManifestData { Experiments = Experiments };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(manifest, options);
                File.WriteAllText(ManifestPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageLogger.LogError($"保存实验清单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建新实验（待审核状态）
        /// </summary>
        public async Task<AIExperimentInfo> CreateExperimentAsync(string name, string sourceCode, string description = "")
        {
            // 安全验证
            var violations = DynamicExperimentCompiler.ValidateSourceCode(sourceCode);
            if (violations.Count > 0)
            {
                throw new Exception("代码安全验证失败:\n" + string.Join("\n", violations));
            }

            // 生成唯一ID
            var id = Guid.NewGuid().ToString("N").Substring(0, 8);
            var fileName = $"exp_{id}";

            var expInfo = new AIExperimentInfo
            {
                Id = id,
                Name = name,
                Description = description,
                SourceCode = sourceCode,
                ClassName = ExtractClassName(sourceCode),
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                Status = ReviewStatus.Pending,
                SourceFilePath = Path.Combine(PendingPath, $"{fileName}.cs"),
                AssemblyFilePath = Path.Combine(PendingPath, $"{fileName}.dll"),
                IsCompiled = false
            };

            // 保存源代码
            File.WriteAllText(expInfo.SourceFilePath, sourceCode, Encoding.UTF8);

            // 编译
            var compileResult = DynamicExperimentCompiler.Compile(sourceCode, expInfo.AssemblyFilePath);
            if (compileResult.Success)
            {
                expInfo.IsCompiled = true;
                expInfo.ClassName = compileResult.ClassName;
            }
            else
            {
                expInfo.CompileErrors = string.Join("\n", compileResult.Errors);
            }

            // 添加到清单
            Experiments.Add(expInfo);
            SaveManifest();

            ExperimentStatusChanged?.Invoke($"创建实验: {name} (ID: {id})");

            return expInfo;
        }

        /// <summary>
        /// 审核实验
        /// </summary>
        public async Task<bool> ReviewExperimentAsync(string id, bool approved, string comment = "")
        {
            var exp = Experiments.FirstOrDefault(e => e.Id == id);
            if (exp == null)
            {
                throw new Exception($"找不到实验: {id}");
            }

            if (exp.Status == ReviewStatus.Approved)
            {
                throw new Exception("实验已批准，无需重复审核");
            }

            exp.Status = approved ? ReviewStatus.Approved : ReviewStatus.Rejected;
            exp.ReviewComment = comment;
            exp.ReviewedAt = DateTime.Now;

            // 移动文件
            var targetDir = approved ? ApprovedPath : RejectedPath;
            var fileName = Path.GetFileNameWithoutExtension(exp.SourceFilePath);

            try
            {
                // 移动源代码
                var newSourcePath = Path.Combine(targetDir, $"{fileName}.cs");
                if (File.Exists(exp.SourceFilePath))
                {
                    File.Move(exp.SourceFilePath, newSourcePath);
                    exp.SourceFilePath = newSourcePath;
                }

                // 移动编译产物
                if (exp.IsCompiled && File.Exists(exp.AssemblyFilePath))
                {
                    var newAssemblyPath = Path.Combine(targetDir, $"{fileName}.dll");
                    File.Move(exp.AssemblyFilePath, newAssemblyPath);
                    exp.AssemblyFilePath = newAssemblyPath;

                    // 移动PDB
                    var oldPdb = Path.ChangeExtension(exp.AssemblyFilePath, ".pdb");
                    var newPdb = Path.ChangeExtension(newAssemblyPath, ".pdb");
                    if (File.Exists(oldPdb))
                    {
                        File.Move(oldPdb, newPdb);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageLogger.LogError($"移动实验文件失败: {ex.Message}");
            }

            SaveManifest();
            ExperimentStatusChanged?.Invoke($"审核实验: {exp.Name} - {(approved ? "批准" : "拒绝")}");

            return true;
        }

        /// <summary>
        /// 删除实验
        /// </summary>
        public async Task<bool> DeleteExperimentAsync(string id)
        {
            var exp = Experiments.FirstOrDefault(e => e.Id == id);
            if (exp == null)
            {
                throw new Exception($"找不到实验: {id}");
            }

            // 删除文件
            try
            {
                if (File.Exists(exp.SourceFilePath))
                    File.Delete(exp.SourceFilePath);
                if (File.Exists(exp.AssemblyFilePath))
                    File.Delete(exp.AssemblyFilePath);
                
                var pdbPath = Path.ChangeExtension(exp.AssemblyFilePath, ".pdb");
                if (File.Exists(pdbPath))
                    File.Delete(pdbPath);
            }
            catch (Exception ex)
            {
                MessageLogger.LogError($"删除实验文件失败: {ex.Message}");
            }

            // 从清单移除
            Experiments.Remove(exp);
            SaveManifest();

            // 从加载列表移除
            if (_loadedExperiments.ContainsKey(id))
            {
                _loadedExperiments.Remove(id);
            }

            ExperimentStatusChanged?.Invoke($"删除实验: {exp.Name}");

            return true;
        }

        /// <summary>
        /// 加载所有已批准的实验
        /// </summary>
        public List<ODMRExpObject> LoadAllApprovedExperiments()
        {
            var result = new List<ODMRExpObject>();
            var approvedExps = Experiments.Where(e => e.Status == ReviewStatus.Approved && e.IsCompiled).ToList();

            foreach (var exp in approvedExps)
            {
                try
                {
                    if (_loadedExperiments.ContainsKey(exp.Id))
                    {
                        result.Add(_loadedExperiments[exp.Id]);
                        continue;
                    }

                    if (!File.Exists(exp.AssemblyFilePath))
                    {
                        MessageLogger.LogError($"实验程序集不存在: {exp.AssemblyFilePath}");
                        continue;
                    }

                    var instance = ExperimentLoaderWrapper.CreateExperimentInstance(
                        exp.AssemblyFilePath, 
                        exp.ClassName
                    ) as ODMRExpObject;

                    if (instance != null)
                    {
                        instance.IsDynamicExperiment = true;
                        instance.DynamicExperimentId = exp.Id;
                        _loadedExperiments[exp.Id] = instance;
                        result.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    MessageLogger.LogError($"加载实验 {exp.Name} 失败: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 获取动态实验实例列表
        /// </summary>
        public List<ODMRExpObject> GetDynamicExperiments()
        {
            return _loadedExperiments.Values.ToList();
        }

        /// <summary>
        /// 从源代码提取类名
        /// </summary>
        private string ExtractClassName(string sourceCode)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sourceCode, @"class\s+(\w+)");
            return match.Success ? match.Groups[1].Value : "UnknownClass";
        }

        /// <summary>
        /// 清单数据结构
        /// </summary>
        private class ManifestData
        {
            public List<AIExperimentInfo> Experiments { get; set; } = new List<AIExperimentInfo>();
        }
    }
}
