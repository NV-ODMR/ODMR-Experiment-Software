using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ODMR_Lab;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 实验管理
    /// 安全设计：
    /// - AFM 类实验（下针等物理接触操作）在安全模式下必须带 confirm=true，且程序本身会弹出人工确认框
    /// - 实验运行中禁止修改参数 / 切换实验
    /// - start-experiment 在后台线程启动，绝不阻塞 HTTP 服务
    /// </summary>
    public partial class AIService
    {
        #region 实验指令

        /// <summary>当前 ODMR 实验对象（无则 null）</summary>
        private ODMRExpObject CurrentExp()
        {
            return MainWindow.Exp_SequencePage != null ? MainWindow.Exp_SequencePage.CurrentExpObject : null;
        }

        /// <summary>
        /// 本次运行保存的数据文件完整路径（未保存返回 null）。
        /// 实验结束时 SaveFile() 自动保存到 保存路径\组名\实验名\实验名+时间戳.userdat，
        /// 并记录在实验对象的 SavedFilePath/SavedFileName；开始新运行时 SavedFileName 会被清空，
        /// 因此非空即可认定属于最近一次运行（含手动点「保存实验文件」的情况）。
        /// </summary>
        private static string GetExpDataFile(ODMRExpObject exp)
        {
            try
            {
                string dir = exp.SavedFilePath;
                string name = exp.SavedFileName;
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name)) return null;
                string path = Path.Combine(dir, name);
                // 自动保存记录的文件名不带 .userdat 扩展名，补上再试
                if (!File.Exists(path) && !name.EndsWith(".userdat", StringComparison.OrdinalIgnoreCase))
                    path += ".userdat";
                return File.Exists(path) ? path : null;
            }
            catch { return null; }
        }

        [AiCommand("list-experiments", "列出所有可用实验", "无参数。返回 index(序号)/name/group/desc/afm(是否AFM扫描实验)")]
        private string ListExperiments(Dictionary<string, string> args)
        {
            var page = MainWindow.Exp_SequencePage;
            if (page == null || page.ExpObjects == null) return Err("ODMR 实验页面不可用");
            int cur = page.CurrentExpObject != null ? page.ExpObjects.IndexOf(page.CurrentExpObject) : -1;
            var list = page.ExpObjects
                .Select((e, i) => new
                {
                    index = i,
                    name = e.ODMRExperimentName,
                    group = e.ODMRExperimentGroupName,
                    desc = e.Description ?? "",
                    afm = ODMRExpObject.IsAFMScanExperiment(e)
                }).ToList();
            return Ok(new { count = list.Count, currentIndex = cur, experiments = list });
        }

        [AiCommand("select-exp", "选择当前实验（加载其参数面板；set-exp-param / start-experiment 前必须先调用）", "index=<序号> 或 name=<实验名>（二选一，序号来自 list-experiments）")]
        private string SelectExp(Dictionary<string, string> args)
        {
            var page = MainWindow.Exp_SequencePage;
            if (page == null || page.ExpObjects == null || page.ExpObjects.Count == 0)
                return Err("ODMR 实验页面不可用或实验列表为空");

            int index = -1;
            int idx;
            string name = GetArg(args, "name");
            if (int.TryParse(GetArg(args, "index"), out idx))
            {
                index = idx;
            }
            else if (!string.IsNullOrEmpty(name))
            {
                var match = page.ExpObjects.Where(e => e.ODMRExperimentName == name).ToList();
                if (match.Count == 0) return Err("未找到实验：" + name + "。用 list-experiments 查看列表");
                if (match.Count > 1) return Err("实验名不唯一：" + name + "，请改用 index 参数");
                index = page.ExpObjects.IndexOf(match[0]);
            }
            if (index < 0 || index >= page.ExpObjects.Count)
                return Err("index 超出范围：" + index + "（有效 0 ~ " + (page.ExpObjects.Count - 1) + "）");

            if (page.CurrentExpObject != null && !page.CurrentExpObject.IsExpEnd)
                return Err("当前实验正在运行，请先 stop-experiment 或等待其结束，再切换实验");

            try
            {
                MainWindow.Handle.Dispatcher.Invoke(() => page.SelectExp(index));
                var e = page.CurrentExpObject;
                Log("select-exp: " + (e != null ? e.ODMRExperimentName : index.ToString()), LogLevel.Info);
                return Ok(new
                {
                    index,
                    name = e != null ? e.ODMRExperimentName : (string)null,
                    group = e != null ? e.ODMRExperimentGroupName : (string)null,
                    afm = e != null && ODMRExpObject.IsAFMScanExperiment(e),
                    hint = "下一步：get-exp-params 查看参数 → set-exp-param 修改 → start-experiment 启动"
                });
            }
            catch (Exception ex)
            {
                return Err("选择实验失败：" + ex.Message);
            }
        }

        [AiCommand("get-exp-params", "查看当前实验的输入参数(可改)/输出参数/设备选择", "无参数。需先 select-exp")]
        private string GetExpParams(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验，请先 select-exp");
            var inputs = new List<object>();
            if (exp.InputParams != null)
                foreach (var p in exp.InputParams)
                    inputs.Add(new
                    {
                        name = p.PropertyName,
                        desc = p.Description ?? "",
                        value = ParamB.GetUnknownParamValueToString(p),
                        type = p.ValueType != null ? p.ValueType.Name : ""
                    });
            var outputs = new List<object>();
            if (exp.OutputParams != null)
                foreach (var p in exp.OutputParams)
                    outputs.Add(new { name = p.PropertyName, desc = p.Description ?? "" });
            var devices = new List<object>();
            if (exp.DeviceList != null)
                foreach (var d in exp.DeviceList)
                    devices.Add(new
                    {
                        name = d.Value.PropertyName,
                        desc = d.Value.Description ?? "",
                        value = ParamB.GetUnknownParamValueToString(d.Value)
                    });
            return Ok(new
            {
                experiment = exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName,
                inputs,
                outputs,
                devices,
                hint = "用 set-exp-param 修改输入参数（param 用 inputs 中 name 字段的值）"
            });
        }

        [AiCommand("set-exp-param", "设置当前实验的一个输入参数（写入参数面板，下次启动实验时生效）", "param=<参数名> value=<值>。参数名来自 get-exp-params 的 name 字段")]
        private string SetExpParam(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验，请先 select-exp");
            if (!exp.IsExpEnd)
                return Err("实验运行中不能修改参数，请先 stop-experiment");

            string param = GetArg(args, "param");
            if (string.IsNullOrEmpty(param)) return Err("缺少 param 参数");
            string value;
            if (!args.TryGetValue("value", out value)) return Err("缺少 value 参数");

            ParamB p = null;
            if (exp.InputParams != null)
                p = exp.InputParams.FirstOrDefault(x => x.PropertyName == param);
            if (p == null)
                return Err("未找到参数：" + param + "。用 get-exp-params 查看参数名");

            try
            {
                ParamB.SetUnknownParamValue(p, value);
            }
            catch (Exception ex)
            {
                return Err("参数值格式不正确（类型 " + (p.ValueType != null ? p.ValueType.Name : "?") + "）：" + ex.Message);
            }

            // 关键：实验启动时 ReadConfig() 从界面面板读值，必须把参数写回面板
            try
            {
                var page = exp.ParentPage;
                if (page != null && MainWindow.Handle != null)
                    MainWindow.Handle.Dispatcher.Invoke(() => p.LoadToPage(new FrameworkElement[] { page }, false));
            }
            catch (Exception ex)
            {
                Log("警告：参数未同步到界面面板：" + ex.Message, LogLevel.Warning);
            }

            Log("set-exp-param " + param + "=" + value + "（实验 " + exp.ODMRExperimentName + "）", LogLevel.Info);
            return Ok(new
            {
                param,
                value = ParamB.GetUnknownParamValueToString(p),
                hint = "已写入参数面板，下次启动实验时生效"
            });
        }

        [AiCommand("start-experiment", "启动当前实验（异步，立即返回；用 exp-status 轮询进度）", "AFM 类实验在安全模式下必须带 confirm=true")]
        private string StartExperiment(Dictionary<string, string> args)
        {
            var page = MainWindow.Exp_SequencePage;
            if (page == null) return Err("ODMR 实验页面不可用");
            var exp = page.CurrentExpObject;
            if (exp == null) return Err("没有当前实验，请先 select-exp");
            if (!exp.IsExpEnd) return Err("当前实验正在运行或停止中");

            // AFM / 下针类实验：物理接触样品，误操作会损坏探针与样品
            bool isAfmdangerous = ODMRExpObject.IsAFMScanExperiment(exp)
                || (exp.ODMRExperimentName != null && (exp.ODMRExperimentName.IndexOf("AFM", StringComparison.OrdinalIgnoreCase) >= 0 || exp.ODMRExperimentName.IndexOf("下针") >= 0))
                || (exp.ODMRExperimentGroupName != null && (exp.ODMRExperimentGroupName.IndexOf("AFM", StringComparison.OrdinalIgnoreCase) >= 0 || exp.ODMRExperimentGroupName.IndexOf("下针") >= 0));
            string block = NeedConfirm(args, "启动AFM/下针类实验");
            if (block != null && isAfmdangerous) return block;

            Log("start-experiment: " + exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName + " (afm=" + isAfmdangerous + ")", LogLevel.Info);
            Thread t = new Thread(() =>
            {
                try
                {
                    // 必须在后台线程调用：AFM 实验的 PreConfirmProcedure 会在 UI 线程弹人工确认框，
                    // 阻塞 HTTP 线程会导致整个 AI 服务无响应
                    // AI 启动实验时跳过 PreConfirmProcedure 弹框
                    ExperimentObject<ExpParamBase, ConfigBase>.SetSkipPreConfirm(true);
                    exp.Start();
                    Log("实验启动流程完成：" + exp.ODMRExperimentName, LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Log("实验启动失败：" + ex.Message, LogLevel.Error);
                }
                finally
                {
                    ExperimentObject<ExpParamBase, ConfigBase>.SetSkipPreConfirm(false);
                }
            }) { IsBackground = true, Name = "AiStartExp" };
            t.Start();

            return Ok(new
            {
                name = exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName,
                afm = isAfmdangerous,
                hint = "实验已异步启动。用 exp-status 轮询进度，结束后 get-exp-outputs 读取结果"
                    + (isAfmdangerous ? "。注意：该实验涉及 AFM 物理操作，探针下针等步骤必须由人工在界面确认，AI 无法也不应代替点击" : "")
            });
        }

        [AiCommand("exp-status", "查询当前实验运行状态:progress(0-100 进度条百分比,扫描实验实时更新)、state(状态文本),实验结束后 dataFile 返回已保存数据文件路径。无需定时轮询,建议在用户询问进度时查询本指令", "无参数")]
        private string ExpStatus(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Ok("没有当前实验");
            string dataFile = GetExpDataFile(exp);
            return Ok(new
            {
                name = exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName,
                running = !exp.IsExpEnd,
                paused = exp.IsExpResume,
                state = exp.GetExpState(),
                progress = Math.Round(exp.GetProgress(), 1),
                error = exp.ExpFailedException != null ? exp.ExpFailedException.Message : (string)null,
                dataFile,
                hint = exp.IsExpEnd
                    ? (dataFile != null
                        ? "实验已结束且数据文件已保存，用 export-data file=<dataFile> 可导出 CSV"
                        : "实验已结束但无数据文件（自动保存被关闭或未设置保存路径），可用 list-data-files 查找数据文件")
                    : "实验运行中，progress 为进度条百分比(0-100)，state 为当前状态文本。用户询问进度时可查询本指令（仅进度有变化时向用户汇报即可）；running=false 表示实验结束，届时 dataFile 将返回数据文件路径"
            });
        }

        [AiCommand("stop-experiment", "停止当前实验（软停止：实验在下一个检查点结束并自动释放设备，任何时候都允许）", "无参数")]
        private string StopExperiment(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验");
            if (exp.IsExpEnd) return Ok("当前实验未在运行");
            try
            {
                exp.Stop();
                Log("stop-experiment: " + exp.ODMRExperimentName, LogLevel.Info);
                return Ok(new
                {
                    name = exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName,
                    hint = "已发送停止指令，实验将在下一个检查点结束、释放设备并自动保存数据文件。轮询 exp-status 直到 running=false，届时 dataFile 字段返回数据文件路径"
                });
            }
            catch (Exception ex)
            {
                return Err("停止失败：" + ex.Message);
            }
        }

        [AiCommand("resume-experiment", "恢复已暂停的实验", "无参数")]
        private string ResumeExperiment(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验");
            if (exp.IsExpEnd) return Err("当前实验未在运行");
            if (!exp.IsExpResume) return Ok("实验未处于暂停状态");
            try
            {
                exp.Resume();
                Log("resume-experiment: " + exp.ODMRExperimentName, LogLevel.Info);
                return Ok("实验已恢复");
            }
            catch (Exception ex)
            {
                return Err("恢复失败：" + ex.Message);
            }
        }

        [AiCommand("get-exp-outputs", "读取当前实验的输出参数值/拟合信息（实验完成后使用）", "无参数")]
        private string GetExpOutputs(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验");
            var outputs = new List<object>();
            if (exp.OutputParams != null)
                foreach (var p in exp.OutputParams)
                    outputs.Add(new { name = p.PropertyName, desc = p.Description ?? "", value = ParamB.GetUnknownParamValueToString(p) });
            var fits = new List<object>();
            if (exp.D1FitDatas != null)
                foreach (var f in exp.D1FitDatas)
                    fits.Add(new { xAxis = f.XAxisName, expr = f.Expression, group = f.GroupName });
            string dataFile = GetExpDataFile(exp);
            return Ok(new
            {
                experiment = exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName,
                running = !exp.IsExpEnd,
                state = exp.GetExpState(),
                error = exp.ExpFailedException != null ? exp.ExpFailedException.Message : (string)null,
                dataFile,
                outputs,
                fits,
                hint = dataFile != null
                    ? "完整数据已保存到 dataFile 字段所示路径，用 export-data file=<dataFile> 可导出 CSV"
                    : "完整数据可用 export-data 导出 CSV（当前无数据文件路径，先用 list-data-files 查找 .userdat 文件）"
            });
        }

        [AiCommand("list-exp-buttons", "列出当前实验的交互按钮（实验页面上方的按钮栏，如「设置全局脉冲参数」「磁场预测」「导入定位文件」等，完整信息来自 InterativeButtons）", "无参数。需先 select-exp。返回每个按钮的 name / blocked(是否涉及位移台移动，永久禁止) / 提示")]
        private string ListExpButtons(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验，请先 select-exp");
            if (exp.InterativeButtons == null || exp.InterativeButtons.Count == 0)
                return Ok(new { experiment = exp.ODMRExperimentName, count = 0, buttons = new object[0], hint = "该实验没有交互按钮" });
            var buttons = new List<object>();
            foreach (var item in exp.InterativeButtons)
            {
                bool blocked = item.Key != null && item.Key.IndexOf("移动") >= 0;
                buttons.Add(new
                {
                    name = item.Key,
                    blocked,
                    note = blocked ? "涉及位移台/扫描台移动，危险操作，本指令永久禁止，请人工在界面执行" : ""
                });
            }
            return Ok(new
            {
                experiment = exp.ODMRExperimentName,
                count = buttons.Count,
                buttons,
                hint = "用 click-exp-button button=<name> 点击；部分按钮会弹出窗口需要人工确认或输入"
            });
        }

        [AiCommand("click-exp-button", "点击当前实验的一个交互按钮（等同人工点击实验页面按钮栏中的对应按钮，在其后台线程执行）", "button=<按钮名> confirm=true(安全模式下必需)。实验运行中禁止；按钮名含「移动」的（移动位移台/扫描台等）永久禁止，只能人工执行；部分按钮会弹出窗口需人工操作")]
        private string ClickExpButton(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp == null) return Err("没有当前实验，请先 select-exp");
            if (!exp.IsExpEnd)
                return Err("实验运行中不能点击按钮（按钮操作可能与运行中的实验争抢设备），请先 stop-experiment");
            if (exp.InterativeButtons == null || exp.InterativeButtons.Count == 0)
                return Err("该实验没有交互按钮");

            string btnname = GetArg(args, "button");
            if (string.IsNullOrEmpty(btnname))
                return Err("缺少 button 参数。可用：" + string.Join(" / ", exp.InterativeButtons.Select(x => x.Key)));
            var item = exp.InterativeButtons.FirstOrDefault(x => x.Key == btnname);
            if (item.Key == null || item.Key != btnname)
                return Err("未找到按钮：" + btnname + "。可用：" + string.Join(" / ", exp.InterativeButtons.Select(x => x.Key)));

            // 危险保护：位移台/扫描台移动只能人工执行（永久禁止，与位移台 Target 参数一致的策略）
            if (btnname.IndexOf("移动") >= 0)
                return Err("按钮「" + btnname + "」会移动位移台/扫描台，属于危险操作，本指令永久禁止，请让用户在界面上手动执行");

            string block = NeedConfirm(args, "点击实验按钮「" + btnname + "」");
            if (block != null) return block;

            // 找到界面上对应的按钮控件，走程序自己的 ButtonClickEvent（内部已含后台线程/禁用按钮/异常提示）
            DecoratedButton ctrl = null;
            try
            {
                var page = exp.ParentPage;
                if (page != null && page.ButtonsPanel != null)
                    ctrl = page.ButtonsPanel.Children.OfType<DecoratedButton>().FirstOrDefault(b => b.Text == btnname);
            }
            catch (Exception ex)
            {
                Log("查找按钮控件失败，改用直接调用：" + ex.Message, LogLevel.Warning);
            }

            Log("click-exp-button " + btnname + "（实验 " + exp.ODMRExperimentName + "）", LogLevel.Info);
            try
            {
                if (ctrl != null)
                {
                    #pragma warning disable 618
                    exp.ButtonClickEvent(ctrl, new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, ctrl));
                    #pragma warning restore 618
                }
                else
                {
                    // 界面上没有该按钮控件时，直接在新线程执行按钮绑定的操作（与 ButtonClickEvent 相同的行为）
                    Action action = item.Value;
                    Thread t = new Thread(() =>
                    {
                        try { action?.Invoke(); }
                        catch (Exception ex) { Log("按钮指令未完成：" + ex.Message, LogLevel.Error); }
                    }) { IsBackground = true, Name = "AiExpButton" };
                    t.Start();
                }
                return Ok(new
                {
                    button = btnname,
                    experiment = exp.ODMRExperimentName,
                    hint = "按钮已在后台线程执行。若该按钮会弹出窗口（参数设置/文件选择/标定等），请人工在界面上完成操作，AI 无法代替窗口内的输入"
                });
            }
            catch (Exception ex)
            {
                return Err("按钮执行失败：" + ex.Message);
            }
        }

        [AiCommand("read-exp-source", "读取类的源代码,便于 AI 理解实验实现细节。支持读取当前实验类或任意指定类。有源码目录时返回源文件;打包安装时自动用 ILSpy 反编译;两者都不可用时降级为反射结构概览", "class=<类名,支持部分匹配,可省略,省略时读取当前实验类> list=1 列出所有可用类 srcdir=<源码根目录,可省略,默认从程序目录自动向上查找 .csproj> only=1 只取具体类(默认含基类链)。反编译结果可能较大,单类超 8 万字符截断")]
        private string ReadExpSource(Dictionary<string, string> args)
        {
            // 自动定位源码根目录
            string srcdir = GetArg(args, "srcdir");
            bool srcdirAuto = string.IsNullOrEmpty(srcdir);
            if (srcdirAuto)
            {
                srcdir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 5 && !string.IsNullOrEmpty(srcdir); i++)
                {
                    try
                    {
                        if (Directory.EnumerateFiles(srcdir, "*.csproj", SearchOption.TopDirectoryOnly).Any())
                            break;
                    }
                    catch { break; }
                    srcdir = Path.GetDirectoryName(srcdir);
                }
            }

            // 列出所有可用类
            if (GetArg(args, "list") == "1")
            {
                return ListAllClasses(srcdir, args);
            }

            // 确定要读取的类
            string className = GetArg(args, "class");
            Type targetType = null;
            List<Type> typechain = null;

            if (!string.IsNullOrEmpty(className))
            {
                // 按类名查找
                targetType = FindTypeByName(className, srcdir);
                if (targetType == null)
                {
                    return Err("未找到类:" + className + "。用 list=1 查看所有可用类,或检查类名拼写");
                }
                // 构建类型链(具体类 + 基类链)
                typechain = new List<Type> { targetType };
                for (Type b = targetType.BaseType; b != null && b != typeof(object); b = b.BaseType)
                    typechain.Add(b);
            }
            else
            {
                // 读取当前实验类(原有行为)
                var exp = CurrentExp();
                if (exp == null) return Err("没有当前实验,请先 select-exp,或指定 class 参数");
                targetType = exp.GetType();
                typechain = new List<Type> { targetType };
                for (Type b = targetType.BaseType; b != null && b != typeof(object); b = b.BaseType)
                    typechain.Add(b);
            }

            // 尝试从源码目录查找
            string foundfile = null;
            Type foundtype = null;
            if (!string.IsNullOrEmpty(srcdir) && Directory.Exists(srcdir))
            {
                foundfile = FindSourceFile(srcdir, typechain, out foundtype);
            }

            // 找到源码:返回文件内容
            if (foundfile != null)
            {
                string content;
                bool truncated = false;
                try
                {
                    content = File.ReadAllText(foundfile);
                    if (content.Length > 200000)
                    {
                        content = content.Substring(0, 200000);
                        truncated = true;
                    }
                }
                catch (Exception ex)
                {
                    return Err("读取源码文件失败:" + ex.Message);
                }
                Log("read-exp-source " + foundtype.Name + " -> " + foundfile, LogLevel.Info);
                return Ok(new
                {
                    found = true,
                    type = foundtype.FullName,
                    file = foundfile,
                    size = content.Length,
                    truncated,
                    content,
                    baseTypes = typechain.Skip(1).Select(x => x.FullName).ToList(),
                    hint = truncated ? "文件过大已截断到 20 万字符,可用 read-exp-source 结合其他指令分段了解" : "完整文件"
                });
            }

            // 没有源码:尝试用 ILSpy 反编译
            string assemblyPath = null;
            try { assemblyPath = targetType.Assembly.Location; } catch { }

            if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
            {
                bool concreteOnly = GetArg(args, "only") == "1";
                try
                {
                    var dec = new CSharpDecompiler(assemblyPath, new DecompilerSettings());
                    var targets = concreteOnly ? typechain.Take(1) : typechain;
                    var sources = new List<object>();
                    int total = 0;
                    bool anyTruncated = false;
                    foreach (var bt in targets)
                    {
                        string code;
                        try
                        {
                            code = dec.DecompileTypeAsString(new FullTypeName(bt.FullName));
                        }
                        catch (Exception ex)
                        {
                            sources.Add(new { type = bt.FullName, content = "", error = "反编译该类失败:" + ex.Message });
                            continue;
                        }
                        bool tr = code.Length > 80000;
                        if (tr) { code = code.Substring(0, 80000); anyTruncated = true; }
                        total += code.Length;
                        sources.Add(new { type = bt.FullName, content = code, truncated = tr });
                        if (total > 250000) { anyTruncated = true; break; }
                    }
                    Log("read-exp-source 反编译 " + targetType.Name + "(" + assemblyPath + ")", LogLevel.Info);
                    return Ok(new
                    {
                        found = false,
                        decompiled = true,
                        type = targetType.FullName,
                        assembly = assemblyPath,
                        baseTypes = typechain.Skip(1).Select(x => x.FullName).ToList(),
                        sources,
                        hint = "无源码文件,以下为从运行程序集反编译的 C# 代码(与编译版本一致)"
                            + (anyTruncated ? ";部分类内容已截断(单类 8 万字符/总计 25 万字符),可带 only=1 只取具体类" : "")
                            + (concreteOnly ? "" : ";带 only=1 可只取具体类")
                    });
                }
                catch (Exception ex)
                {
                    Log("read-exp-source 反编译失败,降级反射概览:" + ex.Message, LogLevel.Warning);
                }
            }

            // 反编译也不可用:降级为反射结构概览
            Log("read-exp-source 无源码且反编译不可用,返回反射概览:" + targetType.FullName, LogLevel.Info);
            return Ok(new
            {
                found = false,
                decompiled = false,
                type = targetType.FullName,
                baseTypes = typechain.Skip(1).Select(x => x.FullName).ToList(),
                methods = BuildReflectionMethods(targetType),
                hint = "未找到源码文件" + (srcdirAuto ? "(当前为打包安装形式,无源码目录)" : "(在 " + srcdir + " 下未找到)") + ",反编译也不可用,已返回该类的反射结构概览(仅方法名/签名);如需完整源码请在开发环境部署后带 srcdir 参数重试"
            });
        }

        /// <summary>按类名查找类型(支持部分匹配)</summary>
        private Type FindTypeByName(string className, string srcdir)
        {
            // 1. 先尝试从当前加载的程序集中查找精确匹配
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name == className || type.FullName == className)
                            return type;
                    }
                }
                catch { } // 忽略无法加载的程序集
            }

            // 2. 精确匹配失败,尝试部分匹配
            var matches = new List<Type>();
            foreach (var asm in assemblies)
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                            matches.Add(type);
                    }
                }
                catch { }
            }

            // 如果只有一个匹配,返回它
            if (matches.Count == 1) return matches[0];

            // 多个匹配时,优先返回实验类
            if (matches.Count > 1)
            {
                var expMatch = matches.FirstOrDefault(t => t.Name.Contains("Exp") || t.Name.Contains("CW") || t.Name.Contains("Rabi") || t.Name.Contains("T1") || t.Name.Contains("T2"));
                if (expMatch != null) return expMatch;
                return matches[0]; // 返回第一个
            }

            // 3. 从源码目录查找(如果有的话)
            if (!string.IsNullOrEmpty(srcdir) && Directory.Exists(srcdir))
            {
                var files = new List<string>();
                try
                {
                    foreach (var f in Directory.EnumerateFiles(srcdir, "*.cs", SearchOption.AllDirectories))
                    {
                        string lower = f.ToLowerInvariant();
                        if (lower.Contains("\\bin\\") || lower.Contains("\\obj\\") || lower.Contains("\\packages\\")
                            || lower.Contains("\\.git\\") || lower.Contains("\\devparamdir\\"))
                            continue;
                        files.Add(f);
                        if (files.Count > 3000) break;
                    }
                }
                catch { }

                foreach (var f in files)
                {
                    try
                    {
                        string fileContent = File.ReadAllText(f);
                        // 查找 class 声明
                        var match = System.Text.RegularExpressions.Regex.Match(fileContent, @"\bclass\s+([\w_]+)\b");
                        if (match.Success)
                        {
                            string foundClassName = match.Groups[1].Value;
                            if (foundClassName.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // 找到匹配的类名,尝试从程序集中找到对应的 Type
                                foreach (var asm in assemblies)
                                {
                                    try
                                    {
                                        foreach (var type in asm.GetTypes())
                                        {
                                            if (type.Name == foundClassName)
                                                return type;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        /// <summary>列出所有可用的类</summary>
        private string ListAllClasses(string srcdir, Dictionary<string, string> args)
        {
            var classes = new List<object>();
            var seen = new HashSet<string>();

            // 1. 从源码目录扫描
            if (!string.IsNullOrEmpty(srcdir) && Directory.Exists(srcdir))
            {
                try
                {
                    foreach (var f in Directory.EnumerateFiles(srcdir, "*.cs", SearchOption.AllDirectories))
                    {
                        string lower = f.ToLowerInvariant();
                        if (lower.Contains("\\bin\\") || lower.Contains("\\obj\\") || lower.Contains("\\packages\\")
                            || lower.Contains("\\.git\\") || lower.Contains("\\devparamdir\\"))
                            continue;

                        try
                        {
                            string content = File.ReadAllText(f);
                            // 查找所有 class 声明
                            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"\bclass\s+([\w_]+)\b");
                            foreach (System.Text.RegularExpressions.Match match in matches)
                            {
                                string className = match.Groups[1].Value;
                                if (!seen.Contains(className))
                                {
                                    seen.Add(className);
                                    classes.Add(new
                                    {
                                        name = className,
                                        source = "file",
                                        file = Path.GetFileName(f)
                                    });
                                }
                            }
                        }
                        catch { }

                        if (classes.Count > 500) break; // 限制数量
                    }
                }
                catch { }
            }

            // 2. 从运行中的程序集扫描(补充源码中没有的类)
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    // 只扫描 ODMR_Lab 命名空间下的类
                    if (!asm.FullName.Contains("ODMR") && !asm.FullName.Contains("ODMR_Lab"))
                        continue;

                    foreach (var type in asm.GetTypes())
                    {
                        if (type.IsClass && !type.IsAbstract && !type.IsInterface && !seen.Contains(type.Name))
                        {
                            seen.Add(type.Name);
                            classes.Add(new
                            {
                                name = type.Name,
                                fullname = type.FullName,
                                source = "assembly",
                                assembly = asm.GetName().Name
                            });
                        }
                    }
                }
                catch { }

                if (classes.Count > 1000) break; // 限制总数
            }

            // 按名称排序
            classes = classes.OrderBy(c => ((dynamic)c).name).ToList();

            Log("list-all-classes: 找到 " + classes.Count + " 个类", LogLevel.Info);
            return Ok(new
            {
                count = classes.Count,
                classes,
                hint = "用 read-exp-source class=<类名> 读取指定类的源码;支持部分匹配(如 class=CW 会匹配 TotalCW)"
            });
        }

        /// <summary>在源码目录下查找 typechain 中任一类的定义文件，优先具体类、文件名同名、命名空间匹配</summary>
        private static string FindSourceFile(string srcdir, List<Type> typechain, out Type matched)
        {
            matched = null;
            string bestfile = null;
            int bestscore = 0;
            var fileset = new List<string>();
            try
            {
                foreach (var f in Directory.EnumerateFiles(srcdir, "*.cs", SearchOption.AllDirectories))
                {
                    string lower = f.ToLowerInvariant();
                    if (lower.Contains("\\bin\\") || lower.Contains("\\obj\\") || lower.Contains("\\packages\\")
                        || lower.Contains("\\.git\\") || lower.Contains("\\devparamdir\\"))
                        continue;
                    fileset.Add(f);
                    if (fileset.Count > 3000) break;
                }
            }
            catch { }

            foreach (var f in fileset)
            {
                foreach (var t in typechain)
                {
                    string cls = t.Name;
                    // 第一轮：文件名与类名相同
                    if (bestscore < 2 && Path.GetFileNameWithoutExtension(f) == cls)
                    {
                        int score = 2;
                        if (t != typechain[0]) score -= 1; // 基类文件降权
                        if (score > bestscore)
                        {
                            bestscore = score;
                            bestfile = f;
                            matched = t;
                        }
                    }
                }
            }

            // 第二轮：内容中包含 class 声明（含 using 的命名空间匹配加分）
            if (bestscore < 2)
            {
                foreach (var f in fileset)
                {
                    string content;
                    try { content = File.ReadAllText(f); }
                    catch { continue; }
                    foreach (var t in typechain)
                    {
                        string pattern = "\\bclass\\s+" + Regex.Escape(t.Name) + "\\b";
                        if (Regex.IsMatch(content, pattern))
                        {
                            int score = 1;
                            if (t != typechain[0]) score = 0;
                            if (!string.IsNullOrEmpty(t.Namespace) && content.IndexOf("namespace " + t.Namespace, StringComparison.Ordinal) >= 0)
                                score += (t == typechain[0] ? 1 : 0);
                            if (score > bestscore)
                            {
                                bestscore = score;
                                bestfile = f;
                                matched = t;
                            }
                        }
                    }
                }
            }
            return bestscore > 0 ? bestfile : null;
        }

        /// <summary>反射生成类的结构概览（打包安装无源码时的降级输出）</summary>
        private static List<object> BuildReflectionMethods(Type t)
        {
            var res = new List<object>();
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (m.IsSpecialName) continue;
                if (res.Count >= 200) break;
                var ps = new List<string>();
                foreach (var p in m.GetParameters())
                    ps.Add(p.ParameterType.Name + " " + p.Name);
                res.Add(new
                {
                    name = m.Name,
                    access = m.IsPublic ? "public" : (m.IsFamily ? "protected" : (m.IsAssembly ? "internal" : "private")),
                    @params = string.Join(", ", ps),
                    @abstract = m.IsAbstract
                });
            }
            return res;
        }

        #endregion
    }
}
