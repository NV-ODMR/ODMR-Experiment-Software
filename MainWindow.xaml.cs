using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodeHelper;
using Controls;
using System.IO;
using System.Threading;
using ODMR_Lab.数据处理;
using ODMR_Lab.IO操作;
using Controls.Windows;
using ODMR_Lab.设备部分;
using ODMR_Lab.共享剪切板;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HardWares.APD.Exclitas_SPCM_AQRH;

namespace ODMR_Lab
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public static MainWindow Handle { get; set; } = null;

        public static PageBase CurrentPage { get; set; } = null;

        #region 设备页面
        public static 设备部分.其他设备.DevicePage Dev_OtherDevPage = new 设备部分.其他设备.DevicePage();

        public static 设备部分.位移台部分.DevicePage Dev_MoversPage = new 设备部分.位移台部分.DevicePage();

        public static 设备部分.相机_翻转镜.DevicePage Dev_CameraPage = new 设备部分.相机_翻转镜.DevicePage();

        public static 设备部分.光子探测器.DevicePage Dev_APDPage = new 设备部分.光子探测器.DevicePage();

        #endregion

        #region 实验页面
        /// <summary>
        /// 设备参数监测
        /// </summary>
        public static 实验部分.设备参数监测.DisplayPage Exp_DevParamListenPage = new 实验部分.设备参数监测.DisplayPage();

        /// <summary>
        /// 设备参数设置
        /// </summary>
        public static 实验部分.设备参数面板.DisplayPage Exp_DevParamSetPage = new 实验部分.设备参数面板.DisplayPage();

        /// <summary>
        /// 样品定位
        /// </summary>
        public static 样品定位.DisplayPage Exp_SamplePage = new 样品定位.DisplayPage();

        /// <summary>
        /// 场效应管测量
        /// </summary>
        public static 场效应器件测量.DisplayPage Exp_SourcePage = new 场效应器件测量.DisplayPage();

        /// <summary>
        /// 自定义算法
        /// </summary>
        public static 自定义算法.DisplayPage Exp_AlgorithmPage = new 自定义算法.DisplayPage();

        /// <summary>
        /// 位移台控制
        /// </summary>
        public static 位移台界面.DisplayPage Exp_StagePage = new 位移台界面.DisplayPage();

        /// <summary>
        /// 位移台控制
        /// </summary>
        public static 序列编辑器.DisplayPage Exp_SequenceEditPage = new 序列编辑器.DisplayPage();

        /// <summary>
        /// 序列实验
        /// </summary>
        public static 实验部分.ODMR实验.DisplayPage Exp_SequencePage = new 实验部分.ODMR实验.DisplayPage(true);

        /// <summary>
        /// Trace实验
        /// </summary>
        public static 激光控制.DisplayPage Exp_TracePage = new 激光控制.DisplayPage();

        #endregion

        #region 扩展页面
        /// <summary>
        /// Python管理器
        /// </summary>
        public static Python管理器.ExtPage Ext_PythonPage = new Python管理器.ExtPage();

        /// <summary>
        /// Python管理器
        /// </summary>
        public static 数据记录.ExtPage Ext_NotePage = new 数据记录.ExtPage();

        #endregion

        #region 数据预览与处理窗口
        public static DataViewingWindow DataWindow = new DataViewingWindow();
        #endregion

        public MainWindow()
        {
            Mutex mm = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isrunning);
            if (!isrunning)
            {
                Environment.Exit(0);
            }
            Handle = this;
            var dwinhelper = new WindowInteropHelper(this);
            SetCurrentProcessExplicitAppUserModelID("MainWindow");
            InitializeComponent();
            ExpandableBar bar = new ExpandableBar();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterCloseWindow(this, MinimizeBtn, MaximizeBtn, null, PinBtn, 5, 30);

            #region 调用页面的初始化方法
            var pages = GetType().GetFields().Where(x => typeof(PageBase).IsAssignableFrom(x.FieldType));
            foreach (var item in pages)
            {
                (item.GetValue(this) as PageBase)?.Init();
            }
            #endregion

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEvent;
        }


        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭吗?", MessageBoxButton.YesNo, owner: this, AllowKeyboardInput: true, TopMost: true) == MessageBoxResult.Yes)
            {
                bool canclose = true;
                Thread t = new Thread(() =>
                  {
                      MessageWindow win = null;
                      Dispatcher.Invoke(() =>
                      {
                          win = new MessageWindow("提示", "正在保存参数...", MessageBoxButton.OK, false, false);
                          win.Owner = this;
                          win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                          IsEnabled = false;
                          win.Show();
                      });
                      //保存界面参数
                      ParamManager.SaveParams(win);

                      #region 清除剪切板缓存文件
                      try
                      {
                          var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "TempPasteFile"), "*", SearchOption.AllDirectories);
                          foreach (var item in files)
                          {
                              try
                              {
                                  File.Delete(item);
                              }
                              catch (Exception)
                              {
                              }
                          }
                      }
                      catch (Exception)
                      {
                      }
                      #endregion

                      Dispatcher.Invoke(() =>
                      {
                          win.Close();
                          win = new MessageWindow("提示", "正在关闭设备...", MessageBoxButton.OK, false, false);
                          win.Owner = this;
                          win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                          IsEnabled = false;
                          win.Show();
                      });
                      canclose = DeviceDispatcher.CloseDevicesAndSave();
                      Dispatcher.Invoke(() =>
                      {
                          win.Close();
                          if (canclose)
                          {
                              #region 调用页面的中止方法
                              var pages = GetType().GetFields().Where(x => typeof(PageBase).IsAssignableFrom(x.FieldType));
                              foreach (var item in pages)
                              {
                                  (item.GetValue(this) as PageBase).CloseBehaviour();
                              }
                              #endregion
                              Close();
                              Environment.Exit(0);
                          }
                          else
                          {
                              IsEnabled = true;
                          }
                      });
                  });
                t.Start();
            }
        }

        private void UnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs e)
        {
            PrintStacktrace((Exception)e.ExceptionObject);
            MessageWindow.ShowTipWindow("程序运行出现异常,即将退出,异常原因：\n" + ((Exception)e.ExceptionObject).Message, this);
            //保存界面参数
            ParamManager.SaveParams();
            DeviceDispatcher.CloseDevicesAndSave();
            #region 调用页面的中止方法
            var pages = GetType().GetFields().Where(x => typeof(PageBase).IsAssignableFrom(x.FieldType));
            foreach (var item in pages)
            {
                (item.GetValue(this) as PageBase).CloseBehaviour();
            }
            #endregion
        }

        private void PrintStacktrace(Exception e)
        {
            using (StreamWriter w = new StreamWriter(File.Open(Environment.CurrentDirectory + "\\errlog.txt", FileMode.Append)))
            {
                w.WriteLine(DateTime.Now.ToLongTimeString());
                w.WriteLine(e.Message);
                w.WriteLine(e.StackTrace);
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            #region 调用页面的参数同步方法
            var pages = GetType().GetFields().Where(x => typeof(PageBase).IsAssignableFrom(x.FieldType));
            foreach (var item in pages)
            {
                (item.GetValue(this) as PageBase).UpdateParam();
            }
            #endregion
            #region 自动连接设备
            UpdateLayout();
            Thread t = new Thread(() =>
            {
                try
                {
                    MessageWindow window = null;
                    Dispatcher.Invoke(() =>
                    {
                        IsEnabled = false;
                        window = new MessageWindow("自动连接", "正在自动连接设备...", MessageBoxButton.OK, false, false);
                        window.Owner = this;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        window.Show();
                    });
                    string connectresult = DeviceDispatcher.ScanDevices(window);
                    Dispatcher.Invoke(() =>
                    {
                        window.Close();
                        window = new MessageWindow("设置参数", "正在读取保存参数...", MessageBoxButton.OK, false, false);
                        window.Owner = this;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        window.Show();
                    });

                    #region 加载参数
                    ParamManager.ReadAndLoadParams(window);
                    #endregion

                    Dispatcher.Invoke(() =>
                    {
                        window.Close();
                        MessageWindow.ShowMessageBox("自动连接", connectresult, MessageBoxButton.OK, true, owner: this);
                        IsEnabled = true;
                    });

                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageWindow.ShowTipWindow("程序环境安装出现问题,即将退出：\n" + ex.Message, Window.GetWindow(this));
                    });
                    Environment.Exit(0);
                    return;
                }
            });
            t.Start();
            #endregion
        }


        ClipBoardWindow ClipboardWindow = null;

        public static void SetAppIdForWindow(Window window, string appId)
        {
        }

        // 导入 Windows API 中的设置任务栏应用程序 ID 的方法
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        /// <summary>
        /// 显示次级菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowSubMenu(object sender, RoutedEventArgs e)
        {
            DeviceList.Visibility = Visibility.Collapsed;
            ExpList.Visibility = Visibility.Collapsed;
            ExternalList.Visibility = Visibility.Collapsed;
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "设备")
            {
                DeviceList.Visibility = Visibility.Visible;
            }
            if (btn.Text == "实验")
            {
                ExpList.Visibility = Visibility.Visible;
            }
            if (btn.Text == "数据")
            {
                DataWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                DataWindow.WindowState = WindowState.Normal;
                DataWindow.Activate();
                DataWindow.Show();
                var dwinhelper = new WindowInteropHelper(this);
                SetCurrentProcessExplicitAppUserModelID("MainWindow");
            }
            if (btn.Text == "扩展")
            {
                ExternalList.Visibility = Visibility.Visible;
            }
            if (btn.Text == "共享剪切板")
            {
                if (ClipboardWindow == null)
                {
                    ClipboardWindow = new ClipBoardWindow();
                }
                ClipboardWindow.WindowState = WindowState.Normal;
                ClipboardWindow.Activate();
                ClipboardWindow.Show();
                var cwinhelper = new WindowInteropHelper(this);
                SetCurrentProcessExplicitAppUserModelID("MainWindow");

            }
        }

        private void ShowDeviceContent(object sender, RoutedEventArgs e)
        {
            if (CurrentPage != null)
            {
                PageContent.Children.Clear();
            }
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "其他设备")
            {
                CurrentPage = Dev_OtherDevPage;
                AddPageToView(Dev_OtherDevPage);
            }
            if (btn.Text == "位移台")
            {
                CurrentPage = Dev_MoversPage;
                AddPageToView(Dev_MoversPage);
            }
            if (btn.Text == "相机")
            {
                CurrentPage = Dev_CameraPage;
                AddPageToView(Dev_CameraPage);
            }
            if (btn.Text == "光子计数器")
            {
                CurrentPage = Dev_APDPage;
                AddPageToView(Dev_APDPage);
            }
        }

        private void ShowExpContent(object sender, RoutedEventArgs e)
        {
            if (CurrentPage != null)
            {
                PageContent.Children.Clear();
            }
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "设备参数监测")
            {
                CurrentPage = Exp_DevParamListenPage;
                AddPageToView(Exp_DevParamListenPage);
            }
            if (btn.Text == "设备参数设置")
            {
                CurrentPage = Exp_DevParamSetPage;
                AddPageToView(Exp_DevParamSetPage);
            }
            if (btn.Text == "Trace")
            {
                CurrentPage = Exp_TracePage;
                AddPageToView(Exp_TracePage);
            }
            if (btn.Text == "序列编辑器")
            {
                CurrentPage = Exp_SequenceEditPage;
                AddPageToView(Exp_SequenceEditPage);
            }
            if (btn.Text == "ODMR实验")
            {
                CurrentPage = Exp_SequencePage;
                AddPageToView(Exp_SequencePage);
            }
            if (btn.Text == "位移台控制界面")
            {
                CurrentPage = Exp_StagePage;
                AddPageToView(Exp_StagePage);
            }

            if (btn.Text == "样品定位")
            {
                CurrentPage = Exp_SamplePage;
                AddPageToView(Exp_SamplePage);
            }

            if (btn.Text == "场效应器件测量")
            {
                CurrentPage = Exp_SourcePage;
                AddPageToView(Exp_SourcePage);
            }

            if (btn.Text == "自定义算法")
            {
                CurrentPage = Exp_AlgorithmPage;
                AddPageToView(Exp_AlgorithmPage);
            }

        }

        private void ShowExternalContent(object sender, RoutedEventArgs e)
        {
            if (CurrentPage != null)
            {
                PageContent.Children.Clear();
            }
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "Python管理器")
            {
                CurrentPage = Ext_PythonPage;
                AddPageToView(Ext_PythonPage);
            }
            if (btn.Text == "数据记录")
            {
                CurrentPage = Ext_NotePage;
                AddPageToView(Ext_NotePage);
            }
        }

        public void AddPageToView(PageBase page)
        {
            if (page != CurrentPage) return;
            PageContent.Children.Clear();
            if (!page.IsDisplayedInPage)
            {
                DisplayInWindowBtn.Visibility = Visibility.Hidden;
                PageContent.Children.Add(new TextBlock()
                {
                    Text = "正在独立窗口中显示...",
                    FontSize = 40,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });
                DisplayInWindowBtn.Visibility = Visibility.Hidden;
                return;
            }
            DisplayInWindowBtn.Visibility = Visibility.Visible;
            PageContent.Children.Add(page);
            return;
        }

        private void AutoConnect(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                MessageWindow win = null;
                Dispatcher.Invoke(() =>
                {
                    win = new MessageWindow("自动连接", "正在搜索设备", MessageBoxButton.OK, false, false);
                    win.Owner = this;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.Show();
                });
                string result = DeviceDispatcher.AppendDevices();
                Dispatcher.Invoke(() =>
                {
                    win.Close();
                    MessageWindow.ShowTipWindow(result, this);
                });
            });
            t.Start();
        }

        /// <summary>
        /// 在独立窗口中显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowInWindow(object sender, RoutedEventArgs e)
        {
            if (CurrentPage != null && CurrentPage.IsDisplayedInPage == true)
            {
                CurrentPage.IsDisplayedInPage = false;
            }
        }
    }
}
