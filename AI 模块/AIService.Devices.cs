using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HardWares;
using HardWares.端口基类;
using HardWares.端口基类部分;
using ODMR_Lab;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.其他设备;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 设备控制
    /// 安全设计：
    /// - device-get 纯读、不占用设备（与设备监测页相同机制），实验运行中也可调用
    /// - device-set 安全模式下必须 confirm=true；写前检查设备占用；位移台目标位置写入永远禁止（必须人工操作）
    /// - laser-on 安全模式下必须 confirm=true；laser-off 永远允许
    /// - apd-sample 会短暂独占 APD，实验运行中自动拒绝
    /// - camera-open 安全模式下必须 confirm=true；预览窗口独占相机直到 camera-close 关窗；camera-close 永远允许
    /// - auto-connect 安全模式下必须 confirm=true；实验运行中或任何设备被占用时禁止（重连会替换设备列表）
    /// </summary>
    public partial class AIService
    {
        #region 设备指令

        /// <summary>一条设备参数引用</summary>
        private class DeviceParamRef
        {
            public string Desc;
            public string Channel;
            public Parameter Param;
        }

        private static string SafeDesc(InfoBase info)
        {
            try { return info.GetDeviceDescription() ?? ""; }
            catch { return "?"; }
        }

        /// <summary>是否位移台类设备（任何移动都视为危险，目标位置写入永远禁止）</summary>
        private static bool IsStageType(DeviceTypes t)
        {
            return t == DeviceTypes.位移台
                || t == DeviceTypes.探针位移台
                || t == DeviceTypes.样品位移台
                || t == DeviceTypes.微波位移台
                || t == DeviceTypes.镜头位移台
                || t == DeviceTypes.磁铁位移台
                || t == DeviceTypes.AFM扫描台;
        }

        /// <summary>从 type+desc 解析目标设备；info==null 时 errmsg 为原因</summary>
        private InfoBase ResolveDevice(Dictionary<string, string> args, out DeviceTypes dtype, out string errmsg)
        {
            dtype = default(DeviceTypes);
            errmsg = "";
            string typestr = GetArg(args, "type");
            string desc = GetArg(args, "desc");
            DeviceTypes t;
            if (string.IsNullOrEmpty(typestr) || !Enum.TryParse(typestr, out t))
            {
                errmsg = "缺少或无效的 type 参数（如 type=锁相放大器），用 device-list 查看可用类型";
                return null;
            }
            dtype = t;
            List<InfoBase> all;
            try { all = DeviceDispatcher.GetDevice(t) ?? new List<InfoBase>(); }
            catch (Exception ex) { errmsg = "设备枚举失败：" + ex.Message; return null; }
            if (all.Count == 0) { errmsg = "未发现该类型设备：" + t; return null; }
            if (string.IsNullOrEmpty(desc))
            {
                if (all.Count == 1) return all[0];
                errmsg = "该类型设备有多个，必须带 desc 参数：" + string.Join(" / ", all.Select(SafeDesc));
                return null;
            }
            var m = all.FirstOrDefault(x => SafeDesc(x) == desc);
            if (m == null)
            {
                errmsg = "未找到设备：" + desc + "。可用：" + string.Join(" / ", all.Select(SafeDesc));
                return null;
            }
            return m;
        }

        /// <summary>展开设备所有可读写参数（主设备 + 各通道）</summary>
        private List<DeviceParamRef> FlattenParams(InfoBase info, DeviceTypes dtype)
        {
            var list = new List<DeviceParamRef>();
            string desc = SafeDesc(info);
            var el = info.SourceDevice as PortElement;
            if (el != null)
            {
                foreach (var p in el.AvailableParameterNames())
                    list.Add(new DeviceParamRef { Desc = desc, Channel = el.ChannelName ?? "", Param = p });
                return list;
            }
            var po = info.SourceDevice as PortObject;
            if (po == null) return list;
            foreach (var p in po.AvailableParameterNames())
                list.Add(new DeviceParamRef { Desc = desc, Channel = "", Param = p });
            var epo = po as ElementPortObject;
            if (epo != null && epo.Channels != null)
            {
                foreach (var ch in epo.Channels)
                {
                    foreach (var p in ch.AvailableParameterNames())
                        list.Add(new DeviceParamRef { Desc = desc, Channel = ch.ChannelName ?? "", Param = p });
                }
            }
            return list;
        }

        private DeviceParamRef FindParam(InfoBase info, DeviceTypes dtype, string prop)
        {
            foreach (var r in FlattenParams(info, dtype))
            {
                if (r.Param != null && (r.Param.ParameterName == prop || r.Param.Description == prop))
                    return r;
            }
            return null;
        }

        [AiCommand("device-list", "列出设备（按类型）及占用状态", "type=<设备类型,如 锁相放大器/源表/温控/位移台,缺省列全部> params=1(同时列出每个设备可读写参数)")]
        private string DeviceList(Dictionary<string, string> args)
        {
            string typestr = GetArg(args, "type");
            bool withparams = GetArg(args, "params") == "1";

            DeviceTypes[] targets;
            if (string.IsNullOrEmpty(typestr))
            {
                targets = (DeviceTypes[])Enum.GetValues(typeof(DeviceTypes));
            }
            else
            {
                DeviceTypes dt;
                if (!Enum.TryParse(typestr, out dt))
                {
                    var valid = string.Join(" / ", ((DeviceTypes[])Enum.GetValues(typeof(DeviceTypes))).Select(x => x.ToString()));
                    return Err("未知设备类型：" + typestr + "。可用：" + valid);
                }
                targets = new[] { dt };
            }

            var result = new List<object>();
            foreach (var t in targets)
            {
                List<InfoBase> infos;
                try { infos = DeviceDispatcher.GetDevice(t); }
                catch { continue; }
                if (infos == null) continue;
                foreach (var info in infos)
                {
                    var entry = new Dictionary<string, object>
                    {
                        { "type", t.ToString() },
                        { "desc", SafeDesc(info) },
                        { "inUse", info.IsWriting }
                    };
                    if (withparams)
                    {
                        try
                        {
                            var refs = FlattenParams(info, t);
                            entry["params"] = refs.Select(p => new
                            {
                                channel = p.Channel,
                                name = p.Param.ParameterName,
                                desc = p.Param.Description,
                                readOnly = p.Param.IsReadOnly
                            }).ToList();
                        }
                        catch (Exception ex)
                        {
                            entry["paramError"] = ex.Message;
                        }
                    }
                    result.Add(entry);
                }
            }
            return Ok(new { count = result.Count, devices = result });
        }

        [AiCommand("device-get", "读取设备参数值（只读，不占用设备，实验运行中也可调用）", "type=<设备类型> desc=<设备描述,单台设备可省略> prop=<参数名或描述>")]
        private string DeviceGet(Dictionary<string, string> args)
        {
            DeviceTypes dtype; string errmsg;
            var info = ResolveDevice(args, out dtype, out errmsg);
            if (info == null) return Err(errmsg);
            string prop = GetArg(args, "prop");
            if (string.IsNullOrEmpty(prop)) return Err("缺少 prop 参数");
            var refp = FindParam(info, dtype, prop);
            if (refp == null)
            {
                string avail = "";
                try
                {
                    avail = string.Join(", ", FlattenParams(info, dtype)
                        .Select(p => (string.IsNullOrEmpty(p.Channel) ? "" : p.Channel + ".") + p.Param.ParameterName + "(" + p.Param.Description + ")"));
                }
                catch { }
                return Err("未找到参数：" + prop + "。可用参数：" + avail);
            }
            try
            {
                dynamic v = refp.Param.ReadValue();
                return Ok(new
                {
                    type = dtype.ToString(),
                    desc = refp.Desc,
                    channel = refp.Channel,
                    prop = refp.Param.ParameterName,
                    value = v == null ? "null" : v.ToString(),
                    valueType = refp.Param.ParamType != null ? refp.Param.ParamType.Name : ""
                });
            }
            catch (Exception ex)
            {
                return Err("读取失败（设备可能未连接）：" + ex.Message);
            }
        }

        [AiCommand("device-set", "写设备参数（会短暂独占设备；实验运行中会失败）", "type=<设备类型> desc=<设备描述> prop=<参数名> value=<值> confirm=true(安全模式下必需)。位移台目标位置写入永远禁止")]
        private string DeviceSet(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "写设备参数");
            if (block != null) return block;

            DeviceTypes dtype; string errmsg;
            var info = ResolveDevice(args, out dtype, out errmsg);
            if (info == null) return Err(errmsg);
            string prop = GetArg(args, "prop");
            if (string.IsNullOrEmpty(prop)) return Err("缺少 prop 参数");
            string value;
            if (!args.TryGetValue("value", out value)) return Err("缺少 value 参数");

            var refp = FindParam(info, dtype, prop);
            if (refp == null)
                return Err("未找到参数：" + prop + "。用 device-list params=1 查看参数列表");
            var param = refp.Param;
            if (param.IsReadOnly) return Err("该参数只读：" + param.ParameterName);

            // 位移台移动 = 危险操作，AI 永不执行（即使 full 模式）
            if (IsStageType(dtype) && param.ParameterName.IndexOf("Target", StringComparison.OrdinalIgnoreCase) >= 0)
                return Err("写入位移台目标位置会移动位移台，属于危险操作，必须由人工在位移台界面执行");

            if (info.IsWriting)
                return Err("设备正被使用（可能有实验在运行），请待其结束后重试");

            object val;
            try
            {
                var pt = param.ParamType;
                if (pt == null) return Err("参数类型未知：" + param.ParameterName);
                if (pt.IsEnum) val = Enum.Parse(pt, value, true);
                else if (pt == typeof(bool)) val = value.Trim().ToLower() == "true" || value == "1";
                else val = Convert.ChangeType(value, pt);
            }
            catch (Exception ex)
            {
                return Err("参数值无法转换为 " + param.ParamType.Name + "：" + ex.Message);
            }

            try { info.BeginUse(); }
            catch (Exception ex) { return Err("未能占用设备（正在被使用）：" + ex.Message); }
            try
            {
                param.WriteValue(val);
                Log("device-set " + dtype + " " + refp.Desc + (string.IsNullOrEmpty(refp.Channel) ? "" : "." + refp.Channel) + " " + param.ParameterName + "=" + value, LogLevel.Info);
                string readback;
                try { dynamic rv = param.ReadValue(); readback = rv == null ? "null" : rv.ToString(); }
                catch { readback = "(读回失败)"; }
                return Ok(new
                {
                    type = dtype.ToString(),
                    desc = refp.Desc,
                    channel = refp.Channel,
                    prop = param.ParameterName,
                    value,
                    readback
                });
            }
            catch (Exception ex)
            {
                Log("device-set 失败：" + ex.Message, LogLevel.Error);
                return Err("写入失败：" + ex.Message);
            }
            finally
            {
                try { info.EndUse(); } catch { }
            }
        }

        [AiCommand("apd-sample", "APD 光子计数采样：返回当前计数率(光子/秒)，用于判断探针是否在 NV 上", "desc=<APD描述,单台可省略> ms=<采样时长毫秒,默认3000,请求会阻塞等待>")]
        private string ApdSample(Dictionary<string, string> args)
        {
            List<InfoBase> all;
            try { all = DeviceDispatcher.GetDevice(DeviceTypes.光子计数器) ?? new List<InfoBase>(); }
            catch (Exception ex) { return Err("APD 枚举失败：" + ex.Message); }
            if (all.Count == 0) return Err("未发现光子计数器设备");
            string desc = GetArg(args, "desc");
            var info = string.IsNullOrEmpty(desc) ? all[0] : all.FirstOrDefault(x => SafeDesc(x) == desc);
            if (info == null)
                return Err("未找到 APD：" + desc + "。可用：" + string.Join(" / ", all.Select(SafeDesc)));
            var apd = info as APDInfo;
            if (apd == null) return Err("APD 信息类型不正确");
            if (info.IsWriting) return Err("APD 正被使用（可能有实验在运行），请待其结束后重试");

            int ms;
            int.TryParse(GetArg(args, "ms", "3000"), out ms);
            if (ms < 200) ms = 200;
            if (ms > 60000) ms = 60000;

            try { apd.BeginUse(); }
            catch (Exception ex) { return Err("未能占用 APD（正在被使用）：" + ex.Message); }
            try
            {
                apd.StartContinusSample();
                Thread.Sleep(300 + ms);
                double rate = apd.GetContinusSampleRatio();
                Log("apd-sample rate=" + rate, LogLevel.Info);
                return Ok(new { desc = SafeDesc(apd), countRate = rate, note = "单位：光子/秒；计数率明显升高通常表示探针在 NV 上" });
            }
            catch (Exception ex)
            {
                Log("apd-sample 失败：" + ex.Message, LogLevel.Error);
                return Err("采样失败：" + ex.Message);
            }
            finally
            {
                try { apd.EndContinusSample(); } catch { }
                try { apd.EndUse(); } catch { }
            }
        }

        [AiCommand("laser-on", "打开激光（PulseBlaster 通道5 的 300ms 脉冲序列）", "duty=<占空比0-1,默认1> desc=<PulseBlaster描述,单台可省略> confirm=true(安全模式下必需)")]
        private string AiLaserOn(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "打开激光");
            if (block != null) return block;

            double duty;
            if (!double.TryParse(GetArg(args, "duty", "1"), out duty))
                return Err("duty 必须是 0-1 的数字");
            if (duty < 0 || duty > 1) return Err("duty 必须在 0-1 之间");

            var pb = GetPulseBlaster(GetArg(args, "desc"));
            if (pb == null) return Err("未找到 PulseBlaster 设备，用 device-list type=PulseBlaster 检查连接");
            if (pb.IsWriting) return Err("PulseBlaster 正被使用（可能有实验在运行），请待其结束后重试");

            try { pb.BeginUse(); }
            catch (Exception ex) { return Err("未能占用 PulseBlaster（正在被使用）：" + ex.Message); }
            try
            {
                new LaserOn().CoreMethod(new List<object> { duty }, pb);
                Log("laser-on duty=" + duty, LogLevel.Info);
                return Ok("激光已打开（300ms 脉冲序列，通道5），duty=" + duty.ToString("0.##"));
            }
            catch (Exception ex)
            {
                Log("laser-on 失败：" + ex.Message, LogLevel.Error);
                return Err("打开激光失败：" + ex.Message);
            }
            finally
            {
                try { pb.EndUse(); } catch { }
            }
        }

        [AiCommand("laser-off", "关闭激光（停止 PulseBlaster 序列，任何时候都允许）", "desc=<PulseBlaster描述,单台可省略>")]
        private string AiLaserOff(Dictionary<string, string> args)
        {
            string msg;
            bool ok = LaserOffInternal(GetArg(args, "desc"), out msg);
            return ok ? Ok("激光已关闭") : Err(msg);
        }

        /// <summary>内部关激光（estop 也使用）</summary>
        private bool LaserOffInternal(string desc, out string msg)
        {
            msg = "";
            var pb = GetPulseBlaster(desc);
            if (pb == null) { msg = "未找到 PulseBlaster 设备"; return false; }
            if (pb.IsWriting) { msg = "PulseBlaster 正被使用（可能有实验在运行），未能关闭激光"; return false; }
            try { pb.BeginUse(); }
            catch (Exception ex) { msg = "未能占用 PulseBlaster：" + ex.Message; return false; }
            try
            {
                new LaserOff().CoreMethod(new List<object>(), pb);
                Log("laser-off", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Log("laser-off 失败：" + ex.Message, LogLevel.Error);
                msg = "关闭激光失败：" + ex.Message;
                return false;
            }
            finally
            {
                try { pb.EndUse(); } catch { }
            }
        }

        private PulseBlasterInfo GetPulseBlaster(string desc)
        {
            List<InfoBase> all;
            try { all = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster) ?? new List<InfoBase>(); }
            catch { return null; }
            if (all.Count == 0) return null;
            if (string.IsNullOrEmpty(desc)) return all[0] as PulseBlasterInfo;
            return all.FirstOrDefault(x => SafeDesc(x) == desc) as PulseBlasterInfo;
        }

        [AiCommand("camera-open", "打开相机实时预览窗口（打开后会独占相机设备直到窗口关闭；若相机正被实验使用则拒绝）", "desc=<相机描述,单台可省略> confirm=true(安全模式下必需)")]
        private string CameraOpen(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "打开相机预览");
            if (block != null) return block;

            var cam = ResolveCamera(GetArg(args, "desc"), out string errmsg);
            if (cam == null) return Err(errmsg);

            var win = MainWindow.Handle;
            if (win == null) return Err("主窗口未就绪");

            string failmsg = "";
            string state = "";
            try
            {
                win.Dispatcher.Invoke(() =>
                {
                    if (cam.DisplayWindow != null)
                    {
                        cam.DisplayWindow.Show();
                        state = "窗口已存在，已激活";
                        return;
                    }
                    if (cam.IsWriting)
                    {
                        failmsg = "相机正被使用（可能有实验在运行），请待其结束后重试";
                        return;
                    }
                    var w = new CameraWindow(MainWindow.Dev_CameraPage, cam);
                    // 构造失败（相机被占用）时 CameraWindow 内部会自行关闭并置回 null
                    cam.DisplayWindow = w;
                    w.Show();
                    state = "已打开";
                });
            }
            catch (Exception ex)
            {
                return Err("打开相机窗口失败：" + ex.Message);
            }
            if (!string.IsNullOrEmpty(failmsg)) return Err(failmsg);
            if (cam.DisplayWindow == null)
                return Err("相机正被使用，预览窗口未能打开（屏幕上已弹出提示）");

            Log("camera-open " + SafeDesc(cam) + "（" + state + "）", LogLevel.Info);
            return Ok(new
            {
                desc = SafeDesc(cam),
                state,
                hint = "预览窗口会一直占用相机直到关闭；运行需要相机的实验（如 AFM 扫描）前请先用 camera-close 关窗"
            });
        }

        [AiCommand("camera-close", "关闭相机预览窗口并释放相机设备（关闭操作永远允许）", "desc=<相机描述,单台可省略>")]
        private string CameraClose(Dictionary<string, string> args)
        {
            var cam = ResolveCamera(GetArg(args, "desc"), out string errmsg);
            if (cam == null) return Err(errmsg);

            var win = MainWindow.Handle;
            if (win == null) return Err("主窗口未就绪");

            bool wasOpen = false;
            try
            {
                win.Dispatcher.Invoke(() =>
                {
                    var w = cam.DisplayWindow;
                    if (w == null) return;
                    wasOpen = true;
                    // 与界面关窗 / 关闭设备时的处理一致：先停取帧线程（释放设备），再关窗口
                    w.CancelThread();
                    w.Close();
                    cam.DisplayWindow = null;
                });
            }
            catch (Exception ex)
            {
                return Err("关闭相机窗口失败：" + ex.Message);
            }
            if (!wasOpen)
                return Err("该相机的预览窗口未打开：" + SafeDesc(cam));

            Log("camera-close " + SafeDesc(cam), LogLevel.Info);
            return Ok("相机预览窗口已关闭，相机设备已释放");
        }

        /// <summary>按 desc 解析目标相机；cam==null 时 errmsg 为原因</summary>
        private CameraInfo ResolveCamera(string desc, out string errmsg)
        {
            errmsg = "";
            List<InfoBase> all;
            try { all = DeviceDispatcher.GetDevice(DeviceTypes.相机) ?? new List<InfoBase>(); }
            catch (Exception ex) { errmsg = "相机枚举失败：" + ex.Message; return null; }
            if (all.Count == 0)
            {
                errmsg = "未发现相机设备。请先在相机页面连接相机，或用 auto-connect 重新搜索";
                return null;
            }
            if (string.IsNullOrEmpty(desc))
            {
                if (all.Count == 1) return all[0] as CameraInfo;
                errmsg = "该类型设备有多个，必须带 desc 参数：" + string.Join(" / ", all.Select(SafeDesc));
                return null;
            }
            var m = all.FirstOrDefault(x => SafeDesc(x) == desc);
            if (m == null)
            {
                errmsg = "未找到相机：" + desc + "。可用：" + string.Join(" / ", all.Select(SafeDesc));
                return null;
            }
            return m as CameraInfo;
        }

        [AiCommand("auto-connect", "自动连接设备：重新搜索并连接全部已配置设备（等同主界面右上角「自动连接」按钮；会替换当前设备列表）", "无参数。安全模式下需 confirm=true；实验运行中或任何设备被占用时禁止。注意：请求可能阻塞 30~60 秒，调用方超时请设 120 秒以上")]
        private string AutoConnect(Dictionary<string, string> args)
        {
            string block = NeedConfirm(args, "自动连接设备");
            if (block != null) return block;

            var exp = CurrentExp();
            if (exp != null && !exp.IsExpEnd)
                return Err("实验运行中不能重新连接设备（重连会替换设备列表，导致运行中实验丢失设备引用），请先 stop-experiment 再重试");

            var busy = FindInUseDevice();
            if (busy != null)
                return Err("有设备正在使用（" + SafeDesc(busy) + "，可能是相机预览窗口或 APD 采样等），请先关闭（如 camera-close / 停止采样）再重试");

            string result;
            try
            {
                result = DeviceDispatcher.AppendDevices();
            }
            catch (Exception ex)
            {
                Log("auto-connect 失败：" + ex.Message, LogLevel.Error);
                return Err("设备连接失败：" + ex.Message);
            }
            Log("auto-connect 完成：" + result.Replace("\n", " | "), LogLevel.Info);
            return Ok(new
            {
                result,
                hint = "已连接设备列表可用 device-list 查看"
            });
        }

        /// <summary>返回第一个被占用（IsWriting）的设备，没有则 null</summary>
        private InfoBase FindInUseDevice()
        {
            foreach (var t in (DeviceTypes[])Enum.GetValues(typeof(DeviceTypes)))
            {
                List<InfoBase> all;
                try { all = DeviceDispatcher.GetDevice(t) ?? new List<InfoBase>(); }
                catch { continue; }
                foreach (var d in all)
                    if (d.IsWriting) return d;
            }
            return null;
        }

        #endregion
    }
}
