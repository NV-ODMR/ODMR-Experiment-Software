using ODMR_Lab.温度监测部分;
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
        public static 设备部分.温控.DevicePage Dev_TemPeraPage = new 设备部分.温控.DevicePage();

        public static 设备部分.位移台部分.DevicePage Dev_MoversPage = new 设备部分.位移台部分.DevicePage();

        public static 设备部分.相机_翻转镜.DevicePage Dev_CameraPage = new 设备部分.相机_翻转镜.DevicePage();

        public static 设备部分.源表.DevicePage Dev_PowerMeterPage = new 设备部分.源表.DevicePage();

        public static 设备部分.射频源_锁相放大器.DevicePage Dev_RFSource_LockInPage = new 设备部分.射频源_锁相放大器.DevicePage();

        public static 设备部分.光子探测器.DevicePage Dev_APDPage = new 设备部分.光子探测器.DevicePage();

        public static 设备部分.板卡.DevicePage Dev_PBPage = new 设备部分.板卡.DevicePage();
        #endregion

        #region 实验页面
        /// <summary>
        /// 温度曲线
        /// </summary>
        public static TemperaturePage Exp_TemPeraPage = new TemperaturePage();
        /// <summary>
        /// 磁场控制
        /// </summary>
        public static 磁场调节.DisplayPage Exp_MagnetControlPage = new 磁场调节.DisplayPage();
        /// <summary>
        /// 样品定位
        /// </summary>
        public static 样品定位.DisplayPage Exp_SamplePage = new 样品定位.DisplayPage();

        /// <summary>
        /// 场效应管测量
        /// </summary>
        public static 场效应器件测量.DisplayPage Exp_SourcePage = new 场效应器件测量.DisplayPage();

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

        #endregion

        #region 数据处理部分
        public static DataVisualPage Data_Page = new DataVisualPage();
        #endregion

        public MainWindow()
        {
            Mutex mm = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out bool isrunning);
            if (!isrunning)
            {
                Environment.Exit(0);
            }
            Handle = this;
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, MinimizeBtn, MaximizeBtn, null, 5, 30);

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
            if (MessageWindow.ShowMessageBox("提示", "确定要关闭吗?", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
            {
                bool canclose = true;
                Thread t = new Thread(() =>
                  {
                      MessageWindow win = null;
                      Dispatcher.Invoke(() =>
                      {
                          win = new MessageWindow("提示", "正在关闭设备并保存参数...", MessageBoxButton.OK, false, false);
                          win.Owner = this;
                          win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                          IsEnabled = false;
                          win.Show();
                          //保存界面参数
                          ParamManager.SaveParams();
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
                    string connectresult = DeviceDispatcher.ScanDevices();
                    Dispatcher.Invoke(() =>
                    {
                        window.Close();
                        MessageWindow.ShowMessageBox("自动连接", connectresult, MessageBoxButton.OK, true, owner: this);
                        IsEnabled = true;
                    });
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("程序环境安装出现问题,即将退出：\n" + ex.Message, Window.GetWindow(this));
                    Environment.Exit(0);
                    return;
                }
            });
            t.Start();
            #endregion
            #region 加载参数
            ParamManager.ReadAndLoadParams();
            #endregion
        }


        #region 设备列表
        #endregion

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
                GeneralGrid.ColumnDefinitions[1].Width = new GridLength(160);
            }
            if (btn.Text == "实验")
            {
                ExpList.Visibility = Visibility.Visible;
                GeneralGrid.ColumnDefinitions[1].Width = new GridLength(160);
            }
            if (btn.Text == "数据")
            {
                CurrentPage = Data_Page;
                GeneralGrid.ColumnDefinitions[1].Width = new GridLength(0);
                PageContent.Children.Clear();
                AddPageToView(Data_Page);
            }
            if (btn.Text == "扩展")
            {
                ExternalList.Visibility = Visibility.Visible;
                GeneralGrid.ColumnDefinitions[1].Width = new GridLength(160);
            }
        }

        private void ShowDeviceContent(object sender, RoutedEventArgs e)
        {
            if (CurrentPage != null)
            {
                PageContent.Children.Clear();
            }
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "温度控制器")
            {
                CurrentPage = Dev_TemPeraPage;
                AddPageToView(Dev_TemPeraPage);
            }
            if (btn.Text == "位移台")
            {
                CurrentPage = Dev_MoversPage;
                AddPageToView(Dev_MoversPage);
            }
            if (btn.Text == "相机/翻转镜")
            {
                CurrentPage = Dev_CameraPage;
                AddPageToView(Dev_CameraPage);
            }
            if (btn.Text == "源表")
            {
                CurrentPage = Dev_PowerMeterPage;
                AddPageToView(Dev_PowerMeterPage);
            }
            if (btn.Text == "射频源/Lock In")
            {
                CurrentPage = Dev_RFSource_LockInPage;
                AddPageToView(Dev_RFSource_LockInPage);
            }
            if (btn.Text == "光子计数器")
            {
                CurrentPage = Dev_APDPage;
                AddPageToView(Dev_APDPage);
            }
            if (btn.Text == "PulseBlaster")
            {
                CurrentPage = Dev_PBPage;
                AddPageToView(Dev_PBPage);
            }
        }

        private void ShowExpContent(object sender, RoutedEventArgs e)
        {
            if (CurrentPage != null)
            {
                PageContent.Children.Clear();
            }
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "温度监测")
            {
                CurrentPage = Exp_TemPeraPage;
                AddPageToView(Exp_TemPeraPage);
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
            if (btn.Text == "磁场定位")
            {
                CurrentPage = Exp_MagnetControlPage;
                AddPageToView(Exp_MagnetControlPage);
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
