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
        }

        public override void Init()
        {
        }

        public override void CloseBehaviour()
        {
            IsThreadEnd = true;
            while (ControlThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(50);
            }
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

                double the = double.Parse(ThetaPre.Text);
                double phi = double.Parse(PhiPre.Text);
                double currentzloc = double.Parse(ZHeight.Text);

                double offsetx = double.Parse(OffsetX.Text);
                double offsety = double.Parse(OffsetY.Text);

                double zdis = GetReverseNum(P.ReverseZ.Value) * (currentzloc - FileObj.Param.ZLoc.Value) + FileObj.Param.ZDistance.Value;

                List<double> res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, the, phi, currentzloc);
                double ang = res[0];
                double dx = res[1];
                double dy = res[2];
                double B = res[3];
                double dz = zdis - FileObj.Param.ZDistance.Value;
                dx *= GetReverseNum(P.ReverseX.Value);
                dy *= GetReverseNum(P.ReverseY.Value);
                dz *= GetReverseNum(P.ReverseZ.Value);
                ang *= GetReverseNum(P.ReverseA.Value);
                double ang1 = P.AngleStart.Value + ang;

                List<double> doffs = MagnetAutoScanHelper.GetTargetOffset(P, ang1);
                double doffx = doffs[0];
                double doffy = doffs[1];

                if (ang > 150)
                    ang -= 360;
                if (ang < -150)
                    ang += 360;

                //角度超量程,取等效位置
                if (Math.Abs(ang) > 150)
                {
                    res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, 180 - the, phi + 180, zdis);
                    ang = res[0];
                    dx = res[1];
                    dy = res[2];
                    B = res[3];
                    dz = zdis - FileObj.Param.ZDistance.Value;
                    dx *= GetReverseNum(P.ReverseX.Value);
                    dy *= GetReverseNum(P.ReverseY.Value);
                    dz *= GetReverseNum(P.ReverseZ.Value);
                    ang *= GetReverseNum(P.ReverseA.Value);
                    ang1 = P.AngleStart.Value + ang;

                    if (ang > 150)
                        ang -= 360;
                    if (ang < -150)
                        ang += 360;

                    //根据需要移动的角度进行偏心修正
                    doffs = MagnetAutoScanHelper.GetTargetOffset(P, ang1);
                    doffx = doffs[0];
                    doffy = doffs[1];
                }

                XPre.Content = Math.Round(FileObj.Param.XLoc.Value + dx + doffx, 5).ToString();
                YPre.Content = Math.Round(FileObj.Param.YLoc.Value + dy + doffy, 5).ToString();
                ZPre.Content = Math.Round(currentzloc, 5).ToString();
                AnglePre.Content = Math.Round(ang1, 5).ToString();

                try
                {
                    //如果可能的话计算预测的磁场强度 
                    double Intensity = double.Parse(MIntensity.Text);
                    BPre.Content = Math.Round(Intensity * B * 10000, 5).ToString();
                }
                catch (Exception) { }
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

        private Thread ControlThread = null;

        /// <summary>
        /// 线程是否停止
        /// </summary>
        private bool IsThreadEnd = false;

        /// <summary>
        /// 扫描是否暂停
        /// </summary>
        private bool IsThreadResume = false;

        public MagnetScanFileObjecct FileObj = new MagnetScanFileObjecct();

        #region 线程参数
        private NanoStageInfo XStage = new NanoStageInfo(new NanoMoverInfo(), new PIStage("", null));
        private NanoStageInfo YStage = new NanoStageInfo(new NanoMoverInfo(), new PIStage("", null));
        private NanoStageInfo ZStage = new NanoStageInfo(new NanoMoverInfo(), new PIStage("", null));
        private NanoStageInfo AStage = new NanoStageInfo(new NanoMoverInfo(), new PIStage("", null));
        #endregion

        private event Action ThreadResumeEvent = null;

        #region 开始，暂停和停止操作
        private void StartScan(object sender, RoutedEventArgs e)
        {
            if (IsThreadResume == true && IsThreadEnd == false)
            {
                IsThreadResume = false;
                SetStartState();
            }
            else
            {
                SetStartState();
                StartThread();
            }
        }

        private void ResumeScan(object sender, RoutedEventArgs e)
        {
            IsThreadResume = true;
            SetResumeState();
        }

        private void StopScan(object sender, RoutedEventArgs e)
        {
            IsThreadEnd = true;
        }

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

        private void StartThread()
        {
            ControlThread = new Thread(() =>
            {
                IsThreadEnd = false;
                IsThreadResume = false;
                MagnetScanConfigParams P = new MagnetScanConfigParams();
                #region 检查数据合法性
                try
                {
                    #region 从UI读取参数
                    Dispatcher.Invoke(() =>
                    {
                        P.ReadFromPage(new FrameworkElement[] { this });
                    });
                    #endregion

                    P.AngleY = P.AngleStart.Value;
                    P.AngleX = P.AngleY + 90;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageWindow.ShowTipWindow("参数设置存在错误", MainWindow.Handle);
                        SetStopState();
                    });
                    return;
                }
                #endregion
                #region 获取位移台,设置实验开始时间
                Dispatcher.Invoke(() =>
                {
                    FileObj.Param.SetStartTime(DateTime.Now);
                    StartTime.Content = FileObj.Param.ExpStartTime.ToString();
                    EndTime.Content = "";
                });
                #endregion
                try
                {
                    bool iscontinue = true;
                    Dispatcher.Invoke(() =>
                    {
                        if (MessageWindow.ShowMessageBox("提示", "执行扫描过程会清除当前记录的数据且不可恢复，是否继续?", MessageBoxButton.YesNo, owner: MainWindow.Handle) == MessageBoxResult.Yes)
                        {
                            #region 清除数据
                            FileObj.Param = new MagnetScanExpParams();
                            FileObj.Config = P;
                            FileObj.XPoints.Clear();
                            FileObj.YPoints.Clear();
                            FileObj.ZPoints.Clear();
                            FileObj.AnglePoints.Clear();
                            FileObj.CheckPoints.Clear();
                            //刷新窗口
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
                            #endregion
                        }
                        else
                        {
                            iscontinue = false;
                        }
                    });
                    if (!iscontinue) return;

                    #region 刷新面板显示
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
                    #endregion

                    SetText(XState, "正在执行流程...");
                    //移动Z轴
                    ScanHelper.Move(ZStage, JudgeThreadEndOrResume, FileObj.Config.ZRangeLo.Value, FileObj.Config.ZRangeHi.Value, FileObj.Config.ZPlane.Value, 10000);
                    //旋转台移动到轴向沿Y
                    ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, FileObj.Config.AngleX, 60000, 360);
                    //X方向扫描
                    ScanX();
                    SetProgress("X", 100);
                    SetText(XState, "已完成，X方向磁场最大位置为：");
                    SetText(XLoc, Math.Round(FileObj.Param.XLoc.Value, 4).ToString());

                    SetText(YState, "正在执行流程...");
                    //旋转台移动到轴向沿X
                    ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, FileObj.Config.AngleY, 10000);
                    //移动X到最大值
                    ScanHelper.Move(XStage, JudgeThreadEndOrResume, FileObj.Config.ZRangeLo.Value, FileObj.Config.ZRangeHi.Value, FileObj.Param.XLoc.Value, 10000);
                    //Y方向扫描
                    ScanY();
                    SetProgress("Y", 100);
                    SetText(YState, "已完成,Y方向磁场最大位置为：");
                    SetText(YLoc, Math.Round(FileObj.Param.YLoc.Value, 4).ToString());

                    SetText(ZState, "正在执行流程...");
                    //移动Y到最大值
                    ScanHelper.Move(YStage, JudgeThreadEndOrResume, FileObj.Config.YRangeLo.Value, FileObj.Config.YRangeHi.Value, FileObj.Param.YLoc.Value, 10000);
                    //Z扫描
                    ScanZ();
                    SetProgress("Z", 100);
                    SetText(ZState, "已完成,Z轴位置及对应的与NV距离分别为：");
                    SetText(ZDistance, Math.Round(FileObj.Param.ZDistance.Value, 4).ToString());
                    SetText(ZLoc, Math.Round(FileObj.Param.ZLoc.Value, 4).ToString());

                    SetText(AngleState, "正在执行流程...");
                    //角度扫描
                    ScanHelper.Move(ZStage, JudgeThreadEndOrResume, FileObj.Config.ZRangeLo.Value, FileObj.Config.ZRangeHi.Value, FileObj.Config.ZPlane.Value, 10000);
                    ScanAngle();
                    SetProgress("A", 100);
                    SetText(AngleState, "已完成,NV的方位角θ和φ分别为:");
                    SetText(Phi1, Math.Round(FileObj.Param.Phi1.Value, 4).ToString());
                    SetText(Phi2, Math.Round(FileObj.Param.Phi2.Value, 4).ToString());
                    SetText(Theta1, Math.Round(FileObj.Param.Theta1.Value, 4).ToString());
                    SetText(Theta2, Math.Round(FileObj.Param.Theta2.Value, 4).ToString());

                    SetText(CheckState, "正在执行流程...");
                    //角度检查
                    CheckAngle();
                    SetProgress("C", 100);
                    SetText(CheckState, "已完成,NV的方位角θ和φ分别为:");
                    SetText(CheckedTheta, Math.Round(FileObj.Param.CheckedTheta.Value, 4).ToString());
                    SetText(CheckedPhi, Math.Round(FileObj.Param.CheckedPhi.Value, 4).ToString());
                    //计算目标位置
                }
                catch (Exception e)
                {
                    MessageWindow.ShowTipWindow("定位过程发生异常,已结束定位过程：\n" + e.Message, MainWindow.Handle);
                    SetText(XState, "");
                    SetText(YState, "");
                    SetText(ZState, "");
                    SetText(AngleState, "");
                    SetText(CheckState, "");
                }
                finally
                {
                    SetStopState();
                    FileObj.Param.SetEndTime(DateTime.Now);
                    Dispatcher.Invoke(() =>
                    {
                        EndTime.Content = DateTime.Now.ToString();
                    });
                }

            });
            ControlThread.Start();
        }

        /// <summary>
        /// X方向扫描
        /// </summary>
        private void ScanX()
        {
            double loc = LineScanCore("X", FileObj);
            // 读取磁铁角度，计算偏移量
            double angle = FileObj.Config.AngleX;
            List<double> xy = MagnetAutoScanHelper.GetTargetOffset(FileObj.Config, angle);
            FileObj.Param.XLoc.Value = loc - xy[0];
        }

        /// <summary>
        /// Y方向扫描
        /// </summary>
        private void ScanY()
        {
            FileObj.Param.YLoc.Value = LineScanCore("Y", FileObj);
        }

        private double LineScanCore(string ScanDir, MagnetScanFileObjecct obj)
        {
            //根据点数设置位移台（二分法,扫描点数始终为6)
            //遍历一遍范围，之后拟合得到最大值位置，之后范围减半
            //重复上一步骤，直到步长小于0.05mm
            //计算总点数
            double scancount = 6;
            double countApprox = 0;
            double step = (obj.Config.XScanHi.Value - obj.Config.XScanLo.Value) / (scancount - 1);
            while (step >= 0.1)
            {
                countApprox += scancount;
                step /= 2;
            }

            int ind = 0;
            Scan1DSession session = new Scan1DSession();
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.ProgressBarMethod = SetProgressFromSession;
            session.StateJudgeEvent = JudgeThreadEndOrResume;

            double restrictlo = 0;
            double restricthi = 0;

            if (ScanDir == "X")
            {
                session.ScanMover = XStage;
                restrictlo = obj.Config.XScanLo.Value;
                restricthi = obj.Config.XScanHi.Value;
            }
            if (ScanDir == "Y")
            {
                session.ScanMover = YStage;
                restrictlo = obj.Config.YScanLo.Value;
                restricthi = obj.Config.YScanHi.Value;
            }

            double cw1 = 0, cw2 = 0;
            double peak = 0;
            double scanmin = obj.Config.XScanLo.Value;
            double scanmax = obj.Config.XScanHi.Value;
            double scanrange = Math.Abs(obj.Config.XScanHi.Value - obj.Config.XScanLo.Value);

            step = (scanrange) / (scancount - 1);
            while (step >= 0.1)
            {
                session.BeginScan(scanmin, scanmax, restrictlo, restricthi, 6, 0.1, ind * 100.0 / countApprox, (ind + 5) * 100.0 / countApprox, obj, cw1, cw2);
                ind += 6;

                #region 根据二次函数计算当前峰值
                List<double> xdata = new List<double>();
                List<double> ydata = new List<double>();
                List<double> cw1s = new List<double>();
                List<double> cw2s = new List<double>();

                if (ScanDir == "X")
                {
                    xdata = obj.XPoints.Select((x) => x.MoverLoc).ToList();
                    ydata = obj.XPoints.Select((x) => x.B).ToList();
                    cw1s = obj.XPoints.Select((x) => x.CW1).ToList();
                    cw2s = obj.XPoints.Select((x) => x.CW2).ToList();
                }
                if (ScanDir == "Y")
                {
                    xdata = obj.YPoints.Select((x) => x.MoverLoc).ToList();
                    ydata = obj.YPoints.Select((x) => x.B).ToList();
                    cw1s = obj.YPoints.Select((x) => x.CW1).ToList();
                    cw2s = obj.YPoints.Select((x) => x.CW2).ToList();
                }

                List<double> param = param = MagnetAutoScanHelper.FitDataWithPow2(xdata, ydata);
                //计算峰值
                peak = -param[1] / (2 * param[2]);
                //步长减半，范围减半
                step /= 2;
                scanrange = Math.Abs(scanmax - scanmin);
                scanmin = peak - scanrange / 4;
                scanmax = peak + scanrange / 4;

                int peakind = MagnetAutoScanHelper.GetNearestDataIndex(xdata, scanmin);
                cw1 = cw1s[peakind];
                cw2 = cw2s[peakind];
                #endregion
            }
            return peak;
        }

        /// <summary>
        /// Z方向扫描
        /// </summary>
        private void ScanZ()
        {
            //扫第一个点
            ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, FileObj.Config.AngleX, 60000, 360);
            Scan1DSession session = new Scan1DSession();
            session.ProgressBarMethod = SetProgressFromSession;
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.StateJudgeEvent = JudgeThreadEndOrResume;
            session.ScanMover = ZStage;

            double reslo = FileObj.Config.ZRangeLo.Value;
            double reshi = FileObj.Config.ZRangeHi.Value;

            //扫第一个点
            double height = FileObj.Config.ZPlane.Value;
            session.BeginScan(height, height, reslo, reshi, 1, 0.1, 0, 25, FileObj, 0.0, 0.0);
            //扫第二个点
            height = FileObj.Config.ZPlane.Value + GetReverseNum(FileObj.Config.ReverseZ.Value);
            session.BeginScan(height, height, reslo, reshi, 1, 0.1, 25, 50, FileObj, 0.0, 0.0);

            CWPointObject cp1 = FileObj.ZPoints[0];
            CWPointObject cp2 = FileObj.ZPoints[1];

            #region 如果NV磁场分量太小则转90度再测
            if (cp1.Bv != 0 && cp2.Bv != 0)
            {
                if (cp1.Bp / cp1.Bv < 0.1 || cp2.Bp / cp2.Bv < 0.1)
                {
                    ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, FileObj.Config.AngleY, 60000, 360);

                    FileObj.ZPoints.Clear();

                    //扫第一个点
                    height = FileObj.Config.ZPlane.Value;
                    session.BeginScan(height, height, reslo, reshi, 1, 0.1, 50, 75, FileObj, 0.0, 0.0);
                    //扫第二个点
                    height = FileObj.Config.ZPlane.Value + GetReverseNum(FileObj.Config.ReverseZ.Value);
                    session.BeginScan(height, height, reslo, reshi, 1, 0.1, 75, 100, FileObj, 0.0, 0.0);

                    cp1 = FileObj.ZPoints[0];
                    cp2 = FileObj.ZPoints[1];
                }
            }
            #endregion


            #region 计算位置
            double ratio = cp1.Bp / cp2.Bp;
            FileObj.Param.ZLoc.Value = cp1.MoverLoc;
            if (ratio < 1)
            {
                ratio = cp2.Bp / cp1.Bp;
                FileObj.Param.ZLoc.Value = cp2.MoverLoc;
            }
            if (double.IsInfinity(ratio))
            {
                ratio = cp1.B / cp2.B;
                FileObj.Param.ZLoc.Value = cp1.MoverLoc;
                if (ratio < 1)
                {
                    ratio = cp2.B / cp1.B;
                    FileObj.Param.ZLoc.Value = cp2.MoverLoc;
                }
            }
            FileObj.Param.ZDistance.Value = MagnetAutoScanHelper.FindRoot(new PillarMagnet(FileObj.Config.MRadius.Value, FileObj.Config.MLength.Value), cp1.MoverLoc, cp2.MoverLoc, ratio);
            #endregion

            #region 刷新界面
            Dispatcher.Invoke(() =>
            {
                ZWin.CWPoint1 = cp1;
                ZWin.CWPoint2 = cp2;
                ZWin.UpdateChartAndDataFlow(true);
            });
            #endregion
        }

        /// <summary>
        /// 角度扫描
        /// </summary>
        private void ScanAngle()
        {
            Scan1DSession session = new Scan1DSession();
            session.ProgressBarMethod = SetProgressFromSession;
            session.FirstScanEvent = ScanAngleEvent;
            session.ScanEvent = ScanAngleEvent;
            session.StateJudgeEvent = JudgeThreadEndOrResume;
            session.ScanMover = AStage;

            session.BeginScan(-140, 140, -150, 150, 15, 0.1, 0, 100, FileObj, 0.0, 0.0);

            #region 进行拟合得到方位角
            List<double> locs = FileObj.AnglePoints.Select((x) => x.MoverLoc).ToList();
            List<double> bs = FileObj.AnglePoints.Select((x) => x.B).ToList();
            List<double> sindata = AngleWin.ConvertAbsDataToSin(FileObj.AnglePoints.Select((x) => x.Bp).ToList());
            for (int i = 0; i < locs.Count; i++)
            {
                locs[i] = GetReverseNum(FileObj.Config.ReverseA.Value) * locs[i] - FileObj.Config.AngleY;
            }
            List<double> result = MagnetAutoScanHelper.FitSinCurve(locs, sindata);
            double amplitude = result[0];
            double phase = result[1];
            double B = bs.Average();
            //拟合参数A
            double sint = amplitude / B;
            if (sint > 1) sint = 1;
            if (sint < -1) sint = -1;
            FileObj.Param.Theta1.Value = Math.Abs(Math.Asin(sint) * 180 / Math.PI);
            FileObj.Param.Theta2.Value = 180 - FileObj.Param.Theta1.Value;
            FileObj.Param.Phi1.Value = phase;
            FileObj.Param.Phi2.Value = FileObj.Param.Phi1.Value + 180;
            while (FileObj.Param.Phi2.Value > 360)
            {
                FileObj.Param.Phi2.Value -= 360;
            }
            while (FileObj.Param.Phi2.Value < 360)
            {
                FileObj.Param.Phi2.Value += 360;
            }
            #endregion

            #region 在界面上更新
            Dispatcher.Invoke(() =>
            {
                AngleWin.CWPoints.Clear(false);
                AngleWin.CWPoints.AddRange(FileObj.AnglePoints);
                AngleWin.UpdateChartAndDataFlow(true);
            });
            #endregion
        }

        /// <summary>
        /// 角度检查
        /// </summary>
        private void CheckAngle()
        {
            Dispatcher.Invoke(() =>
            {
                CheckWin.UpdateCalculate();
                CheckWin.UpdateChartAndDataFlow(true);
            });
            ///刷新计算结果
            MagnetAutoScanHelper.CalculatePossibleLocs(FileObj.Config, FileObj.Param, out double x1, out double y1, out double z1, out double a1, out double x2, out double y2, out double z2, out double a2);

            #region 移动并测量第一个点
            ScanHelper.Move(XStage, JudgeThreadEndOrResume, FileObj.Config.XRangeLo.Value, FileObj.Config.XRangeHi.Value, x1, 10000);
            ScanHelper.Move(YStage, JudgeThreadEndOrResume, FileObj.Config.YRangeLo.Value, FileObj.Config.YRangeHi.Value, y1, 10000);
            ScanHelper.Move(ZStage, JudgeThreadEndOrResume, FileObj.Config.ZRangeLo.Value, FileObj.Config.ZRangeHi.Value, z1, 10000);
            ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, a1, 10000);
            //测量
            LabviewConverter.AutoTrace(out Exception e);
            MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out List<double> freqs1, out List<double> contracts1, out List<double> freqs2, out List<double> contracts2);

            CWPointObject p1 = new CWPointObject(0, Math.Min(peaks[0], peaks[1]), Math.Max(peaks[0], peaks[1]), FileObj.Config.D.Value, freqs1, contracts1, freqs2, contracts2);
            FileObj.CheckPoints.Add(p1);
            Dispatcher.Invoke(() =>
            {
                CheckWin.CWPoint1 = p1;
                CheckWin.UpdateChartAndDataFlow(true);
            });
            #endregion

            #region 移动并测量第二个点
            ScanHelper.Move(XStage, JudgeThreadEndOrResume, FileObj.Config.XRangeLo.Value, FileObj.Config.XRangeHi.Value, x2, 10000);
            ScanHelper.Move(YStage, JudgeThreadEndOrResume, FileObj.Config.YRangeLo.Value, FileObj.Config.YRangeHi.Value, y2, 10000);
            ScanHelper.Move(ZStage, JudgeThreadEndOrResume, FileObj.Config.ZRangeLo.Value, FileObj.Config.ZRangeHi.Value, z2, 10000);
            ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, a2, 10000);
            //测量
            LabviewConverter.AutoTrace(out e);
            MagnetAutoScanHelper.TotalCWPeaks2OrException(out peaks, out freqs1, out contracts1, out freqs2, out contracts2);

            CWPointObject p2 = new CWPointObject(0, Math.Min(peaks[0], peaks[1]), Math.Max(peaks[0], peaks[1]), FileObj.Config.D.Value, freqs1, contracts1, freqs2, contracts2);
            FileObj.CheckPoints.Add(p1);
            Dispatcher.Invoke(() =>
            {
                CheckWin.CWPoint2 = p2;
                CheckWin.UpdateChartAndDataFlow(true);
            });
            #endregion

            #region 计算结果
            if (p1 == null || p2 == null)
            {
                throw new Exception("数据不全，无法筛选出正确的NV朝向");
            }
            double v1 = p1.Bp / p1.B;
            double v2 = p2.Bp / p2.B;
            if (v1 < v2)
            {
                FileObj.Param.CheckedPhi = FileObj.Param.Phi1;
                FileObj.Param.CheckedTheta = FileObj.Param.Theta1;
            }
            else
            {
                FileObj.Param.CheckedPhi = FileObj.Param.Phi2;
                FileObj.Param.CheckedTheta = FileObj.Param.Theta2;
            }
            #endregion 

            #region 刷新界面
            Dispatcher.Invoke(() =>
            {
                CheckWin.CWPoint1 = p1;
                CheckWin.CWPoint2 = p2;
            });
            #endregion
        }

        /// <summary>
        /// 获取反转系数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetReverseNum(bool value)
        {
            return value ? 1 : -1;
        }

        #region 扫描实验步骤(扫谱后保存数据点)
        private List<object> ScanEvent(NanoStageInfo stage, double loc, List<object> originOutput)
        {
            return Experiment(stage, loc, 10, originOutput);
        }

        private List<object> ScanAngleEvent(NanoStageInfo stage, double loc, List<object> originOutput)
        {
            // 获取偏心修正后的x,y位置
            List<double> xy = GetRealXYLoc(loc, FileObj.Param.XLoc.Value, FileObj.Param.YLoc.Value);
            // 设置XY
            ScanHelper.Move(XStage, JudgeThreadEndOrResume, FileObj.Config.XRangeLo.Value, FileObj.Config.XRangeHi.Value, xy[0], 10000);
            ScanHelper.Move(YStage, JudgeThreadEndOrResume, FileObj.Config.YRangeLo.Value, FileObj.Config.YRangeHi.Value, xy[0], 10000);

            return Experiment(stage, loc, 30, originOutput);
        }

        private List<object> Experiment(NanoStageInfo stage, double loc, double scanWidth, List<object> originOutput)
        {
            List<double> freqs1 = new List<double>();
            List<double> contracts1 = new List<double>();
            List<double> freqs2 = new List<double>();
            List<double> contracts2 = new List<double>();
            //频率未确定
            if ((double)originOutput[1] == 0 && (double)originOutput[2] == 0)
            {
                LabviewConverter.AutoTrace(out Exception e);
                JudgeThreadEndOrResume();
                MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out freqs1, out contracts1, out freqs2, out contracts2);
                JudgeThreadEndOrResume();

                originOutput[1] = Math.Min(peaks[0], peaks[1]);
                originOutput[2] = Math.Max(peaks[0], peaks[1]);
            }
            else
            {
                LabviewConverter.AutoTrace(out Exception exc);
                JudgeThreadEndOrResume();
                MagnetAutoScanHelper.ScanCW2(out double cw1, out double cw2, out freqs1, out contracts1, out freqs2, out contracts2, (double)originOutput[1], (double)originOutput[2], scanWidth);
                JudgeThreadEndOrResume();
                if (cw1 == 0 || cw2 == 0)
                {
                    //未扫描到谱峰，添加提示信息
                    MessageLogger.AddLogger("磁场定位", "位移台" + stage.MoverType.ToString() + "=" + Math.Round(loc, 5).ToString() + "时未扫描到完整的共振峰谱", MessageTypes.Information);
                    originOutput[1] = cw1;
                    originOutput[2] = cw2;
                    return originOutput;
                }
                originOutput[1] = cw1;
                originOutput[2] = cw2;
            }

            MagnetScanFileObjecct obj = originOutput[0] as MagnetScanFileObjecct;
            CWPointObject point = new CWPointObject(loc, (double)originOutput[1], (double)originOutput[2], obj.Config.D.Value, freqs1, contracts1, freqs2, contracts2);

            if (stage == XStage)
            {
                obj.XPoints.Add(point);
                Dispatcher.Invoke(() =>
                {
                    XWin.CWPoints.Add(point);
                    XWin.UpdateChartAndDataFlow(true);
                });
            }
            if (stage == YStage)
            {
                obj.YPoints.Add(point);
                Dispatcher.Invoke(() =>
                {
                    YWin.CWPoints.Add(point);
                    YWin.UpdateChartAndDataFlow(true);
                });
            }
            if (stage == ZStage)
            {
                obj.ZPoints.Add(point);
                Dispatcher.Invoke(() =>
                {
                    if (obj.ZPoints.Count == 1)
                    {
                        ZWin.CWPoint1 = point;
                    }
                    else
                    {
                        ZWin.CWPoint2 = point;
                    }
                    ZWin.UpdateChartAndDataFlow(true);
                });
            }
            if (stage == AStage)
            {
                obj.AnglePoints.Add(point);
                Dispatcher.Invoke(() =>
                {
                    AngleWin.CWPoints.Add(point);
                    AngleWin.UpdateChartAndDataFlow(true);
                });
            }

            return originOutput;
        }
        #endregion

        private void SetProgress(string procedureName, double value)
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

        private void SetProgressFromSession(NanoStageInfo info, double arg2)
        {
            if (info == XStage)
            {
                SetProgress("X", arg2);
                return;
            }
            if (info == YStage)
            {
                SetProgress("Y", arg2);
                return;
            }
            if (info == ZStage)
            {
                SetProgress("Z", arg2);
                return;
            }
            if (info == AStage)
            {
                SetProgress("A", arg2);
                return;
            }
        }

        private void SetText(TextBlock l, string value)
        {
            Dispatcher.Invoke(() =>
            {
                l.Text = value;
            });
        }

        /// <summary>
        /// 如果状态为等待则挂起，如果状态为结束则抛出异常
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void JudgeThreadEndOrResume()
        {
            if (IsThreadEnd)
            {
                throw new Exception("定位进程已被终止");
            }
            if (IsThreadResume)
            {
                ThreadResumeEvent?.Invoke();
                while (IsThreadResume)
                {
                    if (IsThreadEnd)
                    {
                        throw new Exception("定位进程已被终止");
                    }
                    Thread.Sleep(50);
                }
            }
        }

        private List<double> GetRealXYLoc(double angle, double centerx, double centery)
        {
            List<double> xy = MagnetAutoScanHelper.GetTargetOffset(FileObj.Config, angle);
            return new List<double>() { centerx + xy[0], centery + xy[1] };
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
