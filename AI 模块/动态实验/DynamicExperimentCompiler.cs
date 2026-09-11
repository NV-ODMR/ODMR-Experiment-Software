using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace ODMRLab.AI.DynamicExperiments
{
    /// <summary>
    /// 动态实验编译器 - 使用 CSharpCodeProvider 编译实验代码
    /// </summary>
    public class DynamicExperimentCompiler
    {
        /// <summary>
        /// 禁止使用的命名空间（安全检查）
        /// </summary>
        private static readonly HashSet<string> ForbiddenNamespaces = new HashSet<string>
        {
            "System.IO",
            "System.Net",
            "System.Diagnostics",
            "Microsoft.Win32",
            "System.Runtime.InteropServices",
            "System.Security",
            "System.Threading",
            "System.Reflection.Emit"
        };

        /// <summary>
        /// 禁止使用的方法/模式（安全检查）
        /// </summary>
        private static readonly string[] ForbiddenPatterns = new string[]
        {
            @"Process\.Start",
            @"File\.Delete",
            @"File\.Move",
            @"File\.Copy",
            @"Registry\.",
            @"DllImport",
            @"unsafe\s",
            @"fixed\s*\(",
            @"Marshal\.",
            @"Environment\.",
            @"AppDomain\.",
            @"Assembly\.Load"
        };

        /// <summary>
        /// 编译结果
        /// </summary>
        public class CompileResult
        {
            public bool Success { get; set; }
            public string OutputAssemblyPath { get; set; }
            public string ClassName { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }

        /// <summary>
        /// 安全验证源代码
        /// </summary>
        public static List<string> ValidateSourceCode(string sourceCode)
        {
            var violations = new List<string>();

            // 检查禁止的命名空间
            foreach (var ns in ForbiddenNamespaces)
            {
                if (sourceCode.Contains("using " + ns))
                {
                    violations.Add($"禁止使用命名空间: {ns}");
                }
            }

            // 检查禁止的方法调用
            foreach (var pattern in ForbiddenPatterns)
            {
                if (Regex.IsMatch(sourceCode, pattern))
                {
                    violations.Add($"禁止使用: {pattern.Replace(@"\\", @"\")}");
                }
            }

            // 检查是否继承了正确的基类
            if (!Regex.IsMatch(sourceCode, @":\s*ODMRExpObject") &&
                !Regex.IsMatch(sourceCode, @":\s*ODMRExperimentWithoutAFM") &&
                !Regex.IsMatch(sourceCode, @":\s*PulseExpBase"))
            {
                violations.Add("实验类必须继承自 ODMRExpObject、ODMRExperimentWithoutAFM 或 PulseExpBase");
            }

            // 检查是否包含必要的抽象方法实现
            // ODMRExpObject 使用 ExperimentCore()
            // ODMRExperimentWithoutAFM 使用 ODMRExpWithoutAFM()
            // PulseExpBase 使用 ExperimentCore()
            bool hasExperimentCore = sourceCode.Contains("ExperimentCore(");
            bool hasODMRExpWithoutAFM = sourceCode.Contains("ODMRExpWithoutAFM(");
            
            if (!hasExperimentCore && !hasODMRExpWithoutAFM)
            {
                violations.Add("必须实现 ExperimentCore() 或 ODMRExpWithoutAFM() 方法");
            }

            return violations;
        }

        /// <summary>
        /// 编译实验代码
        /// </summary>
        public static CompileResult Compile(string sourceCode, string outputPath)
        {
            var result = new CompileResult();

            try
            {
                // 提取类名
                var classMatch = Regex.Match(sourceCode, @"class\s+(\w+)");
                if (classMatch.Success)
                {
                    result.ClassName = classMatch.Groups[1].Value;
                }
                else
                {
                    result.Errors.Add("未找到类定义");
                    result.Success = false;
                    return result;
                }

                // 创建编译器（使用 C# 6.0+ 支持）
                var providerOptions = new Dictionary<string, string>
                {
                    { "CompilerVersion", "v4.0" }
                };
                using (var provider = new CSharpCodeProvider(providerOptions))
                {
                    var parameters = new CompilerParameters
                    {
                        GenerateExecutable = false,
                        GenerateInMemory = false,
                        OutputAssembly = outputPath,
                        TreatWarningsAsErrors = false,
                        CompilerOptions = "/langversion:5"
                    };

                    // 添加主程序集引用（会自动包含大部分依赖）
                    var mainAssembly = Assembly.GetExecutingAssembly();
                    parameters.ReferencedAssemblies.Add(mainAssembly.Location);
                    
                    // 添加 mscorlib
                    parameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
                    
                    // 添加其他必要的系统程序集（使用完整路径避免重复）
                    var systemDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
                    var requiredAssemblies = new[] { "System.dll", "System.Core.dll" };
                    foreach (var asmName in requiredAssemblies)
                    {
                        var fullPath = Path.Combine(systemDir, asmName);
                        if (File.Exists(fullPath) && !parameters.ReferencedAssemblies.Contains(fullPath))
                        {
                            parameters.ReferencedAssemblies.Add(fullPath);
                        }
                    }
                    
                    // 添加 WPF 相关程序集
                    var wpfDir = systemDir; // WPF DLL 通常在同一目录
                    var wpfAssemblies = new[] 
                    { 
                        "PresentationCore.dll", 
                        "PresentationFramework.dll", 
                        "WindowsBase.dll",
                        "System.Xaml.dll",
                        "Controls.dll"
                    };
                    foreach (var asmName in wpfAssemblies)
                    {
                        var fullPath = Path.Combine(wpfDir, asmName);
                        if (File.Exists(fullPath) && !parameters.ReferencedAssemblies.Contains(fullPath))
                        {
                            parameters.ReferencedAssemblies.Add(fullPath);
                        }
                    }
                    
                    // 添加程序目录下的其他必要程序集（如 Controls.dll）
                    var appDir = Path.GetDirectoryName(mainAssembly.Location);
                    var additionalAssemblies = new[] { "Controls.dll", "HardWares.dll" };
                    foreach (var asmName in additionalAssemblies)
                    {
                        var fullPath = Path.Combine(appDir, asmName);
                        if (File.Exists(fullPath) && !parameters.ReferencedAssemblies.Contains(fullPath))
                        {
                            parameters.ReferencedAssemblies.Add(fullPath);
                        }
                    }
                    // 编译
                    var compilerResults = provider.CompileAssemblyFromSource(
                        parameters, 
                        sourceCode
                    );

                    // 收集错误和警告
                    foreach (CompilerError error in compilerResults.Errors)
                    {
                        if (error.IsWarning)
                        {
                            result.Warnings.Add($"行{error.Line}: {error.ErrorText}");
                        }
                        else
                        {
                            result.Errors.Add($"行{error.Line}: {error.ErrorText}");
                        }
                    }

                    result.Success = compilerResults.Errors.Count == 0;
                    if (result.Success)
                    {
                        result.OutputAssemblyPath = outputPath;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"编译异常: {ex.Message}");
                result.Success = false;
            }

            return result;
        }
    }
}
