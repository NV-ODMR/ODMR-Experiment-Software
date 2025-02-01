using CodeHelper;
using Controls;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.位移台部分;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Xml.Linq;
using Path = System.IO.Path;
using MathNet.Numerics.Distributions;
using System.Diagnostics.Contracts;
using MathNet.Numerics;
using ODMR_Lab.Python.LbviewHandler;
using ODMR_Lab.实验部分.扫描基方法;
using OpenCvSharp;
using static System.Collections.Specialized.BitVector32;
using ODMR_Lab.IO操作;
using System.Net.Http;
using ODMR_Lab.实验部分.磁场调节.主窗口;
using ODMR_Lab.实验类;
using ODMR_Lab.Python管理器;
using Controls.Windows;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : PageBase
    {
        public MagnetXYAngleWindow XWin { get; set; } = null;
        public MagnetXYAngleWindow YWin { get; set; } = null;
        public MagnetZWindow ZWin { get; set; } = null;

        public MagnetXYAngleWindow AngleWin { get; set; } = null;
        public MagnetCheckWindow CheckWin { get; set; } = null;

        public DisplayPage()
        {
            InitializeComponent();

            #region 设置子窗口
            XWin = new MagnetXYAngleWindow("X扫描窗口", false, this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };
            YWin = new MagnetXYAngleWindow("Y扫描窗口", false, this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };
            ZWin = new MagnetZWindow("Z方向窗口", this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };

            AngleWin = new MagnetXYAngleWindow("角度扫描窗口", true, this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };
            CheckWin = new MagnetCheckWindow("角度检查窗口", this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };
            #endregion

            XRelate.TemplateButton = TemplateBtn;
            YRelate.TemplateButton = TemplateBtn;
            ZRelate.TemplateButton = TemplateBtn;
            XRelate.Items.Add(new DecoratedButton() { Text = "X" });
            XRelate.Items.Add(new DecoratedButton() { Text = "Y" });
            XRelate.Items.Add(new DecoratedButton() { Text = "Z" });
            YRelate.Items.Add(new DecoratedButton() { Text = "X" });
            YRelate.Items.Add(new DecoratedButton() { Text = "Y" });
            YRelate.Items.Add(new DecoratedButton() { Text = "Z" });
            ZRelate.Items.Add(new DecoratedButton() { Text = "X" });
            ZRelate.Items.Add(new DecoratedButton() { Text = "Y" });
            ZRelate.Items.Add(new DecoratedButton() { Text = "Z" });
            ARelate.Items.Add(new DecoratedButton() { Text = "AngleX" });
            ARelate.Items.Add(new DecoratedButton() { Text = "AngleY" });
            ARelate.Items.Add(new DecoratedButton() { Text = "AngleZ" });

            CodeHelper.MouseColorHelper h = new MouseColorHelper(BorderX.Background, TemplateBtn.MoveInColor, TemplateBtn.PressedColor);
            h.RegistateTarget(BorderX);
            h = new MouseColorHelper(BorderX.Background, TemplateBtn.MoveInColor, TemplateBtn.PressedColor);
            h.RegistateTarget(BorderY);
            h = new MouseColorHelper(BorderX.Background, TemplateBtn.MoveInColor, TemplateBtn.PressedColor);
            h.RegistateTarget(BorderZ);
            h = new MouseColorHelper(BorderX.Background, TemplateBtn.MoveInColor, TemplateBtn.PressedColor);
            h.RegistateTarget(BorderAngle);
            h = new MouseColorHelper(BorderX.Background, TemplateBtn.MoveInColor, TemplateBtn.PressedColor);
            h.RegistateTarget(BorderCheck);

            InitExperimentConfigs();
        }

        public override void Init()
        {
        }

        public override void CloseBehaviour()
        {
            FileObj?.Dispose();
        }

        /// <summary>
        /// 计算偏心参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OffsetCalculate(object sender, RoutedEventArgs e)
        {
            try
            {
                double anglestart = double.Parse(AngleStart.Text);
                OffsetWindow win = new OffsetWindow(this);
                win.Owner = MainWindow.Handle;
                win.ShowDialog();
                if (!double.IsNaN(win.OffsetX))
                {
                    OffsetX.Text = Math.Round(win.OffsetX, 4).ToString();
                }
                if (!double.IsNaN(win.OffsetY))
                {
                    OffsetY.Text = Math.Round(win.OffsetY, 4).ToString();
                }
            }
            catch (Exception ex) { return; }
        }

        /// <summary>
        /// 计算预测磁场
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalculatePredictField(object sender, RoutedEventArgs e)
        {
            try
            {
                MagnetScanConfigParams P = new MagnetScanConfigParams();
                P.ReadFromPage(new FrameworkElement[] { this });
                PredictData d = new PredictData(FileObj.Param, P, double.Parse(ThetaPre.Text), double.Parse(PhiPre.Text), double.Parse(ZHeight.Text));
                MagnetAutoScanHelper.CalculatePredictField(d);
                XPre.Content = Math.Round(d.XLocPredictOutPut, 5).ToString();
                YPre.Content = Math.Round(d.YLocPredictOutPut, 5).ToString();
                ZPre.Content = Math.Round(d.ZLocPredictOutPut, 5).ToString();
                AnglePre.Content = Math.Round(d.ALocPredictOutPut, 5).ToString();
            }
            catch (Exception ex) { return; }
        }

        /// <summary>
        /// 显示轴关联提示框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAxisRelateInform(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowMessageBox("提示", "磁场定位中的坐标轴名称及方向规定", MessageBoxButton.OK, source: new BitmapImage(new Uri(Path.Combine(Environment.CurrentDirectory, "图片资源", "Setup.png"))), owner: MainWindow.Handle);
        }

        private void OpenWindow(object sender, MouseButtonEventArgs e)
        {
            Border b = sender as Border;
            if (b.Name == "BorderX")
            {
                XWin.Show();
                XWin.Topmost = true;
                XWin.Topmost = false;
            }
            if (b.Name == "BorderY")
            {
                YWin.Show();
                YWin.Topmost = true;
                YWin.Topmost = false;
            }
            if (b.Name == "BorderZ")
            {
                ZWin.Show();
                ZWin.Topmost = true;
                ZWin.Topmost = false;
            }
            if (b.Name == "BorderAngle")
            {
                AngleWin.Show();
                AngleWin.Topmost = true;
                AngleWin.Topmost = false;
            }
            if (b.Name == "BorderCheck")
            {
                CheckWin.Show();
                CheckWin.Topmost = true;
                CheckWin.Topmost = false;
            }
        }

        #region 定位线程

        private void InitExperimentConfigs()
        {
            FileObj.ControlStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(InitParamPanel, RunningBehaviours.DisableWhenRunning));
            FileObj.ControlStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(ParamPage, RunningBehaviours.DisableWhenRunning));
            FileObj.ControlStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(PredictPanel, RunningBehaviours.DisableWhenRunning));
            FileObj.ExpPage = this;
            FileObj.StartButton = StartBtn;
            FileObj.ExpStartTimeLabel = StartTime;
            FileObj.ExpEndTimeLabel = EndTime;
            FileObj.ResumeButton = ResumeBtn;
            FileObj.StopButton = StopBtn;

            FileObj.InitEvent += InitWindows;
            FileObj.ErrorStateEvent += InitWindows;
        }

        /// <summary>
        /// 清除窗口数据
        /// </summary>
        public void ClearWindows()
        {
            Dispatcher.Invoke(() =>
            {
                XWin.CWPoints.Clear();
                XWin.UpdateChartAndDataFlow(true);
                YWin.CWPoints.Clear();
                YWin.UpdateChartAndDataFlow(true);
                AngleWin.CWPoints.Clear();
                AngleWin.UpdateChartAndDataFlow(true);
                ZWin.CWPoint1 = null;
                ZWin.CWPoint2 = null;
                ZWin.UpdateChartAndDataFlow(true);
                CheckWin.CWPoint1 = null;
                CheckWin.CWPoint2 = null;
                CheckWin.UpdateChartAndDataFlow(true);
            });
        }

        /// <summary>
        /// 初始化扫描状态
        /// </summary>
        public void InitWindows()
        {
            Dispatcher.Invoke(() =>
            {
                SetText(XLoc, "");
                SetText(YLoc, "");
                SetText(ZDistance, "");
                SetText(ZLoc, "");
                SetText(Theta1, "");
                SetText(Theta2, "");
                SetText(Phi1, "");
                SetText(Phi2, "");
                SetText(CheckedTheta, "");
                SetText(CheckedPhi, "");
                SetProgress("X", 0);
                SetProgress("Y", 0);
                SetProgress("Z", 0);
                SetProgress("A", 0);
                SetProgress("C", 0);
            });
        }

        public MagnetScanExpObject FileObj = new MagnetScanExpObject();

        #region 开始，暂停和停止操作

        private void SetStartState()
        {
            Dispatcher.Invoke(() =>
            {
                StartBtn.IsEnabled = false;
                StopBtn.IsEnabled = true;
                ResumeBtn.IsEnabled = true;
                InitParamPanel.IsEnabled = false;
                ParamPage.IsEnabled = false;
                PredictPanel.IsEnabled = false;
            });
        }

        private void SetResumeState()
        {
            Dispatcher.Invoke(() =>
            {
                StartBtn.IsEnabled = true;
                StopBtn.IsEnabled = true;
                ResumeBtn.IsEnabled = false;
                InitParamPanel.IsEnabled = false;
                ParamPage.IsEnabled = false;
                PredictPanel.IsEnabled = false;
            });
        }

        private void SetStopState()
        {
            Dispatcher.Invoke(() =>
            {
                StartBtn.IsEnabled = true;
                StopBtn.IsEnabled = false;
                ResumeBtn.IsEnabled = false;
                InitParamPanel.IsEnabled = true;
                ParamPage.IsEnabled = true;
                PredictPanel.IsEnabled = true;
            });
        }
        #endregion

        public void SetProgress(string procedureName, double value)
        {
            Dispatcher.Invoke(() =>
            {
                if (procedureName == "X")
                {
                    SetSingleProcess(XProgress, XOk, value);
                }
                if (procedureName == "Y")
                {
                    SetSingleProcess(YProgress, YOk, value);
                }
                if (procedureName == "Z")
                {
                    SetSingleProcess(ZProgress, ZOk, value);
                }
                if (procedureName == "A")
                {
                    SetSingleProcess(AngleProgress, AngleOk, value);
                }
                if (procedureName == "C")
                {
                    SetSingleProcess(CheckProgress, CheckOk, value);
                }
            });
        }

        private void SetSingleProcess(Label Progress, Image ProgressImage, double value)
        {
            if (value >= 100)
            {
                Progress.Visibility = Visibility.Hidden;
                ProgressImage.Visibility = Visibility.Visible;
            }
            else
            {
                Progress.Content = Math.Round(value, 2).ToString() + "%";
                Progress.Visibility = Visibility.Visible;
                ProgressImage.Visibility = Visibility.Hidden;
            }
        }

        private void SetText(TextBlock l, string value)
        {
            Dispatcher.Invoke(() =>
            {
                l.Text = value;
            });
        }
        #endregion

        /// <summary>
        /// 保存实验数据为文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFile(object sender, RoutedEventArgs e)
        {
            bool result = FileObj.WriteFromExplorer();
            if (result)
            {
                TimeWindow w = new TimeWindow();
                w.Owner = MainWindow.Handle;
                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                w.ShowWindow("文件已保存");
            }
        }
    }
}
