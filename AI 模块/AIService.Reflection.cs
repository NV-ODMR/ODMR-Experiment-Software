using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ODMR_Lab;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 反射查询
    /// 用于查询类信息、命名空间等元数据
    /// </summary>
    public partial class AIService
    {
        #region 反射查询指令

        [AiCommand("get-class-info", "查询指定类的命名空间和基本信息", "class=<类名，支持部分匹配>")]
        private string GetClassInfo(Dictionary<string, string> args)
        {
            try
            {
                string className = GetArg(args, "class", "");
                if (string.IsNullOrEmpty(className))
                {
                    return Err("缺少必要参数: class");
                }

                // 获取所有加载的程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var results = new List<object>();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // 跳过系统程序集
                        if (assembly.FullName.StartsWith("System.") || 
                            assembly.FullName.StartsWith("Microsoft.") ||
                            assembly.FullName.StartsWith("mscorlib"))
                            continue;

                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            // 支持部分匹配（不区分大小写）
                            if (type.Name.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                type.FullName.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var info = new
                                {
                                    name = type.Name,
                                    fullName = type.FullName,
                                    namespaceName = type.Namespace,
                                    assembly = assembly.GetName().Name,
                                    isClass = type.IsClass,
                                    isAbstract = type.IsAbstract,
                                    isInterface = type.IsInterface,
                                    baseType = type.BaseType?.Name,
                                    interfaces = type.GetInterfaces().Select(i => i.Name).Take(5).ToArray()
                                };
                                results.Add(info);

                                // 限制返回数量
                                if (results.Count >= 50)
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略无法访问的程序集
                        continue;
                    }

                    if (results.Count >= 50)
                        break;
                }

                if (results.Count == 0)
                {
                    return Err($"未找到匹配的类: {className}");
                }

                return Ok(new
                {
                    query = className,
                    count = results.Count,
                    classes = results
                });
            }
            catch (Exception ex)
            {
                return Err($"查询类信息失败: {ex.Message}");
            }
        }

        #endregion
    }
}
