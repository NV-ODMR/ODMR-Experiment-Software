using CodeHelper;
using HardWares;
using HardWares.端口基类;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ODMR_Lab.设备部分
{
    /// <summary>
    /// 设备清单（DevParamDir\*.userdat）——【已连接设备的记录 / 下次自动连接的依据】。
    ///
    /// ★ 语义（用户裁定 2026-10-05，取代此前「白名单 / 全程序唯一裁判所」口径）：
    ///   工作目录下 DevParamDir 目录里的 *.userdat = 程序在关闭设备/退出程序时由
    ///   InfoBase.CloseDeviceInfoAndSaveParams 写出的「已连接设备记录」，一文件即一条记录，
    ///   文件名 = ProductIdentifier + "_" + ProductName。
    ///   它【不是】允许连接的设备白名单，【不得】作为连接准入判据：新设备必须先能连上，才可能被记录，
    ///   把清单当准入会形成「连不上 ⇒ 写不进记录 ⇒ 永远连不上」的死锁（用户已明确反对）。
    ///   宿主（HardWares 控制台 device.list）只作差异比对与告警来源，【不作】清单真值（也不作准入）。
    ///
    /// 默认口径：EnforceAsWhitelist = false ⇒ 三个 EnsureInCatalog 重载一律放行（fail-open）；
    ///   清单外设备只在日志留一条告警（WarnOutside），不阻断 UI 连接、不阻断设备枚举、
    ///   不阻断实验与参数流程。该开关仅为排障保留（置 true 可临时恢复旧的拦截行为），日常不得开启。
    ///
    /// 本类只做判定与记日志：不连接任何设备、不读写任何设备、不修改任何清单文件。
    /// </summary>
    public static class DeviceCatalog
    {
        /// <summary>清单目录名（相对工作目录）</summary>
        public const string DirectoryName = "DevParamDir";
        /// <summary>清单文件扩展名</summary>
        public const string FileExtension = ".userdat";
        /// <summary>标识「这是设备参数文件」的 FileType 取值</summary>
        public const string FileTypeValue = "DeviceParamsFile";

        /// <summary>
        /// 清单目录【整体缺失】时是否放行（首启引导例外）。
        /// 理由：清单是在「关闭程序」时写出的；全新机器上首次连接设备前清单必然不存在，
        /// 此处若硬拒会形成「无法连接 ⇒ 无法生成清单 ⇒ 永远无法连接」的死锁。
        /// 目录存在但【条目】非法时一律逐条拒绝（不放行）。
        /// </summary>
        public static bool FailOpenWhenMissing = true;

        /// <summary>
        /// 是否把清单当作「连接准入白名单」（默认 false = 不作准入，一律放行）。
        /// ★ 用户裁定：清单只记录曾经连接过的设备、供下次自动连接复用，不是准入真值 ⇒ 默认 false。
        ///   置 true 仅用于排障复现旧行为；届时清单外设备会在连接/枚举/参数流程被拒。
        /// </summary>
        public static bool EnforceAsWhitelist = false;

        /// <summary>清单条目</summary>
        public sealed class Entry
        {
            public string FileName { get; set; }
            public string Key { get; set; }
            public string ProductIdentifier { get; set; }
            public string ProductName { get; set; }
            public string PortType { get; set; }
            public string DeviceType { get; set; }
            public string DeviceInfoType { get; set; }
        }

        private static readonly object _lock = new object();
        private static readonly List<Entry> _entries = new List<Entry>();
        private static readonly List<string> _rejected = new List<string>();
        private static bool _scanned = false;
        private static bool _loaded = false;
        private static bool _dirExists = false;
        private static DateTime _dirStampUtc = DateTime.MinValue;
        private static DateTime _lastScanUtc = DateTime.MinValue;
        private static bool _warnedMissing = false;
        /// <summary>已告警过的「清单外设备」描述（去重，避免高频路径刷日志）</summary>
        private static readonly HashSet<string> _warnedOutside = new HashSet<string>(StringComparer.Ordinal);
        private static DateTime _lastOutsideLogUtc = DateTime.MinValue;

        /// <summary>清单目录绝对路径</summary>
        public static string DirectoryPath
        {
            get { return Path.Combine(Environment.CurrentDirectory, DirectoryName); }
        }

        /// <summary>是否已成功加载到至少 1 条合法清单条目</summary>
        public static bool IsLoaded
        {
            get { RefreshIfNeeded(); lock (_lock) { return _loaded; } }
        }

        /// <summary>当前合法清单条目（副本）</summary>
        public static List<Entry> Entries
        {
            get { RefreshIfNeeded(); lock (_lock) { return new List<Entry>(_entries); } }
        }

        /// <summary>本次加载被拒绝的条目（含原因）</summary>
        public static List<string> Rejected
        {
            get { RefreshIfNeeded(); lock (_lock) { return new List<string>(_rejected); } }
        }

        /// <summary>合法条目数</summary>
        public static int Count
        {
            get { RefreshIfNeeded(); lock (_lock) { return _entries.Count; } }
        }

        /// <summary>强制重新加载（供界面/排查用）</summary>
        public static void Reload()
        {
            lock (_lock) { _scanned = false; }
            RefreshIfNeeded();
        }

        #region 加载与入库校验

        private static void RefreshIfNeeded()
        {
            lock (_lock)
            {
                // 高频路径保护：本类会被设备枚举/轮询高频调用，2 秒内不再访问文件系统
                if (_scanned && (DateTime.UtcNow - _lastScanUtc).TotalSeconds < 2.0) return;

                string dir = DirectoryPath;
                bool exists = false;
                DateTime stamp = DateTime.MinValue;
                try
                {
                    exists = Directory.Exists(dir);
                    if (exists) stamp = Directory.GetLastWriteTimeUtc(dir);
                }
                catch (Exception) { exists = false; }

                if (_scanned && exists == _dirExists && stamp == _dirStampUtc)
                {
                    _lastScanUtc = DateTime.UtcNow;
                    return;
                }

                LoadLocked(dir, exists, stamp);
            }
        }

        private static void LoadLocked(string dir, bool exists, DateTime stamp)
        {
            _entries.Clear();
            _rejected.Clear();
            _scanned = true;
            _lastScanUtc = DateTime.UtcNow;
            _dirExists = exists;
            _dirStampUtc = stamp;

            if (!exists)
            {
                _loaded = false;
                if (FailOpenWhenMissing)
                {
                    if (!_warnedMissing)
                    {
                        _warnedMissing = true;
                        MessageLogger.LogError("未找到设备清单目录：" + dir
                            + " —— 按「首启引导例外」放行（清单会在正常关闭程序时自动生成）。", "DeviceCatalog");
                    }
                }
                return;
            }
            _warnedMissing = false;

            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*" + FileExtension);
            }
            catch (Exception ex)
            {
                _loaded = false;
                _rejected.Add("目录读取失败：" + ex.Message);
                MessageLogger.LogError("设备清单目录读取失败：" + ex.Message, "DeviceCatalog");
                return;
            }

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in files.OrderBy(x => x, StringComparer.Ordinal))
            {
                string fileName = Path.GetFileName(file);
                string key = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(key)) { _rejected.Add(fileName + " → 文件名为空"); continue; }

                Dictionary<string, string> d;
                try
                {
                    d = FileObject.ReadDescription(file);
                }
                catch (Exception ex)
                {
                    _rejected.Add(fileName + " → 解析失败：" + ex.Message);
                    continue;
                }

                string ft = GetValue(d, "FileType");
                string port = GetValue(d, "PortType");
                string dtype = GetValue(d, "DeviceType");
                string itype = GetValue(d, "DeviceInfoType");
                string name = GetValue(d, "DevName");

                if (!string.Equals(ft, FileTypeValue, StringComparison.Ordinal))
                { _rejected.Add(fileName + " → FileType 不是 " + FileTypeValue + "（实际：" + ft + "）"); continue; }
                if (string.IsNullOrEmpty(port)) { _rejected.Add(fileName + " → 缺少 PortType"); continue; }
                if (string.IsNullOrEmpty(dtype)) { _rejected.Add(fileName + " → 缺少 DeviceType"); continue; }
                if (string.IsNullOrEmpty(name)) { _rejected.Add(fileName + " → 缺少 DevName"); continue; }
                if (!keys.Add(key)) { _rejected.Add(fileName + " → 重复条目（同名清单项已存在）"); continue; }
                if (!names.Add(name)) { _rejected.Add(fileName + " → 重复设备名：" + name); continue; }

                string pid;
                string suffix = "_" + name;
                if (key.Length > suffix.Length && key.EndsWith(suffix, StringComparison.Ordinal))
                    pid = key.Substring(0, key.Length - suffix.Length);
                else
                {
                    int u = key.IndexOf('_');
                    pid = u > 0 ? key.Substring(0, u) : key;
                }

                _entries.Add(new Entry
                {
                    FileName = fileName,
                    Key = key,
                    ProductIdentifier = pid,
                    ProductName = name,
                    PortType = port,
                    DeviceType = dtype,
                    DeviceInfoType = itype
                });
            }

            _loaded = _entries.Count > 0;
            MessageLogger.LogInfo("设备清单已加载：" + _entries.Count + " 项"
                + (_rejected.Count > 0 ? "，拒绝 " + _rejected.Count + " 项" : "")
                + "（目录：" + dir + "）", "DeviceCatalog");
            foreach (var r in _rejected)
                MessageLogger.LogError("清单条目已拒绝：" + r, "DeviceCatalog");
        }

        private static string GetValue(Dictionary<string, string> d, string key)
        {
            if (d == null) return "";
            string v;
            if (!d.TryGetValue(key, out v)) return "";
            return v == null ? "" : v.Trim();
        }

        #endregion

        #region 判定与匹配

        /// <summary>按设备句柄算清单键（与写入口 CloseDeviceInfoAndSaveParams 的规则逐字一致）</summary>
        public static string BuildKey(PortObject dev)
        {
            if (dev == null) return "";
            string s = (dev.ProductIdentifier == null ? "" : dev.ProductIdentifier)
                     + "_" + (dev.ProductName == null ? "" : dev.ProductName);
            try
            {
                foreach (char c in Path.GetInvalidFileNameChars())
                    s = s.Replace(c.ToString(), "");
                s = s.Replace("/", "");
            }
            catch (Exception) { }
            return s.Trim();
        }

        /// <summary>键（文件名去扩展名）是否在清单内</summary>
        public static bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            foreach (var e in Entries)
                if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>设备名（ProductName）是否在清单内</summary>
        public static bool ContainsProductName(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return false;
            foreach (var e in Entries)
                if (string.Equals(e.ProductName, productName, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// 按「设备描述」匹配清单。设备描述两种形态：设备级 = ProductName；
        /// 通道级 = ProductName + " " + ChannelName（见 TemperatureChannelInfo.GetDeviceDescription 等）。
        /// </summary>
        public static bool MatchesDescription(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return false;
            foreach (var e in Entries)
            {
                if (string.IsNullOrEmpty(e.ProductName)) continue;
                if (string.Equals(e.ProductName, desc, StringComparison.Ordinal)) return true;
                if (desc.Length > e.ProductName.Length
                    && desc[e.ProductName.Length] == ' '
                    && desc.StartsWith(e.ProductName, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>拒绝文案（可直接回给用户/AI）</summary>
        public static string RejectMessage(string what)
        {
            return "设备不在 ODMR 设备清单内：" + (string.IsNullOrEmpty(what) ? "(未知)" : what)
                 + "。清单目录：" + DirectoryPath + "（当前 " + Count + " 项合法条目）"
                 + "。★ 该清单只记录「曾经连接过的设备」（供下次自动连接复用），"
                 + "【不是】连接准入白名单；正常口径下本提示不会出现，"
                 + "出现即说明排障开关 DeviceCatalog.EnforceAsWhitelist 被置为 true。";
        }

        /// <summary>判定设备句柄是否在清单内</summary>
        public static bool EnsureInCatalog(PortObject dev, out string reason)
        {
            reason = "";
            if (dev == null)
            {
                // 连接未成功、未取得句柄：此处仍不入册（下游需要有效句柄），但这不是「清单」问题
                reason = "连接未成功，未取得设备句柄，未入册（非清单判定）。";
                return false;
            }
            if (!IsLoaded)
            {
                if (FailOpenWhenMissing) { WarnMissing(); return true; }
                reason = RejectMessage(dev.ProductName);
                return false;
            }
            string key = BuildKey(dev);
            foreach (var e in Entries)
                if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase)) return true;
            string pn = dev.ProductName == null ? "" : dev.ProductName;
            if (ContainsProductName(pn)) return true;
            string what = pn + " / " + (dev.ProductIdentifier == null ? "" : dev.ProductIdentifier);
            if (!EnforceAsWhitelist) { WarnOutside(what); reason = ""; return true; }
            reason = RejectMessage(what);
            return false;
        }

        /// <summary>按设备描述判定（拿不到句柄时的退路，例如清单载入阶段的占位项）</summary>
        public static bool EnsureInCatalog(string desc, out string reason)
        {
            reason = "";
            if (string.IsNullOrEmpty(desc))
            {
                if (!EnforceAsWhitelist) { WarnOutside("(空设备描述)"); reason = ""; return true; }
                reason = RejectMessage("(空设备描述)");
                return false;
            }
            if (!IsLoaded)
            {
                if (FailOpenWhenMissing) { WarnMissing(); return true; }
                reason = RejectMessage(desc);
                return false;
            }
            if (MatchesDescription(desc)) return true;
            if (!EnforceAsWhitelist) { WarnOutside(desc); reason = ""; return true; }
            reason = RejectMessage(desc);
            return false;
        }

        /// <summary>
        /// 判定 InfoBase（含通道、位移台各轴）是否在清单内。
        /// 位移台各轴的描述是「Probe:X」这类轴标签，故一律回溯到所属控制器句柄再判定。
        /// </summary>
        public static bool EnsureInCatalog(InfoBase info, out string reason)
        {
            reason = "";
            if (info == null)
            {
                reason = "未取得设备信息对象，未入册（非清单判定）。";
                return false;
            }
            PortObject dev = null;
            try { dev = info.SourceDevice as PortObject; }
            catch (Exception) { }
            if (dev == null)
            {
                try
                {
                    var pe = info.SourceDevice as PortElement;
                    if (pe != null) dev = pe.ParentDevice;
                }
                catch (Exception) { }
            }
            if (dev == null)
            {
                var stage = info as NanoStageInfo;
                if (stage != null && stage.Parent != null) dev = stage.Parent.Device;
            }
            if (dev != null) return EnsureInCatalog(dev, out reason);

            string desc = SafeDescription(info);
            if (EnsureInCatalog(desc, out reason)) return true;
            if (!EnforceAsWhitelist) { WarnOutside(desc); reason = ""; return true; }
            reason = RejectMessage(desc) + "（无法取得设备句柄，按描述判定）";
            return false;
        }

        /// <summary>取设备描述，异常时返回空串</summary>
        public static string SafeDescription(InfoBase info)
        {
            if (info == null) return "";
            try { return info.GetDeviceDescription() ?? ""; }
            catch (Exception) { return ""; }
        }

        private static void WarnMissing()
        {
            lock (_lock)
            {
                if (_warnedMissing) return;
                _warnedMissing = true;
                MessageLogger.LogError("设备清单目录不存在：" + DirectoryPath
                    + " —— 本次按「首启引导例外」放行（不阻断连接）。", "DeviceCatalog");
            }
        }

        /// <summary>
        /// 清单外设备的日志告警：默认口径下【只告警、不拦截】。
        /// 本类会被设备枚举/轮询高频调用，故同一描述只记一次，并对条目数设上限，避免刷日志。
        /// </summary>
        private static void WarnOutside(string what)
        {
            if (string.IsNullOrEmpty(what)) what = "(未知)";
            lock (_lock)
            {
                if (_warnedOutside.Contains(what)) return;
                if (_warnedOutside.Count > 200 && (DateTime.UtcNow - _lastOutsideLogUtc).TotalSeconds < 60.0) return;
                _warnedOutside.Add(what);
                _lastOutsideLogUtc = DateTime.UtcNow;
            }
            MessageLogger.LogError("设备不在清单内（按「清单仅作已连接记录、不作连接准入」口径放行，不阻断）："
                + what + "（清单 " + Count + " 项，" + DirectoryPath + "）", "DeviceCatalog");
        }

        #endregion

        #region 与宿主的差异比对（只告警，不作真值）

        /// <summary>宿主设备 Id 是否指向该设备名</summary>
        public static bool HostIdMatches(string hostDeviceId, string productName)
        {
            if (string.IsNullOrEmpty(hostDeviceId) || string.IsNullOrEmpty(productName)) return false;
            if (string.Equals(hostDeviceId, productName, StringComparison.OrdinalIgnoreCase)) return true;
            if (hostDeviceId.EndsWith("." + productName, StringComparison.OrdinalIgnoreCase)) return true;
            return hostDeviceId.IndexOf(productName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>把宿主设备清单与本地清单做双向差异比对，返回可读告警列表</summary>
        public static List<string> CompareWithHost(List<string> hostDeviceIds)
        {
            var res = new List<string>();
            var ents = Entries;
            var ids = hostDeviceIds == null ? new List<string>() : hostDeviceIds;

            foreach (var e in ents)
            {
                bool found = false;
                foreach (var id in ids) { if (HostIdMatches(id, e.ProductName)) { found = true; break; } }
                if (!found) res.Add("仅本地清单有、宿主未见：" + e.ProductName + "（" + e.Key + "）");
            }
            foreach (var id in ids)
            {
                bool found = false;
                foreach (var e in ents) { if (HostIdMatches(id, e.ProductName)) { found = true; break; } }
                if (!found) res.Add("仅宿主有、本地清单无（仅告警，不参与准入判定）：" + id);
            }
            return res;
        }

        /// <summary>清单摘要（一行，供日志/界面显示）</summary>
        public static string SummaryText()
        {
            var ents = Entries;
            var rej = Rejected;
            if (!_dirExists) return "设备清单：目录不存在（" + DirectoryPath + "）";
            return "设备清单：" + ents.Count + " 项合法"
                 + (rej.Count > 0 ? "，拒绝 " + rej.Count + " 项" : "")
                 + "（" + DirectoryPath + "）";
        }

        #endregion
    }
}
