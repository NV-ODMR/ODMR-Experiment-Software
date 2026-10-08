using System;
using System.Collections.Generic;
using HardWares.设备管理层.热加载;
using ODMR_Lab;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 设备驱动热加载（智能指令入口）。
    /// ★ 与本地手动入口共用同一底层实现 HardWares.设备管理层.热加载.DeviceDriverHotLoader，不各写一套。
    /// 约束：只支持「新增」型号（net472 默认域无法卸载，换版本必须重启宿主进程）；
    ///       写类指令一律要求 confirm=true，并写入审计（kind=driver.hotload）。
    /// </summary>
    public partial class AIService
    {
        /// <summary>运行期热加载新设备驱动 dll（新增型号立即生效，无需重启）。</summary>
        [AiCommand("hot-load-device-driver", "运行期热加载新设备驱动 dll：新增型号立即出现在型号列表，无需重启；同一路径换版本必须重启宿主进程",
            "path=<dll 绝对路径> 或 dir=<目录>；sha=<期望的 sha256，可选>；confirm=true")]
        private string HotLoadDeviceDriver(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "运行期热加载设备驱动");
            if (block != null) return block;

            string path = GetArg(args, "path", "");
            string dir = GetArg(args, "dir", "");
            string sha = GetArg(args, "sha", "");
            string target = !string.IsNullOrEmpty(path) ? path : dir;
            if (string.IsNullOrEmpty(target)) return Err("缺少 path（dll 绝对路径）或 dir（目录）参数");

            HotLoadResult r;
            try { r = DeviceDriverHotLoader.Load(target, "AIService.5000", sha); }
            catch (Exception ex) { return Err("热加载异常：" + ex.GetType().Name + "：" + ex.Message); }
            if (!r.Success) return Err("热加载失败：" + r.Error);

            return Ok(new
            {
                ok = true,
                action = r.Action,
                dllPath = r.DllPath,
                sha256 = r.Sha256,
                size = r.Size,
                deviceTypes = r.DeviceTypes,
                productIdentifiers = r.ProductIdentifiers,
                warnings = r.Warnings,
                assemblyCountBefore = r.AssemblyCountBefore,
                assemblyCountAfter = r.AssemblyCountAfter,
                text = r.ToText(),
                nextStep = "写清单 <型号标识>_<设备名>.userdat 到 ODMR 的 DevParamDir（落盘后约 2 秒自动重载），再调用 auto-connect 连接"
            });
        }

        /// <summary>列出本进程已加载的驱动程序集。</summary>
        [AiCommand("hot-list-device-drivers", "列出本进程已热加载的设备驱动程序集（只读）", "无参数")]
        private string HotListDeviceDrivers(Dictionary<string, string> args)
        {
            return Ok(new
            {
                text = DeviceDriverHotLoader.List(),
                assemblyCount = DeviceDriverHotLoader.AssemblyCount(),
                pluginsDirectory = DeviceDriverHotLoader.PluginsDirectory,
                pluginsDirectorySource = DeviceDriverHotLoader.ResolvePluginPaths().Source,
                pluginsDirectoryDefault = DeviceDriverHotLoader.DefaultPluginsDirectory,
                pluginsDirectoryNote = DeviceDriverHotLoader.ResolvePluginPaths().Note,
                legacyPluginsDirectory = DeviceDriverHotLoader.LegacyPluginsDirectory,
                legacyDllCount = DeviceDriverHotLoader.ResolvePluginPaths().LegacyDllCount,
                logDirectory = DeviceDriverHotLoader.RootDirectory,
                help = DeviceDriverHotLoader.Help()
            });
        }

        /// <summary>重新加载指定驱动 dll（首次等价加载；内容变化时明确要求重启宿主进程）。</summary>
        [AiCommand("hot-reload-device-driver", "重新加载设备驱动 dll：首次等价于加载；内容未变化仅刷新型号缓存；内容变化时会明确要求重启宿主进程",
            "path=<dll 绝对路径>；sha=<期望的 sha256，可选>；confirm=true")]
        private string HotReloadDeviceDriver(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "重新加载设备驱动");
            if (block != null) return block;
            string path = GetArg(args, "path", "");
            if (string.IsNullOrEmpty(path)) return Err("缺少 path 参数（dll 绝对路径）");
            HotLoadResult r;
            try { r = DeviceDriverHotLoader.Reload(path, "AIService.5000", GetArg(args, "sha", "")); }
            catch (Exception ex) { return Err("重载异常：" + ex.GetType().Name + "：" + ex.Message); }
            if (!r.Success) return Err("重载失败：" + r.Error);
            return Ok(new { ok = true, action = r.Action, dllPath = r.DllPath, sha256 = r.Sha256, message = r.Message, text = r.ToText() });
        }

        /// <summary>卸载驱动 dll（net472 下无法真正卸载，指令会诚实返回失败原因与替代做法）。</summary>
        [AiCommand("hot-unload-device-driver", "卸载设备驱动 dll：net472 默认域无法卸载程序集，本指令会明确返回失败原因与替代做法（移出目录后重启宿主进程）",
            "path=<dll 路径> 或 name=<dll 名称>；confirm=true")]
        private string HotUnloadDeviceDriver(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "卸载设备驱动");
            if (block != null) return block;
            string path = GetArg(args, "path", "");
            string name = GetArg(args, "name", "");
            string target = !string.IsNullOrEmpty(path) ? path : name;
            if (string.IsNullOrEmpty(target)) return Err("缺少 path 或 name 参数");
            HotLoadResult r;
            try { r = DeviceDriverHotLoader.Unload(target, "AIService.5000"); }
            catch (Exception ex) { return Err("卸载异常：" + ex.GetType().Name + "：" + ex.Message); }
            if (!r.Success) return Err("卸载失败：" + r.Error);
            return Ok(new { ok = true, text = r.ToText() });
        }
    }
}
