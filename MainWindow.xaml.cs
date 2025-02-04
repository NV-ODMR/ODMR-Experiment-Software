using ODMR_Lab.温度监测部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CodeHelper;
using Controls;
using ODMR_Lab.Windows;
using System.IO;
using System.Windows.Forms;
using ODMR_Lab.相机;
using HardWares.相机_CCD_;
using Path = System.IO.Path;
using System.Threading;
using ODMR_Lab.Python.LbviewHandler;
using ODMR_Lab.实验部分.磁场调节;
using ODMR_Lab.基本窗口;
using ODMR_Lab.基本控件;
using ODMR_Lab.数据处理;
using ODMR_Lab.实验部分.场效应器件测量;
using Label = System.Windows.Controls.Label;
using ODMR_Lab.Python管理器;
using ODMR_Lab.IO操作;
using Controls.Windows;

namespace ODMR_Lab
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public static MainWindow Handle { get; set; } = null;

        public static PageBase CurrentPage = null;

        #region 设备页面
        public static 温度监测部分.DevicePage Dev_TemPeraPage = new 温度监测部分.DevicePage();

        public static 位移台部分.DevicePage Dev_MoversPage = new 位移台部分.DevicePage();

        public static 相机.DevicePage Dev_CameraPage = new 相机.DevicePage();

        public static 源表部分.DevicePage Dev_PowerMeterPage = new 源表部分.DevicePage();
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

        #endregion

        #region 自定义实验页面
        List<>
        #endregion

        #region 扩展页面
        /// <summary>
        /// Python管理器
        /// </summary>
        public static Python管理器.ExtPage Ext_PythonPage = new Python管理器.ExtPage();

        #endregion


        #region 数据处理部分
        public static bool IsInWindow { get; set; } = false;

        public static DataVisualPage Data_Page { get; set; } = new DataVisualPage();

        public static DataVisualWindow DataWindow { get; set; } = new DataVisualWindow();

        /// <summary>
        /// 设置数据页面显示在独立窗口中
        /// </summary>
        public void SetShowInWindow()
        {
            Data_Page.ShowInPage.Visibility = Visibility.Collapsed;
            if (CurrentPage == Data_Page)
            {
                PageContent.Children.Remove(Data_Page);
                PageContent.Children.Add(
                    new Label()
                    {
                        FontSize = 40,
                        FontWeight = FontWeights.Bold,
                        Content = "正在独立窗口中显示",
                        Foreground = Brushes.White,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
            }
            Data_Page.ParentWindow = DataWindow;
            if (DataWindow.VisualPage.Children.Contains(Data_Page) == false)
                DataWindow.VisualPage.Children.Add(Data_Page);
            DataWindow.Show();
        }

        /// <summary>
        /// 设置数据页面显示在主窗口页面中
        /// </summary>
        public void SetShowInPage()
        {
            if (DataWindow.VisualPage.Children.Count != 0)
            {
                (DataWindow.VisualPage.Children[0] as DataVisualPage).ShowInPage.Visibility = Visibility.Visible;
                (DataWindow.VisualPage.Children[0] as DataVisualPage).ParentWindow = Handle;
            }
            DataWindow.VisualPage.Children.Clear();
            if (CurrentPage == Data_Page)
            {
                PageContent.Children.Clear();
                PageContent.Children.Add(Data_Page);
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Handle = this;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, 5, 30);

            Dev_TemPeraPage.Init();
            Dev_CameraPage.Init();
            Dev_MoversPage.Init();
            Dev_PowerMeterPage.Init();

            Exp_TemPeraPage.Init();
            Exp_MagnetControlPage.Init();
            Exp_SamplePage.Init();
            Exp_SourcePage.Init();

            #region 自定义实验
            //获取所有的自定义实验

            #endregion

            Ext_PythonPage.Init();

            AutoScrollViewer a = new AutoScrollViewer();

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
                Hide();
                bool canclose = DeviceDispatcher.CloseDevicesAndSave();
                if (!canclose) return;
                //保存界面参数
                ParamManager.SaveParams();
                Close();
                Environment.Exit(0);
            }
        }

        private void UnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs e)
        {
            MessageWindow.ShowTipWindow("程序运行出现异常,即将退出,异常原因：\n" + ((Exception)e.ExceptionObject).Message, this);
            PrintStacktrace((Exception)e.ExceptionObject);
            DeviceDispatcher.CloseDevicesAndSave();
            //保存界面参数
            ParamManager.SaveParams();
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

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Maximize(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                return;
            }
            if (WindowState == WindowState.Normal)
            {
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                WindowState = WindowState.Maximized;
                return;
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            #region 加载参数
            ParamManager.ReadAndLoadParams();
            #endregion
            #region 自动连接设备
            MessageBoxResult res = MessageWindow.ShowMessageBox("自动连接", "是否自动尝试连接上次关闭时保存的所有设备?", MessageBoxButton.YesNo, owner: this);
            {
                UpdateLayout();
                Thread t = new Thread(() =>
                {
                    try
                    {
                        if (res == MessageBoxResult.Yes)
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
                    }
                    catch (Exception ex)
                    {
                        MessageWindow.ShowTipWindow("程序环境安装出现问题,即将退出：\n" + ex.Message, MainWindow.Handle);
                        Environment.Exit(0);
                        return;
                    }
                });
                t.Start();
            }
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

                if (Data_Page.ShowInPage.Visibility == Visibility.Visible)
                {
                    SetShowInPage();
                }
                else
                {
                    SetShowInWindow();
                }
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
                PageContent.Children.Add(Dev_TemPeraPage);
            }
            if (btn.Text == "位移台")
            {
                CurrentPage = Dev_MoversPage;
                PageContent.Children.Add(Dev_MoversPage);
            }
            if (btn.Text == "相机/翻转镜")
            {
                CurrentPage = Dev_CameraPage;
                PageContent.Children.Add(Dev_CameraPage);
            }
            if (btn.Text == "源表")
            {
                CurrentPage = Dev_PowerMeterPage;
                PageContent.Children.Add(Dev_PowerMeterPage);
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
                PageContent.Children.Add(Exp_TemPeraPage);
            }
            if (btn.Text == "磁场定位")
            {
                CurrentPage = Exp_MagnetControlPage;
                PageContent.Children.Add(Exp_MagnetControlPage);
            }

            if (btn.Text == "样品定位")
            {
                CurrentPage = Exp_SamplePage;
                PageContent.Children.Add(Exp_SamplePage);
            }

            if (btn.Text == "场效应器件测量")
            {
                CurrentPage = Exp_SourcePage;
                PageContent.Children.Add(Exp_SourcePage);
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
                PageContent.Children.Add(Ext_PythonPage);
            }
        }
    }
}
