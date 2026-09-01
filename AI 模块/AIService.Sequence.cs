using System;
using System.Collections.Generic;
using System.Linq;
using ODMR_Lab;
using ODMR_Lab.实验部分.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 序列全局脉冲变量
    /// 安全设计：
    /// - 全局脉冲表（Sequences\GlobalPulses.userdat）决定序列中同名脉冲的实际运行长度
    /// - 实验运行中禁止修改（实验会并发读写该表）
    /// - set-seq-var 在安全模式下需 confirm=true；修改后自动落盘
    /// </summary>
    public partial class AIService
    {
        #region 序列指令

        [AiCommand("list-seq-vars", "列出全部全局脉冲变量（脉冲名/长度/锁定标志）。序列文件中同名脉冲段运行时以本表长度为准", "无参数。返回 name(脉冲名)/length(长度,ns)/locked(锁定标志,仅表示删除时是否警告,不影响长度修改)")]
        private string ListSeqVars(Dictionary<string, string> args)
        {
            var configs = GlobalPulseParams.GlobalPulseConfigs;
            if (configs == null || configs.Count == 0)
                return Ok(new { count = 0, pulses = new object[0], hint = "全局脉冲表为空（Sequences\\GlobalPulses.userdat 不存在或为空）" });
            var pulses = configs.Select(p => new { name = p.PulseName, length = p.PulseLength, locked = p.IsLocked }).ToList();
            return Ok(new
            {
                count = pulses.Count,
                pulses,
                unit = "ns",
                hint = "用 set-seq-var name=<脉冲名> length=<新长度> 修改；locked=true 为实验核心脉冲，修改长度前请确认实验兼容"
            });
        }

        [AiCommand("set-seq-var", "设置全局脉冲变量的长度（自动写入 Sequences\\GlobalPulses.userdat；之后启动含该脉冲的序列时即用新长度）", "name=<脉冲名> length=<非负整数,ns> confirm=true(安全模式下必需)。实验运行中禁止修改；脉冲名来自 list-seq-vars")]
        private string SetSeqVar(Dictionary<string, string> args)
        {
            var exp = CurrentExp();
            if (exp != null && !exp.IsExpEnd)
                return Err("实验运行中不能修改全局脉冲表（实验可能正在并发读写），请先 stop-experiment 再重试");

            string name = GetArg(args, "name");
            if (string.IsNullOrEmpty(name)) return Err("缺少 name 参数");
            int length;
            if (!int.TryParse(GetArg(args, "length"), out length) || length < 0)
                return Err("length 必须是非负整数（单位 ns）");
            if (GlobalPulseParams.GlobalPulseConfigs == null || !GlobalPulseParams.ExistsInGlobal(name))
            {
                var available = GlobalPulseParams.GlobalPulseConfigs != null
                    ? GlobalPulseParams.GlobalPulseConfigs.Select(p => p.PulseName).ToList()
                    : new List<string>();
                return Err("未找到脉冲：" + name + "。可用：" + string.Join(" / ", available) + "（用 list-seq-vars 查看）");
            }

            string block = NeedConfirm(args, "修改全局脉冲长度 " + name);
            if (block != null) return block;

            int old;
            bool locked;
            try
            {
                old = GlobalPulseParams.GetGlobalPulseLength(name);
                locked = GlobalPulseParams.GlobalPulseConfigs.First(p => p.PulseName == name).IsLocked;
            }
            catch (Exception ex)
            {
                return Err("读取当前长度失败：" + ex.Message);
            }

            try
            {
                GlobalPulseParams.SetGlobalPulseLength(name, length);
            }
            catch (Exception ex)
            {
                return Err("修改脉冲长度失败：" + ex.Message);
            }

            Log("set-seq-var " + name + ": " + old + " -> " + length + " ns" + (locked ? "（锁定核心脉冲）" : ""), LogLevel.Info);
            return Ok(new
            {
                name,
                oldLength = old,
                newLength = length,
                unit = "ns",
                locked,
                persisted = "Sequences\\GlobalPulses.userdat",
                hint = "已生效并自动保存到文件；含该脉冲的序列下次运行时使用新长度" + (locked ? "。注意：该脉冲为锁定核心脉冲，长度变化可能影响实验物理过程（如 Rabi 翻转角）" : "")
            });
        }

        #endregion
    }
}
