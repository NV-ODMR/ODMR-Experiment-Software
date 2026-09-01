using System;
using System.Collections.Generic;
using System.Windows;
using ODMR_Lab;

namespace ODMRLab.Services
{
    /// <summary>
    /// AI 指令 - 程序界面控制
    /// 窗口操作（最大化/最小化等）与页面切换均为无害 UI 操作，不需要安全模式确认
    /// </summary>
    public partial class AIService
    {
        #region 界面控制指令

        [AiCommand("window-control", "主窗口控制：最大化/最小化/还原/激活到前台", "action=maximize|minimize|restore|activate")]
        private string WindowControl(Dictionary<string, string> args)
        {
            var win = MainWindow.Handle;
            if (win == null) return Err("主窗口未就绪");
            string action = GetArg(args, "action").Trim().ToLower();
            if (action != "maximize" && action != "minimize" && action != "restore" && action != "activate")
                return Err("action 必须是 maximize / minimize / restore / activate 之一");

            try
            {
                win.Dispatcher.Invoke(() =>
                {
                    switch (action)
                    {
                        case "maximize":
                            win.WindowState = WindowState.Maximized;
                            break;
                        case "minimize":
                            win.WindowState = WindowState.Minimized;
                            break;
                        case "restore":
                            win.WindowState = WindowState.Normal;
                            break;
                        case "activate":
                            if (win.WindowState == WindowState.Minimized)
                                win.WindowState = WindowState.Normal;
                            win.Activate();
                            break;
                    }
                });
                Log("window-control: " + action, LogLevel.Info);
                return Ok("窗口操作完成：" + action);
            }
            catch (Exception ex)
            {
                return Err("窗口操作失败：" + ex.Message);
            }
        }

        [AiCommand("list-pages", "列出可用页面名称（open-page 的 page 参数取值）", "无参数")]
        private string ListPages(Dictionary<string, string> args)
        {
            if (MainWindow.Handle == null) return Err("主窗口未就绪");
            return Ok(new { count = MainWindow.AIPageNames.Count, pages = MainWindow.AIPageNames });
        }

        [AiCommand("open-page", "打开指定页面（同步更新程序菜单与内容区）", "page=<页面名>，可用名称见 list-pages（如 ODMR实验、光子计数器、设备参数监测）")]
        private string OpenPage(Dictionary<string, string> args)
        {
            var win = MainWindow.Handle;
            if (win == null) return Err("主窗口未就绪");
            string page = GetArg(args, "page");
            if (string.IsNullOrEmpty(page))
                return Err("缺少 page 参数，用 list-pages 查看可用页面名");

            bool ok = false;
            try
            {
                win.Dispatcher.Invoke(() => ok = win.OpenPage(page));
            }
            catch (Exception ex)
            {
                return Err("打开页面失败：" + ex.Message);
            }
            if (!ok)
                return Err("未知页面名：" + page + "。用 list-pages 查看可用名称");
            Log("open-page: " + page, LogLevel.Info);
            return Ok("已打开页面：" + page);
        }

        #endregion
    }
}
