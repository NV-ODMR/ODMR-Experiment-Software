using System;
using System.IO;
using System.Reflection;
using ODMR_Lab.ODMR实验;

namespace ODMRLab.AI.DynamicExperiments
{
    /// <summary>
    /// 沙箱实验加载器 - 使用独立的 AppDomain 隔离加载动态实验
    /// </summary>
    public class SandboxExperimentLoader : MarshalByRefObject
    {
        private Assembly _loadedAssembly;
        private string _assemblyPath;

        /// <summary>
        /// 加载程序集到当前 AppDomain
        /// </summary>
        public void LoadAssembly(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
            
            // 读取字节避免文件锁定
            byte[] assemblyBytes = File.ReadAllBytes(assemblyPath);
            byte[] pdbBytes = null;
            
            string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                pdbBytes = File.ReadAllBytes(pdbPath);
            }

            if (pdbBytes != null)
            {
                _loadedAssembly = Assembly.Load(assemblyBytes, pdbBytes);
            }
            else
            {
                _loadedAssembly = Assembly.Load(assemblyBytes);
            }
        }

        /// <summary>
        /// 创建实验实例
        /// </summary>
        public object CreateInstance(string className)
        {
            if (_loadedAssembly == null)
            {
                throw new InvalidOperationException("程序集未加载");
            }

            // 尝试完整类名
            var type = _loadedAssembly.GetType(className);
            
            // 如果失败，尝试查找匹配的类名
            if (type == null)
            {
                foreach (var t in _loadedAssembly.GetTypes())
                {
                    if (t.Name == className || t.FullName.EndsWith("." + className))
                    {
                        type = t;
                        break;
                    }
                }
            }

            if (type == null)
            {
                throw new TypeLoadException($"找不到类型: {className}");
            }

            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// 获取加载的程序集
        /// </summary>
        public Assembly LoadedAssembly => _loadedAssembly;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Unload()
        {
            _loadedAssembly = null;
            _assemblyPath = null;
        }
    }

    /// <summary>
    /// 实验加载包装器 - 在主 AppDomain 中使用
    /// </summary>
    public class ExperimentLoaderWrapper
    {
        /// <summary>
        /// 从程序集创建实验实例
        /// </summary>
        public static object CreateExperimentInstance(string assemblyPath, string className)
        {
            try
            {
                // 读取字节避免文件锁定
                byte[] assemblyBytes = File.ReadAllBytes(assemblyPath);
                byte[] pdbBytes = null;
                
                string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                if (File.Exists(pdbPath))
                {
                    pdbBytes = File.ReadAllBytes(pdbPath);
                }

                Assembly assembly;
                if (pdbBytes != null)
                {
                    assembly = Assembly.Load(assemblyBytes, pdbBytes);
                }
                else
                {
                    assembly = Assembly.Load(assemblyBytes);
                }

                // 查找类型
                Type type = null;
                foreach (var t in assembly.GetTypes())
                {
                    if (t.Name == className || t.FullName.EndsWith("." + className))
                    {
                        type = t;
                        break;
                    }
                }

                if (type == null)
                {
                    throw new TypeLoadException($"找不到类型: {className}");
                }

                // 验证是否继承自 ODMRExpObject
                var baseType = typeof(ODMRExpObject);
                if (!baseType.IsAssignableFrom(type))
                {
                    throw new InvalidCastException($"类型 {className} 必须继承自 ODMRExpObject");
                }

                return Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载实验失败: {ex.Message}", ex);
            }
        }
    }
}
