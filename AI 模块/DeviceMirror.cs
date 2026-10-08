using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HardWares;
using HardWares.端口基类;
using ODMR_Lab.设备部分;

namespace ODMR_Lab
{
    /// <summary>
    /// 设备镜像上报（路径 B′）：把【本进程（ODMR Lab）已连接的设备】以只读 DTO 形式
    /// 原子落盘到 %ProgramData%\HardWares.AI\mirror\odmr-devices.json，
    /// 供设备控制台（测试项目.exe / HardWares.AI 宿主）集中看到「谁占用了哪些设备」。
    ///
    /// 设计红线（逐条对应方案 §5）：
    ///   · 只读信息面：只导出 key/type/model/serial/desc/portType/connected/inUse；
    ///     ★严禁写入任何设备句柄、写入口、委托或其它可回调对象（JSON 内全部是字符串/布尔）。
    ///   · 不改 HardWares 库：取数只用 public 成员
    ///     （PortObject.ProductName / ProductIdentifier / ProductType / PortType、
    ///      PortObject.IsDeviceConnected、PortElement.ChannelName / ParentDevice、
    ///      InfoBase.GetDeviceDescription / IsWriting）。
    ///   · 零轮询：Publish() 只由事件驱动调用（连接成功 / 自动连接完成 / 关闭设备），
    ///     内部再叠 1 s 节流；本类不创建任何线程与定时器。
    ///   · 可回退：调用 SetEnabled(false, byWhom) 关闭开关后 Publish() 立即返回；删镜像文件或关开关即回到改造前行为。
    ///   · 只写不读：本进程从不读镜像文件，不存在回环依赖。
    /// </summary>
    public static class DeviceMirror
    {
        public const string SchemaId = "hardwares-ai.device-mirror/1";
        public const string WriterName = "ODMR Lab";
        public const int ThrottleMs = 1000;

        private static readonly object _lock = new object();
        private static bool _enabled = true;
        // 默认开启（2026-10-05 用户要求启用）；需临时停用可调用 SetEnabled(false, byWhom) 或删除镜像文件
        private static string _lastError = "";
        private static DateTime _lastPublishUtc = DateTime.MinValue;
        private static DateTime _lastAttemptUtc = DateTime.MinValue;
        private static int _lastCount = 0;
        private static int _skippedByThrottle = 0;

        /// <summary>镜像上报总开关。默认 true（2026-10-05 起启用；与 HostClient.Enabled 相互独立，互不影响）。</summary>
        public static bool Enabled { get { lock (_lock) { return _enabled; } } }

        /// <summary>最近一次发布失败原因（空 = 正常）。</summary>
        public static string LastError { get { lock (_lock) { return _lastError; } } }

        /// <summary>最近一次成功发布的本地时间（未发布过为 DateTime.MinValue）。</summary>
        public static DateTime LastPublishUtc { get { lock (_lock) { return _lastPublishUtc; } } }

        /// <summary>最近一次成功发布的设备台数。</summary>
        public static int LastCount { get { lock (_lock) { return _lastCount; } } }

        /// <summary>镜像目录（与 audit\ / commslog\ / devices\ 同一落盘契约根，不新增顶层目录）。</summary>
        public static string MirrorDirectory
        {
            get
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                return Path.Combine(Path.Combine(root, "HardWares.AI"), "mirror");
            }
        }

        /// <summary>镜像文件完整路径。</summary>
        public static string MirrorPath
        {
            get { return Path.Combine(MirrorDirectory, "odmr-devices.json"); }
        }

        /// <summary>开关镜像上报（只影响本进程写镜像，不影响任何设备访问路径）。</summary>
        public static void SetEnabled(bool enabled, string byWhom)
        {
            lock (_lock) { _enabled = enabled; }
            MessageLogger.LogInfo("设备镜像上报已" + (enabled ? "开启" : "关闭")
                + (string.IsNullOrEmpty(byWhom) ? "" : "（操作者：" + byWhom + "）")
                + "，目标文件 " + MirrorPath, "DeviceMirror");
        }

        /// <summary>
        /// 把本进程已连接的设备写成只读镜像（事件驱动；1 s 节流；失败只记日志，绝不抛出）。
        /// 未启用时立即返回（默认路径，零影响）。
        /// </summary>
        public static void Publish()
        {
            if (!Enabled) return;

            lock (_lock)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - _lastAttemptUtc).TotalMilliseconds < ThrottleMs)
                {
                    _skippedByThrottle++;
                    return;
                }
                _lastAttemptUtc = now;
            }

            try
            {
                List<MirrorItem> items = Collect();
                string json = BuildJson(items);
                AtomicWrite(json);
                lock (_lock)
                {
                    _lastCount = items.Count;
                    _lastPublishUtc = DateTime.Now;
                    _lastError = "";
                }
            }
            catch (Exception ex)
            {
                lock (_lock) { _lastError = ex.Message; }
                MessageLogger.LogError("设备镜像上报失败：" + ex.Message, "DeviceMirror");
            }
        }

        /// <summary>清空镜像（程序关闭时用：写成 0 台，让控制台不再显示本进程的陈旧设备）。</summary>
        public static void Clear()
        {
            if (!Enabled) return;
            try
            {
                AtomicWrite(BuildJson(new List<MirrorItem>()));
                lock (_lock)
                {
                    _lastCount = 0;
                    _lastPublishUtc = DateTime.Now;
                    _lastError = "";
                }
            }
            catch (Exception ex)
            {
                lock (_lock) { _lastError = ex.Message; }
            }
        }

        /// <summary>一句话自述（诊断用；只读，不触发任何设备访问）。</summary>
        public static string SummaryText()
        {
            lock (_lock)
            {
                return string.Format("设备镜像：{0} ｜ 文件 {1} ｜ 上次发布 {2} ｜ {3} 台 ｜ 节流跳过 {4} 次{5}",
                    _enabled ? "已启用" : "已关闭（默认）",
                    MirrorPath,
                    _lastPublishUtc == DateTime.MinValue ? "（从未）" : _lastPublishUtc.ToString("HH:mm:ss"),
                    _lastCount,
                    _skippedByThrottle,
                    string.IsNullOrEmpty(_lastError) ? "" : " ｜ 错误：" + _lastError);
            }
        }

        // ===================== 内部实现 =====================

        /// <summary>镜像条目（纯值类型 DTO：不含任何句柄 / 委托 / 可回调对象）。</summary>
        private sealed class MirrorItem
        {
            public string Key = "";
            public string Type = "";
            public string Model = "";
            public string Serial = "";
            public string Desc = "";
            public string PortType = "";
            public bool Connected = false;
            public bool InUse = false;
        }

        /// <summary>
        /// 采集本进程设备（只读）：遍历 17 类 DeviceTypes → DeviceDispatcher.GetDevice(t)
        /// → InfoBase.GetDeviceDescription() → SourceDevice as PortObject / PortElement。
        /// 按「设备清单键」去重（同一台设备的控制器与各轴会归并为一条）。
        /// </summary>
        private static List<MirrorItem> Collect()
        {
            List<MirrorItem> result = new List<MirrorItem>();
            List<string> keys = new List<string>();

            Array types = Enum.GetValues(typeof(DeviceTypes));
            for (int i = 0; i < types.Length; i++)
            {
                DeviceTypes t = (DeviceTypes)types.GetValue(i);
                List<InfoBase> infos;
                try { infos = DeviceDispatcher.GetDevice(t); }
                catch (Exception) { continue; }
                if (infos == null) continue;

                for (int j = 0; j < infos.Count; j++)
                {
                    InfoBase info = infos[j];
                    if (info == null) continue;

                    PortObject po = null;
                    try { po = info.SourceDevice as PortObject; }
                    catch (Exception) { po = null; }
                    if (po == null)
                    {
                        PortElement el = null;
                        try { el = info.SourceDevice as PortElement; }
                        catch (Exception) { el = null; }
                        if (el != null)
                        {
                            try { po = el.ParentDevice; }
                            catch (Exception) { po = null; }
                        }
                    }
                    if (po == null) continue;

                    string desc = "";
                    try { desc = info.GetDeviceDescription() ?? ""; }
                    catch (Exception) { desc = ""; }

                    bool inuse = false;
                    try { inuse = info.IsWriting; }
                    catch (Exception) { inuse = false; }

                    string key = BuildCatalogKey(po);
                    if (string.IsNullOrEmpty(key)) key = desc;
                    if (string.IsNullOrEmpty(key)) continue;

                    int at = keys.IndexOf(key);
                    if (at >= 0)
                    {
                        if (inuse) result[at].InUse = true;     // 同键归并：占用态取「或」
                        continue;
                    }
                    keys.Add(key);

                    MirrorItem it = new MirrorItem();
                    it.Key = key;
                    it.Type = SafeStr(delegate { return po.ProductType; });
                    it.Model = SafeStr(delegate { return po.ProductIdentifier; });
                    it.Serial = SafeStr(delegate { return po.ProductName; });
                    it.Desc = desc;
                    it.PortType = SafeStr(delegate { return po.PortType.ToString(); });
                    try { it.Connected = PortObject.IsDeviceConnected(po); }
                    catch (Exception) { it.Connected = false; }
                    it.InUse = inuse;
                    result.Add(it);
                }
            }
            return result;
        }

        /// <summary>清单键，规则与写入口 / DeviceCatalog 逐字一致：ProductIdentifier + "_" + ProductName（剔非法文件名字符）。</summary>
        private static string BuildCatalogKey(PortObject po)
        {
            try
            {
                string s1 = po.ProductIdentifier == null ? "" : po.ProductIdentifier;
                string s2 = po.ProductName == null ? "" : po.ProductName;
                string key = s1 + "_" + s2;
                char[] bad = Path.GetInvalidFileNameChars();
                StringBuilder sb = new StringBuilder(key.Length);
                for (int i = 0; i < key.Length; i++)
                {
                    if (Array.IndexOf(bad, key[i]) >= 0) continue;
                    if (key[i] == '/') continue;
                    sb.Append(key[i]);
                }
                return sb.ToString();
            }
            catch (Exception) { return ""; }
        }

        private static string SafeStr(Func<string> f)
        {
            try { return f() == null ? "" : f(); }
            catch (Exception) { return ""; }
        }

        private static string BuildJson(List<MirrorItem> items)
        {
            int pid = 0;
            try { pid = System.Diagnostics.Process.GetCurrentProcess().Id; }
            catch (Exception) { pid = 0; }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.Append("  \"schema\": \"").Append(Esc(SchemaId)).Append("\",\r\n");
            sb.Append("  \"writer\": \"").Append(Esc(WriterName)).Append("\",\r\n");
            sb.Append("  \"pid\": ").Append(pid).Append(",\r\n");
            sb.Append("  \"machine\": \"").Append(Esc(Environment.MachineName)).Append("\",\r\n");
            sb.Append("  \"generatedAt\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\",\r\n");
            sb.Append("  \"count\": ").Append(items.Count).Append(",\r\n");
            sb.Append("  \"devices\": [");
            for (int i = 0; i < items.Count; i++)
            {
                MirrorItem it = items[i];
                if (i > 0) sb.Append(",");
                sb.Append("\r\n    {");
                sb.Append("\"key\": \"").Append(Esc(it.Key)).Append("\", ");
                sb.Append("\"type\": \"").Append(Esc(it.Type)).Append("\", ");
                sb.Append("\"model\": \"").Append(Esc(it.Model)).Append("\", ");
                sb.Append("\"serial\": \"").Append(Esc(it.Serial)).Append("\", ");
                sb.Append("\"desc\": \"").Append(Esc(it.Desc)).Append("\", ");
                sb.Append("\"portType\": \"").Append(Esc(it.PortType)).Append("\", ");
                sb.Append("\"connected\": ").Append(it.Connected ? "true" : "false").Append(", ");
                sb.Append("\"inUse\": ").Append(it.InUse ? "true" : "false");
                sb.Append("}");
            }
            if (items.Count > 0) sb.Append("\r\n  ");
            sb.Append("]\r\n}");
            return sb.ToString();
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\t') sb.Append("\\t");
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>原子落盘：先写 .tmp 再 Replace/Move，避免读者读到半截 JSON。</summary>
        private static void AtomicWrite(string json)
        {
            string dir = MirrorDirectory;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = MirrorPath;
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            if (File.Exists(path))
            {
                try { File.Replace(tmp, path, null); }
                catch (Exception)
                {
                    File.Delete(path);
                    File.Move(tmp, path);
                }
            }
            else
            {
                File.Move(tmp, path);
            }
        }
    }
}
