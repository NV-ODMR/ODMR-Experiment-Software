using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeHelper;
using ODMR_Lab;
using ODMR_Lab.基本控件;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验部分.ODMR实验.参数;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 数据与诊断
    /// </summary>
    public partial class AIService
    {
        #region 数据指令

        [AiCommand("list-data-files", "列出实验数据文件(.userdat)", "dir=<目录,默认程序目录> extype=<实验类型过滤,如 ODMR实验>")]
        private string ListDataFiles(Dictionary<string, string> args)
        {
            string dir = GetArg(args, "dir", Environment.CurrentDirectory);
            if (!Directory.Exists(dir)) return Err("目录不存在：" + dir);
            string extype = GetArg(args, "extype");
            string[] files;
            try { files = Directory.GetFiles(dir, "*.userdat", SearchOption.AllDirectories); }
            catch (Exception ex) { return Err("目录扫描失败：" + ex.Message); }

            var items = new List<string[]>();
            foreach (var f in files)
            {
                FileInfo fi = null;
                try { fi = new FileInfo(f); }
                catch { continue; }
                string type = "";
                try
                {
                    var d = FileObject.ReadDescription(f);
                    if (d != null && d.ContainsKey("实验类型")) type = d["实验类型"] ?? "";
                }
                catch { }
                if (!string.IsNullOrEmpty(extype) && type != extype) continue;
                items.Add(new[] { f, fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), fi.Length.ToString(), type });
            }

            var list = items
                .OrderByDescending(x => x[1])
                .Take(200)
                .Select(x => new { path = x[0], time = x[1], sizeKB = long.Parse(x[2]) / 1024, type = x[3] })
                .ToList();
            return Ok(new { dir, count = list.Count, note = "最多返回最近200个文件", files = list });
        }

        [AiCommand("export-data", "把实验数据文件(.userdat)导出为 CSV（供外部分析）", "file=<.userdat 文件路径> outcsv=<输出路径,默认与源文件同名的.csv>")]
        private string ExportData(Dictionary<string, string> args)
        {
            string file = GetArg(args, "file");
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                return Err("文件不存在：" + file + "（用 list-data-files 查看数据文件）");

            var fob = new SequenceFileExpObject();
            bool ok;
            try { ok = fob.ReadFromFile(file); }
            catch (Exception ex) { return Err("读取数据文件失败：" + ex.Message); }
            if (!ok) return Err("文件读取失败（可能不是 ODMR 实验数据文件）：" + file);

            string outcsv = GetArg(args, "outcsv");
            if (string.IsNullOrEmpty(outcsv)) outcsv = Path.ChangeExtension(file, ".csv");

            int n1d = 0;
            int n2d = 0;
            try
            {
                using (var sw = new StreamWriter(outcsv, false, new UTF8Encoding(true)))
                {
                    sw.WriteLine("# 实验: " + fob.ODMRExperimentGroupName + ":" + fob.ODMRExperimentName);
                    if (fob.InputParams != null)
                        foreach (var p in fob.InputParams)
                            sw.WriteLine("# 输入: " + (p.Description ?? "") + " = " + ParamB.GetUnknownParamValueToString(p) + "  [" + p.PropertyName + "]");
                    if (fob.OutputParams != null)
                        foreach (var p in fob.OutputParams)
                            sw.WriteLine("# 输出: " + (p.Description ?? "") + " = " + ParamB.GetUnknownParamValueToString(p));
                    if (fob.DeviceList != null)
                        foreach (var d in fob.DeviceList)
                            sw.WriteLine("# 设备: " + (d.Value.Description ?? "") + " = " + ParamB.GetUnknownParamValueToString(d.Value));
                    sw.WriteLine();

                    if (fob.D1ChartDatas != null)
                        foreach (var d in fob.D1ChartDatas)
                        {
                            var nd = d as NumricChartData1D;
                            if (nd == null || nd.Data == null) continue;
                            sw.WriteLine("## 曲线: " + d.GroupName + " / " + nd.Name + " (轴: " + nd.DataAxisType + ")");
                            sw.WriteLine("index," + nd.Name);
                            for (int i = 0; i < nd.Data.Count; i++)
                                sw.WriteLine((i + 1) + "," + nd.Data[i]);
                            sw.WriteLine();
                            n1d++;
                        }

                    if (fob.D2ChartDatas != null)
                        foreach (var d in fob.D2ChartDatas)
                        {
                            var dd = d.Data;
                            if (dd == null || dd.XCounts <= 0 || dd.YCounts <= 0) continue;
                            sw.WriteLine("## 曲面: " + d.GroupName + " (X: " + dd.XName + ", Y: " + dd.YName + ", Z: " + dd.ZName + ")");
                            sw.WriteLine("yIndex," + dd.XName);
                            for (int j = 0; j < dd.YCounts; j++)
                            {
                                double yval = dd.YLo + (double)j * (dd.YHi - dd.YLo) / Math.Max(1, dd.YCounts - 1);
                                var row = new List<string> { j + "," + yval.ToString("0.###") };
                                for (int i = 0; i < dd.XCounts; i++)
                                    row.Add(dd.GetValue(j, i).ToString("0.###"));
                                sw.WriteLine(string.Join(",", row));
                            }
                            sw.WriteLine();
                            n2d++;
                        }
                }
                Log("export-data: " + file + " -> " + outcsv, LogLevel.Info);
                return Ok(new
                {
                    outcsv,
                    curves1d = n1d,
                    surfaces2d = n2d,
                    note = "1D 曲线为 index,value 行；2D 曲面每行一个 Y 扫描（前两列为 Y 序号与 Y 值，其余为各 X 处的 Z 值）；时间序列曲线不导出"
                });
            }
            catch (Exception ex)
            {
                return Err("CSV 导出失败：" + ex.Message);
            }
        }

        [AiCommand("read-errlog", "读取程序异常日志(errlog.txt)最后 N 行", "lines=<行数,默认50,最大1000>")]
        private string ReadErrlog(Dictionary<string, string> args)
        {
            string path = Environment.CurrentDirectory + "\\errlog.txt";
            if (!File.Exists(path)) return Ok("无异常日志（errlog.txt 不存在）");
            int lines;
            int.TryParse(GetArg(args, "lines", "50"), out lines);
            if (lines < 1) lines = 1;
            if (lines > 1000) lines = 1000;
            string[] all;
            try { all = File.ReadAllLines(path); }
            catch (Exception ex) { return Err("读取日志失败：" + ex.Message); }
            var tail = all.Skip(Math.Max(0, all.Length - lines)).ToList();
            return Ok(new { count = tail.Count, lines = tail });
        }

        [AiCommand("save-params", "保存全部界面参数到文件（等同于程序关闭时的保存）", "无参数")]
        private string SaveParams(Dictionary<string, string> args)
        {
            if (MainWindow.Handle == null) return Err("主窗口未就绪");
            try
            {
                MainWindow.Handle.Dispatcher.Invoke(() => ParamManager.SaveParams());
                Log("save-params", LogLevel.Info);
                return Ok("界面参数已保存");
            }
            catch (Exception ex)
            {
                return Err("保存失败：" + ex.Message);
            }
        }

        [AiCommand("load-params", "从文件恢复全部界面参数（会覆盖当前界面参数值）", "无参数")]
        private string LoadParams(Dictionary<string, string> args)
        {
            if (MainWindow.Handle == null) return Err("主窗口未就绪");
            try
            {
                MainWindow.Handle.Dispatcher.Invoke(() => ParamManager.ReadAndLoadParams());
                Log("load-params", LogLevel.Info);
                return Ok("界面参数已恢复");
            }
            catch (Exception ex)
            {
                return Err("恢复失败：" + ex.Message);
            }
        }

        #endregion
    }
}
