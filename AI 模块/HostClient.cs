using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    /// <summary>
    /// 设备宿主（HardWares 控制台，回环 127.0.0.1:5001）客户端骨架。
    ///
    /// ★ 总开关默认【关闭】。Enabled=false 时本类不参与任何判定，程序行为与改造前一致
    ///   （此时清单判定仍会参与，但其默认口径为【只放行并记日志】——清单不作连接准入）。
    /// 开启后：连接层必须先探活宿主；宿主不可达则【拒绝连接】，严禁降级为本地直连。
    ///
    /// 长连接约定：单例 HttpClient + Keep-Alive 复用（实测长连接 0.23~0.31 ms/次，
    /// 每次新建连接 1.2~1.5 ms、p90 10~12 ms ⇒ 复用是强制约束）。
    ///
    /// 本轮约束：不改硬件库、不连真机 ⇒ 未与真实宿主联通，响应解析采用容错式。
    /// </summary>
    public static class HostClient
    {
        /// <summary>宿主端口（HardWares 控制台唯一启动者 = 测试项目.exe）</summary>
        public const int Port = 5001;
        /// <summary>宿主基址（仅回环）</summary>
        public const string BaseUrl = "http://127.0.0.1:5001/";
        /// <summary>普通请求超时（秒）</summary>
        public const int NormalTimeoutSeconds = 20;
        /// <summary>连接类请求超时（秒）</summary>
        public const int LongTimeoutSeconds = 120;
        /// <summary>连接池上限</summary>
        public const int MaxConnections = 8;

        private static readonly object _lock = new object();
        private static HttpClient _normalClient = null;
        private static HttpClient _longClient = null;
        private static bool _enabled = false;
        private static string _lastError = "";
        private static string _lastDeviceListRaw = "";
        private static List<string> _lastDeviceIds = new List<string>();

        /// <summary>总开关（默认关闭）</summary>
        public static bool Enabled { get { return _enabled; } }

        /// <summary>最近一次失败原因</summary>
        public static string LastError { get { return _lastError; } }

        /// <summary>最近一次 device.list 原始响应（排查用）</summary>
        public static string LastDeviceListRaw { get { return _lastDeviceListRaw; } }

        /// <summary>最近一次 device.list 解析出的设备 Id</summary>
        public static List<string> LastDeviceIds { get { return new List<string>(_lastDeviceIds); } }

        /// <summary>切换总开关（默认关闭；开启后连接层走宿主）</summary>
        public static void SetEnabled(bool enabled, string byWhom)
        {
            _enabled = enabled;
            MessageLogger.LogInfo("设备宿主代理总开关 → " + (enabled ? "开启" : "关闭")
                + (string.IsNullOrEmpty(byWhom) ? "" : "（操作者：" + byWhom + "）"), "HostClient");
        }

        #region HTTP 基础设施（单例长连接）

        private static HttpClient GetClient(int timeoutSeconds)
        {
            lock (_lock)
            {
                if (timeoutSeconds >= LongTimeoutSeconds)
                {
                    if (_longClient == null) _longClient = BuildClient(LongTimeoutSeconds);
                    return _longClient;
                }
                if (_normalClient == null) _normalClient = BuildClient(NormalTimeoutSeconds);
                return _normalClient;
            }
        }

        private static HttpClient BuildClient(int timeoutSeconds)
        {
            // 说明：.NET Framework 4.7.2 没有 SocketsHttpHandler，改用 HttpClientHandler（默认 Keep-Alive）。
            var handler = new HttpClientHandler();
            try { handler.UseProxy = false; handler.Proxy = null; } catch (Exception) { }
            try { handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; }
            catch (Exception) { }

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            try { client.DefaultRequestHeaders.ConnectionClose = false; } catch (Exception) { }
            try
            {
                var sp = ServicePointManager.FindServicePoint(new Uri(BaseUrl));
                sp.ConnectionLimit = MaxConnections;
                sp.Expect100Continue = false;
            }
            catch (Exception) { }
            return client;
        }

        private static async Task<string> GetAsync(string url, int timeoutSeconds)
        {
            var resp = await GetClient(timeoutSeconds).GetAsync(url).ConfigureAwait(false);
            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<string> PostAsync(string path, Dictionary<string, string> fields, int timeoutSeconds)
        {
            var pairs = new List<string>();
            if (fields != null)
            {
                foreach (var kv in fields)
                    pairs.Add(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value == null ? "" : kv.Value));
            }
            var content = new StringContent(string.Join("&", pairs), Encoding.UTF8, "application/x-www-form-urlencoded");
            var resp = await GetClient(timeoutSeconds).PostAsync(BaseUrl + path, content).ConfigureAwait(false);
            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static string Describe(Exception ex)
        {
            if (ex == null) return "未知错误";
            var baseEx = ex;
            while (baseEx.InnerException != null) baseEx = baseEx.InnerException;
            return baseEx.GetType().Name + "：" + baseEx.Message;
        }

        #endregion
    

        #region 命令面

        /// <summary>探活：GET /（宿主的无审计、无调度状态探针）</summary>
        public static bool Ping(out string err)
        {
            err = "";
            try
            {
                string s = Task.Run(() => GetAsync(BaseUrl, NormalTimeoutSeconds)).GetAwaiter().GetResult();
                if (string.IsNullOrEmpty(s)) { err = "宿主返回空响应"; return false; }
                return s.IndexOf("hardwares-ai", StringComparison.OrdinalIgnoreCase) >= 0
                    || s.IndexOf("HardWares", StringComparison.OrdinalIgnoreCase) >= 0
                    || s.TrimStart().StartsWith("{", StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                err = Describe(ex);
                return false;
            }
        }

        /// <summary>通用调用：POST /exec（表单，body 必带 cmd）</summary>
        public static string Invoke(string cmd, Dictionary<string, string> fields, int timeoutSeconds, out string err)
        {
            err = "";
            if (string.IsNullOrEmpty(cmd)) { err = "cmd 不能为空"; return null; }
            var body = new Dictionary<string, string>(StringComparer.Ordinal);
            body["cmd"] = cmd;
            if (fields != null)
            {
                foreach (var kv in fields) body[kv.Key] = kv.Value;
            }
            try
            {
                return Task.Run(() => PostAsync("exec", body, timeoutSeconds)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                err = Describe(ex);
                return null;
            }
        }

        /// <summary>取宿主设备 Id 列表（容错解析：productId / deviceId / id 三种字段名都认）</summary>
        public static List<string> ListDeviceIds(out string err)
        {
            var ids = new List<string>();
            string raw = Invoke("device.list", null, NormalTimeoutSeconds, out err);
            if (raw == null)
            {
                if (string.IsNullOrEmpty(err)) err = "宿主未响应";
                _lastError = err;
                return ids;
            }
            _lastDeviceListRaw = raw;
            try
            {
                foreach (Match m in Regex.Matches(raw, "\"(?:productId|deviceId|id|name)\"\\s*:\\s*\"([^\"]+)\""))
                {
                    string v = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(v) && !ids.Contains(v)) ids.Add(v);
                }
            }
            catch (Exception ex)
            {
                err = "响应解析失败：" + ex.Message;
            }
            _lastDeviceIds = new List<string>(ids);
            if (ids.Count == 0 && string.IsNullOrEmpty(err)) err = "宿主未返回任何设备";
            _lastError = err;
            return ids;
        }

        /// <summary>宿主是否存在该设备（按产品名宽容匹配）</summary>
        public static bool HostHasDevice(string productName, out string matchedId)
        {
            matchedId = "";
            foreach (var id in _lastDeviceIds)
            {
                if (DeviceCatalog.HostIdMatches(id, productName)) { matchedId = id; return true; }
            }
            return false;
        }

        /// <summary>请求宿主连接清单内全部设备（清单为真值，宿主只提供可连接目标）</summary>
        public static int ConnectCatalog(out string err, out List<string> details)
        {
            err = "";
            details = new List<string>();
            var ids = ListDeviceIds(out err);
            if (ids.Count == 0)
            {
                if (string.IsNullOrEmpty(err)) err = "宿主未返回任何设备";
                return 0;
            }
            int ok = 0;
            foreach (var e in DeviceCatalog.Entries)
            {
                string hit = null;
                foreach (var id in ids)
                {
                    if (DeviceCatalog.HostIdMatches(id, e.ProductName)) { hit = id; break; }
                }
                if (string.IsNullOrEmpty(hit))
                {
                    details.Add("宿主无对应设备，跳过：" + e.ProductName);
                    continue;
                }
                string oneErr;
                var body = new Dictionary<string, string>(StringComparer.Ordinal);
                body["deviceId"] = hit;
                body["confirm"] = "true";
                string raw = Invoke("device.connect", body, LongTimeoutSeconds, out oneErr);
                if (raw == null)
                {
                    details.Add("请求连接失败：" + hit + " → " + oneErr);
                    continue;
                }
                details.Add("已请求连接：" + hit);
                ok++;
            }
            return ok;
        }

        /// <summary>与本地清单做双向差异比对并记日志（只告警，不作真值，也不阻断）</summary>
        public static List<string> CompareWithLocalCatalog(out string err)
        {
            var ids = ListDeviceIds(out err);
            var res = DeviceCatalog.CompareWithHost(ids);
            foreach (var r in res)
                MessageLogger.LogError("清单/宿主差异：" + r, "HostClient");
            if (res.Count == 0 && string.IsNullOrEmpty(err))
                MessageLogger.LogInfo("清单与宿主设备一致（" + ids.Count + " 项）", "HostClient");
            return res;
        }

        /// <summary>
        /// 代理接线完整时序（方案 §5.4）：探活 GET / → 拉宿主 device.list → 与本地清单差异比对 → 请求宿主连接。
        /// 返回实际请求连接的设备数；errors 非空表示在探活阶段即失败（此时不发起任何连接请求）。
        /// ★ 仅在 HostClient.Enabled = true 时由调用方使用；默认关闭时零影响。
        /// </summary>
        public static int PrepareAndConnectAll(out string err, out List<string> details)
        {
            err = "";
            details = new List<string>();
            if (!Ping(out err))
            {
                err = "设备宿主（HardWares 控制台 127.0.0.1:5001）不可达：" + err
                    + "；按策略已禁止降级为本地直连。请先启动设备宿主（测试项目.exe）。";
                MessageLogger.LogError(err, "HostClient");
                return 0;
            }
            details.Add("宿主探活：正常");

            string derr;
            var warns = CompareWithLocalCatalog(out derr);
            if (!string.IsNullOrEmpty(derr)) details.Add("差异比对提示：" + derr);
            foreach (var w in warns) details.Add("差异告警：" + w);
            details.Add("差异比对：本地清单 " + DeviceCatalog.Count + " 项，宿主设备 "
                + _lastDeviceIds.Count + " 项，差异 " + warns.Count + " 条");

            string cerr;
            List<string> cdetails;
            int n = ConnectCatalog(out cerr, out cdetails);
            if (!string.IsNullOrEmpty(cerr)) details.Add("连接阶段提示：" + cerr);
            if (cdetails != null)
            {
                foreach (var d in cdetails) details.Add(d);
            }
            MessageLogger.LogInfo("代理连接完成：已请求 " + n + " 台清单内设备", "HostClient");
            return n;
        }

        /// <summary>
        /// 连接层总闸。返回 true = 允许按本地（清单）方式连接；
        /// 返回 false = 必须拒绝，reason 给出原因（★严禁降级为本地直连）。
        /// </summary>
        public static bool AllowLocalConnect(out string reason)
        {
            reason = "";
            if (!_enabled) return true;
            string err;
            if (!Ping(out err))
            {
                reason = "设备宿主（HardWares 控制台 127.0.0.1:5001）不可达：" + err
                       + "；按策略已禁止降级为本地直连。请先启动设备宿主（测试项目.exe）。";
                MessageLogger.LogError(reason, "HostClient");
                return false;
            }
            return true;
        }

        /// <summary>连接层总闸（带设备名，用于单设备入口）</summary>
        public static bool AllowLocalConnect(string productName, out string reason)
        {
            if (!AllowLocalConnect(out reason)) return false;
            if (!_enabled) return true;
            if (string.IsNullOrEmpty(productName)) return true;
            if (HostHasDevice(productName, out string _)) return true;
            reason = "宿主清单中没有该设备：" + productName + "（宿主设备 Id 见 device.list），已拒绝连接。";
            MessageLogger.LogError(reason, "HostClient");
            return false;
        }

        #endregion
    }
}
