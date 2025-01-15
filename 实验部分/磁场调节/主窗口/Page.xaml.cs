using CodeHelper;
using Controls;
using DataBaseLib;
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

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : PageBase
    {
        public MagnetXYAngleWindow XWin { get; set; } = new MagnetXYAngleWindow("X扫描窗口", false) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };
        public MagnetXYAngleWindow YWin { get; set; } = new MagnetXYAngleWindow("Y扫描窗口", false) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };
        public MagnetXYAngleWindow AngleWin { get; set; } = new MagnetXYAngleWindow("角度扫描窗口", true) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };

        public MagnetZWindow ZWin { get; set; } = new MagnetZWindow("Z方向窗口") { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };

        public MagnetCheckWindow CheckWin { get; set; } = new MagnetCheckWindow("角度检查窗口") { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = MainWindow.Handle };

        public DisplayPage()
        {
            InitializeComponent();
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
            CreateThread();
        }

        /// <summary>
        /// 列出所有需要向数据库取回的数据
        /// </summary>
        public override void ListDataBaseData()
        {
        }

        public override void UpdateDataBaseToUI()
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
                OffsetWindow win = new OffsetWindow(anglestart);
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
                MagnetScanParams P = new MagnetScanParams();
                P.MRadius.Value = double.Parse(MRadius.Text);
                P.MLength.Value = double.Parse(MLength.Text);
                P.StartAngle.Value = double.Parse(AngleStart.Text);

                double the = double.Parse(ThetaPre.Text);
                double phi = double.Parse(PhiPre.Text);
                double currentzloc = double.Parse(ZHeight.Text);
                double initzdis = double.Parse(ZDistance.Text);
                double x = double.Parse(XLoc.Text);
                double y = double.Parse(YLoc.Text);
                double z = double.Parse(ZLoc.Text);

                MoverTypes xType = (MoverTypes)Enum.Parse(typeof(MoverTypes), XRelate.SelectedItem.Text);
                MoverTypes yType = (MoverTypes)Enum.Parse(typeof(MoverTypes), YRelate.SelectedItem.Text);
                MoverTypes zType = (MoverTypes)Enum.Parse(typeof(MoverTypes), ZRelate.SelectedItem.Text);

                double offsetx = double.Parse(OffsetX.Text);
                double offsety = double.Parse(OffsetY.Text);

                P.ReverseANum.Value = DeviceDispatcher.TryGetMoverDevice(MoverTypes.AngleZ, OperationMode.Read, PartTypes.Magnnet, true).IsReverse ? -1 : 1;
                P.ReverseZNum.Value = ReverseZ.IsSelected ? -1 : 1;
                P.ReverseXNum.Value = DeviceDispatcher.TryGetMoverDevice(xType, OperationMode.Read, PartTypes.Magnnet, true).IsReverse ? -1 : 1;
                P.ReverseYNum.Value = DeviceDispatcher.TryGetMoverDevice(yType, OperationMode.Read, PartTypes.Magnnet, true).IsReverse ? -1 : 1;

                double zdis = P.ReverseZNum.Value * (currentzloc - z) + initzdis;
                List<double> res = MagnetAutoScanHelper.FindDire(P.MRadius.Value, P.MLength.Value, the, phi, currentzloc);
                double ang = res[0];
                double dx = res[1];
                double dy = res[2];
                double B = res[3];
                double dz = zdis - initzdis;
                dx *= P.ReverseXNum.Value;
                dy *= P.ReverseYNum.Value;
                dz *= P.ReverseZNum.Value;
                ang *= P.ReverseANum.Value;
                double ang1 = P.StartAngle.Value + ang;

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
                    dz = zdis - initzdis;
                    dx *= P.ReverseXNum.Value;
                    dy *= P.ReverseYNum.Value;
                    dz *= P.ReverseZNum.Value;
                    ang *= P.ReverseANum.Value;
                    ang1 = P.StartAngle.Value + ang;

                    if (ang > 150)
                        ang -= 360;
                    if (ang < -150)
                        ang += 360;

                    //根据需要移动的角度进行偏心修正
                    doffs = MagnetAutoScanHelper.GetTargetOffset(FileObj.Param, ang1);
                    doffx = doffs[0];
                    doffy = doffs[1];
                }

                XPre.Content = Math.Round(x + dx + doffx, 5).ToString();
                YPre.Content = Math.Round(y + dy + doffy, 5).ToString();
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

        private bool IsThreadEnd = false;
        private bool IsThreadResume = false;

        MagnetControlFileObjecct FileObj = new MagnetControlFileObjecct();

        #region 线程参数

        #endregion

        private NanoStageInfo XStage = null;
        private NanoStageInfo YStage = null;
        private NanoStageInfo ZStage = null;
        private NanoStageInfo AStage = null;

        private double ARangeLoNum = -150;
        private double ARangeHiNum = 150;

        private event Action ThreadResumeEvent = null;

        private bool IsScamFinished = false;

        private void CreateThread()
        {
            ControlThread = new Thread(() =>
            {
                IsScamFinished = false;
                FileObj.Param = new MagnetScanParams();

                //设置初始参数
                bool result = false;
                ScanInitWindow win = null;
                Dispatcher.Invoke(() =>
                {
                    win = new ScanInitWindow();
                    result = win.ShowDialog();
                });
                if (!result) return;

                FileObj.Param.ZPlane.Value = win.ZPlane;
                FileObj.Param.XScanLo.Value = win.XLo;
                FileObj.Param.XScanHi.Value = win.XHi;
                FileObj.Param.YScanLo.Value = win.YLo;
                FileObj.Param.YScanHi.Value = win.YHi;
                FileObj.Param.D.Value = win.D;

                #region 检查数据合法性
                try
                {
                    FileObj.Param.XRelate = (MoverTypes)Enum.Parse(typeof(MoverTypes), XRelate.SelectedItem.Text);
                    FileObj.Param.YRelate = (MoverTypes)Enum.Parse(typeof(MoverTypes), YRelate.SelectedItem.Text);
                    FileObj.Param.ZRelate = (MoverTypes)Enum.Parse(typeof(MoverTypes), ZRelate.SelectedItem.Text);
                    FileObj.Param.ReverseXNum.Value = DeviceDispatcher.TryGetMoverDevice(FileObj.Param.XRelate, OperationMode.Read, PartTypes.Magnnet, true, true).IsReverse ? -1 : 1;
                    FileObj.Param.ReverseYNum.Value = DeviceDispatcher.TryGetMoverDevice(FileObj.Param.YRelate, OperationMode.Read, PartTypes.Magnnet, true, true).IsReverse ? -1 : 1;
                    FileObj.Param.ReverseZNum.Value = ReverseZ.IsSelected ? -1 : 1;
                    FileObj.Param.ReverseANum.Value = DeviceDispatcher.TryGetMoverDevice(MoverTypes.AngleZ, OperationMode.Read, PartTypes.Magnnet, true, true).IsReverse ? -1 : 1;

                    FileObj.Param.AngleY = double.Parse(AngleStart.Text);
                    FileObj.Param.AngleX = FileObj.Param.AngleY + 90;

                    FileObj.Param.ReadFromPage(new FrameworkElement[] { this });

                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageWindow.ShowTipWindow("参数设置存在错误", MainWindow.Handle);
                        return;
                    });
                }
                #endregion

                #region 设备占用
                XStage = DeviceDispatcher.TryGetMoverDevice(FileObj.Param.XRelate, OperationMode.ReadWrite, PartTypes.Magnnet, true, true);
                if (XStage == null) return;
                XStage.Use();
                YStage = DeviceDispatcher.TryGetMoverDevice(FileObj.Param.YRelate, OperationMode.ReadWrite, PartTypes.Magnnet, true, true);
                if (YStage == null) return;
                YStage.Use();
                ZStage = DeviceDispatcher.TryGetMoverDevice(FileObj.Param.ZRelate, OperationMode.ReadWrite, PartTypes.Magnnet, true, true);
                if (ZStage == null) return;
                ZStage.Use();
                AStage = DeviceDispatcher.TryGetMoverDevice(MoverTypes.AngleZ, OperationMode.ReadWrite, PartTypes.Magnnet, true, true);
                if (AStage == null) return;
                AStage.Use();
                #endregion

                try
                {
                    #region 刷新面板显示
                    SetText(XState, "正在执行流程...");
                    SetText(XLoc, "");
                    SetText(YState, "正在执行流程...");
                    SetText(YLoc, "");
                    SetText(ZState, "正在执行流程...");
                    SetText(ZDistance, "");
                    SetText(ZLoc, "");
                    SetText(AngleState, "正在执行流程...");
                    SetText(AngleTheta, "");
                    SetText(AnglePhi, "");
                    SetText(CheckState, "正在执行流程...");
                    SetProgress("X", 0);
                    SetProgress("Y", 0);
                    SetProgress("Z", 0);
                    SetProgress("A", 0);
                    SetProgress("C", 0);
                    #endregion

                    MoveZ(win.ZPlane, 10000);

                    //旋转台移动到轴向沿Y
                    AStage.Device.MoveToAndWait(FileObj.Param.AngleX, 10000);
                    //Y扫描
                    ScanX();
                    SetText(XState, "已完成，X方向磁场最大位置为：");
                    SetText(XLoc, Math.Round(FileObj.Param.XLoc.Value, 5).ToString());

                    //旋转台移动到轴向沿X
                    AStage.Device.MoveToAndWait(FileObj.Param.AngleY, 10000);
                    //移动X到最大值
                    MoveX(FileObj.Param.XLoc.Value, 10000);
                    //X扫描
                    ScanY();
                    SetText(YState, "已完成,Y方向磁场最大位置为：");
                    SetText(YLoc, Math.Round(FileObj.Param.YLoc.Value, 5).ToString());

                    //移动Y到最大值
                    MoveY(FileObj.Param.YLoc.Value, 10000);
                    //Z扫描
                    ScanZ();
                    SetText(ZState, "已完成,Z轴位置及对应的与NV距离分别为：");
                    SetText(ZLoc, Math.Round(FileObj.Param.ZLoc.Value, 5).ToString());
                    SetText(ZDistance, Math.Round(FileObj.Param.ZDistance.Value, 5).ToString());

                    //角度扫描
                    MoveZ(FileObj.Param.ZPlane.Value, 10000);
                    ScanAngle();
                    SetText(AngleState, "已完成,NV的方位角θ和φ分别为:");
                    SetText(AngleTheta, Math.Round(FileObj.Param.Theta1.Value, 4).ToString() + "或" + Math.Round(FileObj.Param.Theta2.Value, 4).ToString());
                    SetText(AnglePhi, Math.Round(FileObj.Param.Phi1.Value, 4).ToString() + "或" + Math.Round(FileObj.Param.Phi2.Value, 4).ToString());

                    //角度检查
                    //计算目标位置
                }
                catch (Exception e) { MessageWindow.ShowTipWindow("定位过程发生异常：\n" + e.Message, MainWindow.Handle); }
                finally
                {
                    #region 结束设备占用
                    XStage.UnUse();
                    YStage.UnUse();
                    ZStage.UnUse();
                    AStage.UnUse();
                    #endregion
                }

                IsScamFinished = true;
            });
        }

        private void StartThread()
        {
            ControlThread.Start();
        }

        private void ScanX()
        {
            double loc = LineScanCore("X", FileObj.Param.XScanLo.Value, FileObj.Param.XScanHi.Value, FileObj.Param.D.Value);
            // 读取磁铁角度，计算偏移量
            double angle = FileObj.Param.AngleX;
            List<double> xy = MagnetAutoScanHelper.GetTargetOffset(FileObj.Param, angle);
            FileObj.Param.XLoc.Value = loc - xy[0];
        }

        private void ScanY()
        {
            FileObj.Param.YLoc.Value = LineScanCore("Y", FileObj.Param.YScanLo.Value, FileObj.Param.YScanHi.Value, FileObj.Param.D.Value);
        }

        private double LineScanCore(string Scandir, double Lo, double Hi, double D)
        {
            SetProgress(Scandir, 0);

            //根据点数设置位移台（二分法,扫描点数始终为6)
            //遍历一遍范围，之后拟合得到最大值位置，之后范围减半
            //重复上一步骤，直到步长小于0.05mm
            //计算总点数
            double scancount = 6;
            double countApprox = 0;
            double step = (Hi - Lo) / (scancount - 1);
            while (step >= 0.1)
            {
                countApprox += scancount;
                step /= 2;
            }

            step = (Hi - Lo) / (scancount - 1);

            bool IsFirstScan = true;

            double cw1 = 0;
            double cw2 = 0;

            List<double> locdata = new List<double>();
            List<double> cw1s = new List<double>();
            List<double> cw2s = new List<double>();
            List<double> Bs = new List<double>();

            int finishedpoint = 0;
            double peak = 0;

            double scanmin = Lo;

            double scanmax = Hi;

            List<double> freqs1 = new List<double>();
            List<double> freqs2 = new List<double>();
            List<double> contracts1 = new List<double>();
            List<double> contracts2 = new List<double>();

            while (step >= 0.1)
            {
                //扫描
                for (int i = 0; i < scancount; i++)
                {
                    if (Scandir == "X")
                    {
                        MoveX(scanmin + i * step, 4000);
                    }
                    if (Scandir == "Y")
                    {
                        MoveY(scanmin + i * step, 4000);
                    }

                    if (IsFirstScan)
                    {
                        IsFirstScan = false;
                        LabviewConverter.AutoTrace(out Exception e);
                        JudgeThreadEndOrResume();
                        MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out freqs1, out contracts1, out freqs2, out contracts2);
                        JudgeThreadEndOrResume();
                        cw1 = peaks[0];
                        cw2 = peaks[1];
                    }
                    else
                    {
                        LabviewConverter.AutoTrace(out Exception exc);
                        JudgeThreadEndOrResume();
                        MagnetAutoScanHelper.ScanCW2(out cw1, out cw2, out freqs1, out contracts1, out freqs2, out contracts2, cw1, cw2);
                        JudgeThreadEndOrResume();
                    }

                    if (cw1 != 0 && cw2 != 0)
                    {
                        locdata.Add(scanmin + i * step);
                        cw1s.Add(cw1);
                        cw2s.Add(cw2);
                        CWPointObject point = new CWPointObject(Math.Round(scanmin + i * step, 5), cw1, cw2, D, freqs1, contracts1, freqs2, contracts2);
                        Dispatcher.Invoke(() =>
                        {
                            if (Scandir == "X")
                            {
                                XWin.AddData(point);
                            }
                            if (Scandir == "Y")
                            {
                                YWin.AddData(point);
                            }
                        });
                    }
                    else
                    {
                        //未扫描到谱峰，添加提示信息
                        MessageLogger.AddLogger("磁场定位", "在磁场定位中的" + Scandir + "=" + Math.Round(scanmin + i * step, 5).ToString() + "时未扫描到完整的共振峰谱", MessageTypes.Information);
                    }

                    SetProgress(Scandir, finishedpoint * 100 / countApprox);
                    finishedpoint += 1;
                }

                //拟合并绘图
                List<double> param = MagnetAutoScanHelper.FitDataWithPow2(locdata, Bs);
                //计算峰值
                peak = -param[1] / (2 * param[2]);
                //步长减半，范围减半
                step /= 2;
                double scanrange = Math.Abs(scanmax - scanmin);
                scanmin = peak - scanrange / 4;
                scanmax = peak + scanrange / 4;

                int peakind = MagnetAutoScanHelper.GetNearestDataIndex(locdata, scanmin);
                cw1 = cw1s[peakind];
                cw2 = cw2s[peakind];
            }

            return peak;
        }


        private void ScanZ()
        {
            //扫第一个点
            AStage.Device.MoveTo(FileObj.Param.AngleX);
            LabviewConverter.AutoTrace(out Exception e);
            SetProgress("Z", 12.5);
            JudgeThreadEndOrResume();
            MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks1, out List<double> freqs11, out List<double> contracts11, out List<double> freqs12, out List<double> contracts12);
            SetProgress("Z", 25);
            JudgeThreadEndOrResume();
            MoveZ(FileObj.Param.ZPlane.Value + FileObj.Param.ReverseZNum.Value, 10000);
            //扫第二个点
            LabviewConverter.AutoTrace(out e);
            SetProgress("Z", 37.5);
            JudgeThreadEndOrResume();
            MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks2, out List<double> freqs21, out List<double> contracts21, out List<double> freqs22, out List<double> contracts22);
            SetProgress("Z", 50);
            JudgeThreadEndOrResume();

            CWPointObject cp1 = new CWPointObject(FileObj.Param.ZPlane.Value, peaks1[0], peaks1[1], FileObj.Param.D.Value, freqs11, contracts11, freqs12, contracts12);
            CWPointObject cp2 = new CWPointObject(FileObj.Param.ZPlane.Value + FileObj.Param.ReverseZNum.Value, peaks2[0], peaks2[1], FileObj.Param.D.Value, freqs21, contracts21, freqs22, contracts22);

            // 如果NV磁场分量太小则转90度再测
            if (cp1.Bv != 0 && cp2.Bv != 0)
            {
                if (cp1.Bp / cp1.Bv < 0.1 || cp2.Bp / cp2.Bv < 0.1)
                {
                    AStage.Device.MoveTo(FileObj.Param.AngleY);
                    MoveZ(FileObj.Param.ZPlane.Value, 10000);
                    //扫第一个点
                    LabviewConverter.AutoTrace(out e);
                    SetProgress("Z", 62.5);
                    JudgeThreadEndOrResume();
                    MagnetAutoScanHelper.TotalCWPeaks2OrException(out peaks1, out freqs11, out contracts11, out freqs12, out contracts12);
                    SetProgress("Z", 75);
                    JudgeThreadEndOrResume();
                    MoveZ(FileObj.Param.ZPlane.Value + FileObj.Param.ReverseZNum.Value, 10000);
                    //扫第二个点
                    LabviewConverter.AutoTrace(out e);
                    SetProgress("Z", 87.5);
                    JudgeThreadEndOrResume();
                    MagnetAutoScanHelper.TotalCWPeaks2OrException(out peaks2, out freqs21, out contracts21, out freqs22, out contracts22);
                    SetProgress("Z", 100);
                    JudgeThreadEndOrResume();
                    cp1 = new CWPointObject(FileObj.Param.ZPlane.Value, peaks1[0], peaks1[1], FileObj.Param.D.Value, freqs11, contracts11, freqs12, contracts12);
                    cp2 = new CWPointObject(FileObj.Param.ZPlane.Value + FileObj.Param.ReverseZNum.Value, peaks2[0], peaks2[1], FileObj.Param.D.Value, freqs21, contracts21, freqs22, contracts22);
                }
            }

            Dispatcher.Invoke(() =>
            {
                ZWin.SetPoint1(cp1);
                ZWin.SetPoint2(cp2);
            });

            //计算位置
            double ratio = cp1.Bp / cp2.Bp;
            FileObj.Param.ZLoc.Value = cp1.MoverLoc;
            if (ratio < 1)
            {
                ratio = cp2.Bp / cp1.Bp;
                FileObj.Param.ZLoc.Value = cp2.MoverLoc;
            }

            FileObj.Param.ZDistance.Value = MagnetAutoScanHelper.FindRoot(new PillarMagnet(FileObj.Param.MRadius.Value, FileObj.Param.MLength.Value), cp1.MoverLoc, cp2.MoverLoc, ratio);
            SetProgress("Z", 100);
        }

        private void ScanAngle()
        {
            bool IsFirstScan = true;

            double peak1 = 0;
            double peak2 = 0;

            List<double> locs = new List<double>();
            List<double> bps = new List<double>();
            List<double> bs = new List<double>();

            for (int i = 0; i < 15; i++)
            {
                //设置旋转角度
                AStage.Device.MoveToAndWait(-140 + 20 * i, 10000);
                // 获取偏心修正后的x,y位置
                List<double> xy = GetRealXYLoc(-140 + 20 * i, FileObj.Param.XLoc.Value, FileObj.Param.YLoc.Value);
                // 设置XY
                MoveX(xy[0], 10000);
                MoveY(xy[1], 10000);

                List<double> freqs1 = new List<double>();
                List<double> freqs2 = new List<double>();
                List<double> contracts1 = new List<double>();
                List<double> contracts2 = new List<double>();

                if (IsFirstScan)
                {
                    IsFirstScan = false;
                    LabviewConverter.AutoTrace(out Exception e);
                    JudgeThreadEndOrResume();
                    MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out freqs1, out contracts1, out freqs2, out contracts2);
                    JudgeThreadEndOrResume();
                    peak1 = peaks[0];
                    peak2 = peaks[1];
                }
                else
                {
                    LabviewConverter.AutoTrace(out Exception e);
                    JudgeThreadEndOrResume();
                    MagnetAutoScanHelper.ScanCW2(out peak1, out peak2, out freqs1, out contracts1, out freqs2, out contracts2, peak1, peak2, scanWidth: 100);
                    JudgeThreadEndOrResume();

                    if (peak1 == 0 || peak2 == 0 || Math.Abs(peak1 - peak2) < 5)
                    {
                        LabviewConverter.AutoTrace(out e);
                        JudgeThreadEndOrResume();
                        MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out freqs1, out contracts1, out freqs2, out contracts2);
                        JudgeThreadEndOrResume();

                        peak1 = peaks[0];
                        peak2 = peaks[1];
                    }
                }

                SetProgress("A", (i + 1) * 100.0 / 15);

                CWPointObject p = new CWPointObject(-140 + 20 * i, peak1, peak2, FileObj.Param.D.Value, freqs1, contracts1, freqs2, contracts2);
                locs.Add(-140 + 20 * i);
                bps.Add(p.Bp);
                bs.Add(p.B);

                Dispatcher.Invoke(() =>
                {
                    AngleWin.AddData(p);
                });
            }

            //进行拟合得到方位角
            List<double> sindata = AngleWin.ConvertAbsDataToSin(bps);
            for (int i = 0; i < locs.Count; i++)
            {
                locs[i] = FileObj.Param.ReverseANum.Value * locs[i] - FileObj.Param.AngleY;
            }
            List<double> result = MagnetAutoScanHelper.FitSinCurve(locs, sindata);
            double amplitude = result[0];
            double phase = result[1];
            double B = bs.Average();
            //拟合参数A
            double sint = amplitude / B;
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
        }

        /// <summary>
        /// 角度检查
        /// </summary>
        private void CheckAngle()
        {
            ///刷新计算结果
            CheckWin.UpdateCalculate(FileObj.Param);
            //CheckWin.
        }

        private void SetProgress(string procedureName, double value)
        {
            Dispatcher.Invoke(() =>
            {
                if (procedureName == "X")
                {
                    if (value >= 100)
                    {
                        XProgress.Visibility = Visibility.Hidden;
                        XOk.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        XProgress.Content = Math.Round(value, 2).ToString();
                        XProgress.Visibility = Visibility.Visible;
                        XOk.Visibility = Visibility.Hidden;
                    }
                }
                if (procedureName == "Y")
                {
                    if (value >= 100)
                    {
                        YProgress.Visibility = Visibility.Hidden;
                        YOk.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        YProgress.Content = Math.Round(value, 2).ToString();
                        YProgress.Visibility = Visibility.Visible;
                        YOk.Visibility = Visibility.Hidden;
                    }
                }
                if (procedureName == "Z")
                {
                    if (value >= 100)
                    {
                        ZProgress.Visibility = Visibility.Hidden;
                        ZOk.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ZProgress.Content = Math.Round(value, 2).ToString();
                        ZProgress.Visibility = Visibility.Visible;
                        ZOk.Visibility = Visibility.Hidden;
                    }
                }
                if (procedureName == "A")
                {
                    if (value >= 100)
                    {
                        AngleProgress.Visibility = Visibility.Hidden;
                        AngleOk.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AngleProgress.Content = Math.Round(value, 2).ToString();
                        AngleProgress.Visibility = Visibility.Visible;
                        AngleOk.Visibility = Visibility.Hidden;
                    }
                }
                if (procedureName == "C")
                {
                    if (value >= 100)
                    {
                        CheckProgress.Visibility = Visibility.Hidden;
                        CheckOk.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CheckProgress.Content = Math.Round(value, 2).ToString();
                        CheckProgress.Visibility = Visibility.Visible;
                        CheckOk.Visibility = Visibility.Hidden;
                    }
                }
            });
        }

        private void SetText(TextBlock l, string value)
        {
            Dispatcher.Invoke(() =>
            {
                l.Text = value;
            });
        }

        /// <summary>
        /// 移动X轴
        /// </summary>
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        private void MoveX(double target, int timeout)
        {
            if (target > FileObj.Param.XRangeHi.Value || target < FileObj.Param.XRangeLo.Value)
            {
                MessageLogger.AddLogger("磁场定位", "位移台" + Enum.GetName(typeof(MoverTypes), FileObj.Param.XRelate) + "超量程", MessageTypes.Warning);
                return;
            }
            double target0 = XStage.Device.Target;
            double det = target - target0;
            int sgn = det > 0 ? 1 : -1;
            det = Math.Abs(det);
            while (det > 0.1)
            {
                target0 += sgn * 0.1;
                XStage.Device.MoveToAndWait(target, timeout);
                JudgeThreadEndOrResume();
                det -= 0.1;
                Thread.Sleep(50);
            }
            XStage.Device.MoveToAndWait(target0 + sgn * det, timeout);
            JudgeThreadEndOrResume();
        }

        /// <summary>
        /// 移动Y轴
        /// </summary>
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        private void MoveY(double target, int timeout)
        {
            if (target > FileObj.Param.YRangeHi.Value || target < FileObj.Param.YRangeLo.Value)
            {
                MessageLogger.AddLogger("磁场定位", "位移台" + Enum.GetName(typeof(MoverTypes), FileObj.Param.YRelate) + "超量程", MessageTypes.Warning);
                return;
            }
            double target0 = YStage.Device.Target;
            double det = target - target0;
            int sgn = det > 0 ? 1 : -1;
            det = Math.Abs(det);
            while (det > 0.1)
            {
                target0 += sgn * 0.1;
                YStage.Device.MoveToAndWait(target, timeout);
                JudgeThreadEndOrResume();
                det -= 0.1;
                Thread.Sleep(50);
            }
            YStage.Device.MoveToAndWait(target0 + sgn * det, timeout);
            JudgeThreadEndOrResume();
        }

        /// <summary>
        /// 移动Z轴
        /// </summary>
        /// <param name="target"></param>
        /// <param name="timeout"></param>
        private void MoveZ(double target, int timeout)
        {
            if (target > FileObj.Param.ZRangeHi.Value || target < FileObj.Param.ZRangeLo.Value)
            {
                MessageLogger.AddLogger("磁场定位", "位移台" + Enum.GetName(typeof(MoverTypes), FileObj.Param.ZRelate) + "超量程", MessageTypes.Warning);
                return;
            }
            double target0 = ZStage.Device.Target;
            double det = target - target0;
            int sgn = det > 0 ? 1 : -1;
            det = Math.Abs(det);
            while (det > 0.1)
            {
                target0 += sgn * 0.1;
                ZStage.Device.MoveToAndWait(target, timeout);
                JudgeThreadEndOrResume();
                det -= 0.1;
                Thread.Sleep(50);
            }
            ZStage.Device.MoveToAndWait(target0 + sgn * det, timeout);
            JudgeThreadEndOrResume();
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
                    Thread.Sleep(50);
                }
            }
        }

        private List<double> GetRealXYLoc(double angle, double centerx, double centery)
        {
            List<double> xy = MagnetAutoScanHelper.GetTargetOffset(FileObj.Param, angle);
            return new List<double>() { centerx + xy[0], centery + xy[1] };
        }
        #endregion
    }
}
