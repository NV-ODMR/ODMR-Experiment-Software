using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ODMR_Lab;
using ODMR_Lab.ODMR实验;
using ODMRLab.AI.DynamicExperiments;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 动态实验指令扩展
    /// </summary>
    public partial class AIService
    {
        /// <summary>
        /// 创建 AI 动态实验
        /// </summary>
        [AiCommand("create-ai-experiment", "创建新的 AI 动态实验（待审核状态）", "name=实验名称, code=源代码, desc=描述(可选)")]
        private string CreateAiExperiment(Dictionary<string, string> args)
        {
            try
            {
                string name = GetArg(args, "name");
                string code = GetArg(args, "code");
                string description = GetArg(args, "desc", "");
                
                // 如果 code 为空，尝试从 content 读取（POST body）
                if (string.IsNullOrEmpty(code) && args.ContainsKey("content"))
                {
                    code = args["content"];
                }
                
                // 如果仍然为空，尝试从文件读取
                if (string.IsNullOrEmpty(code))
                {
                    string filePath = GetArg(args, "file");
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        code = File.ReadAllText(filePath, Encoding.UTF8);
                    }
                }

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
                {
                    return Err("缺少必要参数: name, code (或 file=源码文件路径，或使用POST body传递)");
                }

                var manager = DynamicExperimentManager.Instance;
                var expInfo = manager.CreateExperimentAsync(name, code, description).Result;

                if (expInfo.IsCompiled)
                {
                    return Ok(new
                    {
                        id = expInfo.Id,
                        name = expInfo.Name,
                        status = expInfo.Status.ToString(),
                        className = expInfo.ClassName,
                        message = $"实验创建成功，等待审核。ID: {expInfo.Id}"
                    });
                }
                else
                {
                    return Err(new
                    {
                        id = expInfo.Id,
                        name = expInfo.Name,
                        status = expInfo.Status.ToString(),
                        message = "实验创建失败，编译错误",
                        errors = expInfo.CompileErrors
                    });
                }
            }
            catch (Exception ex)
            {
                return Err($"创建实验失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 审核 AI 实验
        /// </summary>
        [AiCommand("review-ai-experiment", "审核 AI 动态实验（批准或拒绝）", "id=实验ID, approve=true|false, comment=审核意见(可选)")]
        private string ReviewAiExperiment(Dictionary<string, string> args)
        {
            try
            {
                string id = GetArg(args, "id");
                string approveStr = GetArg(args, "approve");
                string comment = GetArg(args, "comment", "");

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(approveStr))
                {
                    return Err("缺少必要参数: id, approve");
                }

                if (!bool.TryParse(approveStr, out bool approve))
                {
                    return Err("approve 参数必须是 true 或 false");
                }

                var manager = DynamicExperimentManager.Instance;
                manager.ReviewExperimentAsync(id, approve, comment).Wait();

                string status = approve ? "已批准" : "已拒绝";
                return Ok(new
                {
                    id = id,
                    status = status,
                    message = $"实验 {status}"
                });
            }
            catch (Exception ex)
            {
                return Err($"审核实验失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除 AI 实验
        /// </summary>
        [AiCommand("delete-ai-experiment", "删除 AI 动态实验", "id=实验ID")]
        private string DeleteAiExperiment(Dictionary<string, string> args)
        {
            try
            {
                string id = GetArg(args, "id");

                if (string.IsNullOrEmpty(id))
                {
                    return Err("缺少必要参数: id");
                }

                var manager = DynamicExperimentManager.Instance;
                manager.DeleteExperimentAsync(id).Wait();

                return Ok(new
                {
                    id = id,
                    message = "实验已删除"
                });
            }
            catch (Exception ex)
            {
                return Err($"删除实验失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 列出所有 AI 实验
        /// </summary>
        [AiCommand("list-ai-experiments", "列出所有 AI 动态实验", "status=Pending|Approved|Rejected(可选，过滤状态)")]
        private string ListAiExperiments(Dictionary<string, string> args)
        {
            try
            {
                string statusFilter = GetArg(args, "status", "");

                var manager = DynamicExperimentManager.Instance;
                var experiments = manager.Experiments.AsEnumerable();

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    if (Enum.TryParse<ReviewStatus>(statusFilter, true, out var status))
                    {
                        experiments = experiments.Where(e => e.Status == status);
                    }
                }

                var result = experiments.Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    description = e.Description,
                    status = e.Status.ToString(),
                    className = e.ClassName,
                    isCompiled = e.IsCompiled,
                    createdAt = e.CreatedAt,
                    reviewedAt = e.ReviewedAt,
                    reviewComment = e.ReviewComment
                }).ToList();

                return Ok(new
                {
                    count = result.Count,
                    experiments = result
                });
            }
            catch (Exception ex)
            {
                return Err($"列出实验失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取 AI 实验详情
        /// </summary>
        [AiCommand("get-ai-experiment", "获取 AI 动态实验的详细信息", "id=实验ID")]
        private string GetAiExperiment(Dictionary<string, string> args)
        {
            try
            {
                string id = GetArg(args, "id");

                if (string.IsNullOrEmpty(id))
                {
                    return Err("缺少必要参数: id");
                }

                var manager = DynamicExperimentManager.Instance;
                var exp = manager.Experiments.FirstOrDefault(e => e.Id == id);

                if (exp == null)
                {
                    return Err($"找不到实验: {id}");
                }

                return Ok(new
                {
                    id = exp.Id,
                    name = exp.Name,
                    description = exp.Description,
                    status = exp.Status.ToString(),
                    className = exp.ClassName,
                    isCompiled = exp.IsCompiled,
                    sourceCode = exp.SourceCode,
                    compileErrors = exp.CompileErrors,
                    createdAt = exp.CreatedAt,
                    reviewedAt = exp.ReviewedAt,
                    reviewComment = exp.ReviewComment,
                    sourceFilePath = exp.SourceFilePath,
                    assemblyFilePath = exp.AssemblyFilePath
                });
            }
            catch (Exception ex)
            {
                return Err($"获取实验详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新编译 AI 实验
        /// </summary>
        [AiCommand("recompile-ai-experiment", "重新编译 AI 动态实验", "id=实验ID")]
        private string RecompileAiExperiment(Dictionary<string, string> args)
        {
            try
            {
                string id = GetArg(args, "id");

                if (string.IsNullOrEmpty(id))
                {
                    return Err("缺少必要参数: id");
                }

                var manager = DynamicExperimentManager.Instance;
                var exp = manager.Experiments.FirstOrDefault(e => e.Id == id);

                if (exp == null)
                {
                    return Err($"找不到实验: {id}");
                }

                // 重新编译
                var compileResult = DynamicExperimentCompiler.Compile(exp.SourceCode, exp.AssemblyFilePath);
                
                if (compileResult.Success)
                {
                    exp.IsCompiled = true;
                    exp.ClassName = compileResult.ClassName;
                    exp.CompileErrors = null;
                    manager.SaveManifestInternal();

                    return Ok(new
                    {
                        id = exp.Id,
                        name = exp.Name,
                        isCompiled = true,
                        className = exp.ClassName,
                        message = "编译成功"
                    });
                }
                else
                {
                    exp.IsCompiled = false;
                    exp.CompileErrors = string.Join("\n", compileResult.Errors);
                    manager.SaveManifestInternal();

                    return Err(new
                    {
                        id = exp.Id,
                        name = exp.Name,
                        isCompiled = false,
                        message = "编译失败",
                        errors = exp.CompileErrors
                    });
                }
            }
            catch (Exception ex)
            {
                return Err($"重新编译失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载 AI 动态实验到主实验列表
        /// </summary>
        [AiCommand("load-ai-experiment", "加载已批准的 AI 动态实验到 ODMR 实验列表中，使其可被选择和使用", "id=实验ID(可选，不填则加载所有已批准的实验)")]
        private string LoadAiExperiment(Dictionary<string, string> args)
        {
            try
            {
                var win = MainWindow.Handle;
                if (win == null) return Err("主窗口未就绪");

                string id = GetArg(args, "id", "");
                var manager = DynamicExperimentManager.Instance;

                // 检查是否有已批准的实验
                var approvedExps = manager.Experiments.FindAll(e => e.Status == ReviewStatus.Approved && e.IsCompiled);
                if (approvedExps.Count == 0)
                {
                    return Err("没有已批准且编译成功的实验。请先使用 review-ai-experiment 批准实验。");
                }

                // 如果指定了 id，只加载指定实验
                if (!string.IsNullOrEmpty(id))
                {
                    var targetExp = approvedExps.Find(e => e.Id == id);
                    if (targetExp == null)
                    {
                        return Err($"找不到已批准的实验: {id}");
                    }
                    approvedExps = new System.Collections.Generic.List<AIExperimentInfo> { targetExp };
                }

                int loadedCount = 0;
                int skippedCount = 0;
                List<string> loadedNames = new List<string>();

                // 在 UI 线程上执行加载
                win.Dispatcher.Invoke(() =>
                {
                    var page = MainWindow.Exp_SequencePage;
                    if (page == null)
                    {
                        return;
                    }

                    // 加载动态实验实例
                    var instances = manager.LoadAllApprovedExperiments();

                    foreach (var exp in instances)
                    {
                        // 如果指定了 id，只加载匹配的
                        if (!string.IsNullOrEmpty(id))
                        {
                            var info = approvedExps.Find(e => e.ClassName == exp.GetType().Name);
                            if (info == null) continue;
                        }

                        // 检查是否已在列表中（避免重复）
                        bool alreadyExists = false;
                        foreach (var existing in page.ExpObjects)
                        {
                            if (existing.GetType().Name == exp.GetType().Name && 
                                existing.ODMRExperimentName == exp.ODMRExperimentName)
                            {
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (alreadyExists)
                        {
                            skippedCount++;
                            continue;
                        }

                        // 设置父页面并添加到列表
                        exp.ParentPage = page;
                        page.ExpObjects.Add(exp);
                        loadedCount++;
                        loadedNames.Add(exp.ODMRExperimentName);

                        // 添加到搜索列表
                        page.ExpSearchBar.SearchList.Add(
                            new System.Collections.Generic.KeyValuePair<string, object>(
                                exp.ODMRExperimentName + " " + exp.ODMRExperimentGroupName, 
                                exp
                            )
                        );
                    }

                    // 重新排序实验列表
                    page.ExpObjects.Sort((e1, e2) => e1.ODMRExperimentName.CompareTo(e2.ODMRExperimentName));
                });

                if (loadedCount > 0)
                {
                    MessageLogger.LogInfo($"加载了 {loadedCount} 个 AI 动态实验");
                }

                return Ok(new
                {
                    loaded = loadedCount,
                    skipped = skippedCount,
                    total = approvedExps.Count,
                    loadedNames = loadedNames,
                    message = loadedCount > 0 
                        ? $"成功加载 {loadedCount} 个实验到 ODMR 实验列表" 
                        : (skippedCount > 0 ? "所有实验已在列表中" : "没有可加载的实验")
                });
            }
            catch (Exception ex)
            {
                return Err($"加载实验失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新实验列表（重新加载所有动态实验）
        /// </summary>
        [AiCommand("refresh-ai-experiments", "刷新 ODMR 实验列表中的 AI 动态实验（移除旧的，重新加载最新的）", "无参数")]
        private string RefreshAiExperiments(Dictionary<string, string> args)
        {
            try
            {
                var win = MainWindow.Handle;
                if (win == null) return Err("主窗口未就绪");

                var manager = DynamicExperimentManager.Instance;
                int removedCount = 0;
                int loadedCount = 0;
                List<string> loadedNames = new List<string>();

                win.Dispatcher.Invoke(() =>
                {
                    var page = MainWindow.Exp_SequencePage;
                    if (page == null) return;

                    // 获取所有动态实验的 ID 列表
                    var dynamicExpIds = new HashSet<string>();
                    foreach (var exp in manager.Experiments)
                    {
                        if (exp.Status == ReviewStatus.Approved && exp.IsCompiled)
                        {
                            dynamicExpIds.Add(exp.ClassName);
                        }
                    }

                    // 移除旧的动态实验（通过 IsDynamicExperiment 标记或类名匹配）
                    var toRemove = new List<ODMRExpObject>();
                    foreach (var exp in page.ExpObjects)
                    {
                        if (exp.IsDynamicExperiment)
                        {
                            toRemove.Add(exp);
                        }
                    }
                    foreach (var exp in toRemove)
                    {
                        page.ExpObjects.Remove(exp);
                        removedCount++;
                    }

                    // 清空已加载的缓存，强制重新加载
                    // 重新加载所有已批准的实验
                    var instances = manager.LoadAllApprovedExperiments();
                    foreach (var exp in instances)
                    {
                        exp.ParentPage = page;
                        page.ExpObjects.Add(exp);
                        loadedCount++;
                        loadedNames.Add(exp.ODMRExperimentName);

                        // 添加到搜索列表
                        page.ExpSearchBar.SearchList.Add(
                            new System.Collections.Generic.KeyValuePair<string, object>(
                                exp.ODMRExperimentName + " " + exp.ODMRExperimentGroupName, 
                                exp
                            )
                        );
                    }

                    // 重新排序
                    page.ExpObjects.Sort((e1, e2) => e1.ODMRExperimentName.CompareTo(e2.ODMRExperimentName));
                });

                return Ok(new
                {
                    removed = removedCount,
                    loaded = loadedCount,
                    loadedNames = loadedNames,
                    message = $"刷新完成：移除 {removedCount} 个旧实验，加载 {loadedCount} 个新实验"
                });
            }
            catch (Exception ex)
            {
                return Err($"刷新实验失败: {ex.Message}");
            }
        }

    }
}
