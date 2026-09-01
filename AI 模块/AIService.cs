using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ODMRLab.Services
{
    #region 基础类型

    /// <summary>日志级别</summary>
    public enum LogLevel { Info, Warning, Error }

    /// <summary>单条日志记录</summary>
    public class LogEntry
    {
        public DateTime Time { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>指令特性：标记一个方法为 AI 可调用的指令</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AiCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        /// <summary>指令参数说明（help 中显示）</summary>
        public string Parameters { get; }
        public AiCommandAttribute(string name, string description = "", string parameters = "")
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }
    }

    #endregion

    /// <summary>
    /// AI 控制服务（AIService）
    /// - 通过 HTTP(localhost) 接收外部指令（bat / curl / AI Agent / Python）
    /// - 特性 + 反射自动注册指令（含 partial 各分部文件）
    /// - 统一 JSON 返回（success / message / time）
    /// - 日志分级 + 最近错误缓存（get-logs 可查）
    /// - 安全模式：危险指令（写设备参数 / 开激光 / 启动AFM实验）需 confirm=true
    /// - 业务指令拆分文件：AIService.Experiments.cs / AIService.Devices.cs / AIService.Data.cs
    /// </summary>
    public partial class AIService
    {
        // 指令委托：参数字典 -> JSON 响应
        private delegate string CommandHandler(Dictionary<string, string> args);

        private readonly Dictionary<string, CommandHandler> _commands = new Dictionary<string, CommandHandler>();
        private readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();

        // 日志缓存
        private readonly List<LogEntry> _recentLogs = new List<LogEntry>();
        private const int MaxLogCount = 100;

        // HTTP 监听
        private readonly HttpListener _listener;
        private readonly Thread _listenThread;
        private readonly int _port;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// 安全模式：
        /// safe(默认) - 危险指令需带 confirm=true 才能执行
        /// full       - 完全信任模式（AI 已获人工授权），危险指令免确认
        /// 切换为 full 本身需要 confirm=yes，防止 AI 自行解除保护
        /// </summary>
        public string SafeMode { get; private set; } = "safe";

        public bool IsSafeMode { get { return SafeMode != "full"; } }

        public AIService(int port = 5000)
        {
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");

            _listenThread = new Thread(ListenLoop) { IsBackground = true, Name = "AIServiceThread" };

            // 反射自动注册所有 [AiCommand] 方法
            AutoRegisterCommands();
        }

        #region 启动 / 停止

        public void Start()
        {
            if (IsRunning) return;
            _listener.Start();
            IsRunning = true;
            _listenThread.Start();
            Log("AIService started", LogLevel.Info);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            try { _listener.Stop(); _listener.Close(); } catch { }
            Log("AIService stopped", LogLevel.Info);
        }

        #endregion

        #region 指令自动注册（反射）

        private void AutoRegisterCommands()
        {
            var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<AiCommandAttribute>();
                if (attr == null) continue;

                var handler = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), this, method);
                _commands[attr.Name] = handler;
                _descriptions[attr.Name] = attr.Description;
                _parameters[attr.Name] = attr.Parameters ?? "";
            }
        }

        #endregion

        #region 安全保护

        /// <summary>取参数值（缺失时返回 def）</summary>
        protected string GetArg(Dictionary<string, string> args, string key, string def = "")
        {
            string v = null;
            return args != null && args.TryGetValue(key, out v) ? v : def;
        }

        /// <summary>是否带了确认标志（confirm=true / 1 / yes）</summary>
        protected bool HasConfirm(Dictionary<string, string> args)
        {
            if (args == null) return false;
            string v;
            if (!args.TryGetValue("confirm", out v)) return false;
            v = v.Trim().ToLower();
            return v == "true" || v == "1" || v == "yes";
        }

        /// <summary>
        /// 危险操作门禁：返回 null 表示放行；
        /// 安全模式下未带 confirm=true 时返回错误 JSON。
        /// 危险指令开头必须：string b = NeedConfirm(args, "操作名"); if (b != null) return b;
        /// </summary>
        protected string NeedConfirm(Dictionary<string, string> args, string opname)
        {
            if (!IsSafeMode || HasConfirm(args)) return null;
            Log($"安全模式拦截危险操作：{opname}（需要 confirm=true）", LogLevel.Warning);
            return Err($"当前为安全模式(safe)。「{opname}」是危险操作，需人工确认后在请求中加 confirm=true 重新发送。");
        }

        #endregion

        #region 内置指令

        [AiCommand("get-logs", "获取最近的错误日志，无参数")]
        private string GetLogs(Dictionary<string, string> args)
        {
            lock (_recentLogs)
            {
                var errors = _recentLogs
                    .Where(x => x.Level == LogLevel.Error)
                    .OrderByDescending(x => x.Time)
                    .Select(x => new { time = x.Time.ToString("yyyy-MM-dd HH:mm:ss"), msg = x.Message })
                    .ToList();
                return Json(new { success = true, errors });
            }
        }

        [AiCommand("help", "列出所有可用指令及其描述和参数说明", "无参数")]
        private string Help(Dictionary<string, string> args)
        {
            var list = _descriptions
                .OrderBy(x => x.Key)
                .Select(x => new
                {
                    cmd = x.Key,
                    desc = x.Value,
                    parameters = _parameters.ContainsKey(x.Key) ? _parameters[x.Key] : ""
                })
                .ToList();
            return Json(new { success = true, safeMode = SafeMode, commands = list });
        }

        [AiCommand("ping", "心跳检测，无参数，返回 pong")]
        private string Ping(Dictionary<string, string> args)
            => Ok("pong");

        [AiCommand("app-status", "查询程序当前状态：当前页面/当前实验/运行状态/安全模式", "无参数")]
        private string AppStatus(Dictionary<string, string> args)
        {
            var seqpage = ODMR_Lab.MainWindow.Exp_SequencePage;
            var exp = seqpage != null ? seqpage.CurrentExpObject : null;
            object expinfo = null;
            if (exp != null)
            {
                expinfo = new
                {
                    name = exp.ODMRExperimentGroupName + ":" + exp.ODMRExperimentName,
                    running = !exp.IsExpEnd,
                    paused = exp.IsExpResume,
                    state = exp.GetExpState(),
                    failed = exp.ExpFailedException != null ? exp.ExpFailedException.Message : (string)null
                };
            }
            return Ok(new
            {
                page = ODMR_Lab.MainWindow.CurrentPage != null ? ODMR_Lab.MainWindow.CurrentPage.GetType().Name : (string)null,
                safeMode = SafeMode,
                port = _port,
                experiment = expinfo
            });
        }

        [AiCommand("set-safe-mode", "切换安全模式", "mode=safe|full。切到 full 会解除全部确认保护，必须带 confirm=yes（仅在人工明确指示时执行）")]
        private string SetSafeMode(Dictionary<string, string> args)
        {
            string mode = GetArg(args, "mode").Trim().ToLower();
            if (mode != "safe" && mode != "full")
                return Err("mode 必须是 safe 或 full。当前模式：" + SafeMode);
            if (mode == "full" && !HasConfirm(args))
                return Err("切换到完全信任模式(full)将解除全部危险操作确认保护，属于高危操作，必须带 confirm=yes。AI 不得自行切换，仅在人工明确指示时执行。");
            SafeMode = mode;
            Log($"安全模式切换为：{mode}", mode == "full" ? LogLevel.Error : LogLevel.Warning);
            return Ok("当前安全模式：" + SafeMode + (mode == "full" ? "（完全信任，危险操作免确认）" : "（危险操作需 confirm=true）"));
        }

        [AiCommand("estop", "紧急停止：停止运行中的实验；若无实验运行则关闭激光。任何时候都允许执行", "无参数")]
        private string EStop(Dictionary<string, string> args)
        {
            var seqpage = ODMR_Lab.MainWindow.Exp_SequencePage;
            var exp = seqpage != null ? seqpage.CurrentExpObject : null;
            if (exp != null && !exp.IsExpEnd)
            {
                try
                {
                    exp.Stop();
                    Log("E-STOP：AI 指令停止实验 " + exp.ODMRExperimentName, LogLevel.Error);
                    return Ok("已发送停止指令，实验将在下一个检查点结束并释放设备。");
                }
                catch (Exception ex)
                {
                    return Err("发送停止指令失败：" + ex.Message);
                }
            }
            string msg;
            bool ok = LaserOffInternal("", out msg);
            Log("E-STOP：无运行实验，" + msg, LogLevel.Error);
            return Ok("当前无运行中实验。" + msg);
        }

        #endregion

        #region 参数自动绑定

        /// <summary>将参数字典自动绑定到强类型对象</summary>
        protected T BindParams<T>(Dictionary<string, string> args) where T : new()
        {
            var result = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (!args.TryGetValue(prop.Name, out var val)) continue;
                try { prop.SetValue(result, Convert.ChangeType(val, prop.PropertyType)); } catch { }
            }
            return result;
        }

        #endregion

        #region 监听循环

        private void ListenLoop()
        {
            while (IsRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException) { break; }
                catch (Exception ex) { Log(ex.Message, LogLevel.Error); }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var req = context.Request;
                var resp = context.Response;

                // 仅允许本机
                if (!IPAddress.IsLoopback(req.RemoteEndPoint?.Address))
                {
                    resp.StatusCode = 403;
                    byte[] buf = Encoding.UTF8.GetBytes("{\"error\":\"forbidden\"}");
                    resp.ContentType = "application/json";
                    resp.ContentLength64 = buf.Length;
                    resp.OutputStream.Write(buf, 0, buf.Length);
                    resp.Close();
                    return;
                }

                var cmd = req.QueryString["cmd"] ?? "";
                var args = new Dictionary<string, string>();
                foreach (var key in req.QueryString.AllKeys)
                {
                    if (key != null && key != "cmd")
                        args[key] = req.QueryString[key];
                }

                Log($"Received: {cmd} args={string.Join(",", args.Select(x => $"{x.Key}={x.Value}"))}", LogLevel.Info);

                string result = Dispatch(cmd, args);

                var bytes = Encoding.UTF8.GetBytes(result);
                resp.StatusCode = 200;
                resp.ContentType = "application/json";
                resp.ContentLength64 = bytes.Length;
                resp.OutputStream.Write(bytes, 0, bytes.Length);
                resp.Close();
            }
            catch (Exception ex)
            {
                Log(ex.Message, LogLevel.Error);
            }
        }

        private string Dispatch(string cmd, Dictionary<string, string> args)
        {
            try
            {
                if (_commands.TryGetValue(cmd, out var handler))
                    return handler(args);

                Log($"Unknown command: {cmd}", LogLevel.Error);
                return Err($"Unknown command: {cmd}. Use 'help' to list commands.");
            }
            catch (Exception ex)
            {
                Log(ex.Message, LogLevel.Error);
                return Err(ex.Message);
            }
        }

        #endregion

        #region 日志

        protected void Log(string msg, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry { Time = DateTime.Now, Level = level, Message = msg };
            lock (_recentLogs)
            {
                _recentLogs.Add(entry);
                if (_recentLogs.Count > MaxLogCount) _recentLogs.RemoveAt(0);
            }
        }

        #endregion

        #region 响应工具

        protected string Ok(object data) => Json(new { success = true, data, time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
        protected string Ok(string message) => Json(new { success = true, message, time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
        protected string Err(string message) => Json(new { success = false, message, time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

        private static string Json(object obj) => JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });

        #endregion
    }
}
