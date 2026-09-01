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
        public AiCommandAttribute(string name, string description = "") { Name = name; Description = description; }
    }

    #endregion

    /// <summary>
    /// AI 控制服务（AIService）
    /// - 通过 HTTP(localhost) 接收外部指令（bat / curl / AI Agent / Python）
    /// - 特性 + 反射自动注册指令
    /// - 统一 JSON 返回（success / cmd / message / time）
    /// - 日志分级 + 最近错误缓存（get-logs 可查）
    /// - 参数自动绑定（BindParams&lt;T&gt;）
    /// </summary>
    public class AIService
    {
        // 指令委托：参数字典 -> JSON 响应
        private delegate string CommandHandler(Dictionary<string, string> args);

        private readonly Dictionary<string, CommandHandler> _commands = new Dictionary<string, CommandHandler>();
        private readonly Dictionary<string, string> _descriptions = new Dictionary<string, string>();

        // 日志缓存
        private readonly List<LogEntry> _recentLogs = new List<LogEntry>();
        private const int MaxLogCount = 100;

        // HTTP 监听
        private readonly HttpListener _listener;
        private readonly Thread _listenThread;
        private readonly int _port;

        public bool IsRunning { get; private set; }

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
            }
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

        [AiCommand("help", "列出所有可用指令及其描述，无参数")]
        private string Help(Dictionary<string, string> args)
        {
            var list = _descriptions
                .OrderBy(x => x.Key)
                .Select(x => new { cmd = x.Key, desc = x.Value })
                .ToList();
            return Json(new { success = true, commands = list });
        }

        [AiCommand("ping", "心跳检测，无参数，返回 pong")]
        private string Ping(Dictionary<string, string> args)
            => Ok("pong");

        // ====== 以下是你的业务指令区，按需增删 ======
        //[AiCommand("engage", "进针")]
        //private string Engage(Dictionary<string, string> args)
        //{
        //    // TODO: 调用硬件
        //    return Ok("Engaged");
        //}

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
