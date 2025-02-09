using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.Python.LbviewHandler;
using ODMR_Lab.Windows;
using ODMR_Lab.位移台部分;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// MagnetZWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MagnetCheckWindow : Window
    {
        private CWPointObject cwwPoint1 = null;
        public CWPointObject CWPoint1
        {
            get { return cwwPoint1; }
            set
            {
                cwwPoint1 = value;
                UpdatePointData();
            }
        }

        private CWPointObject cwwPoint2 = null;
        public CWPointObject CWPoint2
        {
            get { return cwwPoint2; }
            set
            {
                cwwPoint2 = value;
                UpdatePointData();
            }
        }

        private DisplayPage ParentPage { get; set; } = null;

        private NumricChartData1D LineCW1;
        private NumricChartData1D LineCW2;
        private NumricChartData1D Freq1;
        private NumricChartData1D Freq2;

        public MagnetCheckWindow(string windowtitle, DisplayPage parent)
        {
            InitializeComponent();

            LineCW1 = new NumricChartData1D("CW1", "角度校验点1", ChartDataType.Y);
            LineCW2 = new NumricChartData1D("CW2", "角度校验点2", ChartDataType.Y);
            Freq1 = new NumricChartData1D("Freq1", "角度校验点1", ChartDataType.X);
            Freq2 = new NumricChartData1D("Freq2", "角度校验点2", ChartDataType.X);

            CWChart.DataSource.AddRange(new List<ChartData1D>() { LineCW1, LineCW2, Freq1, Freq2 });
            CWChart.UpdateChartAndDataFlow(true);

            ParentPage = parent;
            Title = windowtitle;
            WindowTitle.Content = windowtitle;
            CodeHelper.WindowResizeHelper helper = new CodeHelper.WindowResizeHelper();
            helper.RegisterWindow(this, dragHeight: 30);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #region 刷新显示部分
        /// <summary>
        /// 刷新CW点列表
        /// </summary>
        private void UpdatePointData()
        {
            MeasuredPoints.ClearItems();
            if (CWPoint1 == null)
            {
                MeasuredPoints.AddItem(null, "", "", "", "", "");
            }
            else
            {
                MeasuredPoints.AddItem(CWPoint1, Math.Round(CWPoint1.CW1, 4).ToString(), Math.Round(CWPoint1.CW2, 4).ToString(), Math.Round(CWPoint1.Bp, 4).ToString(), Math.Round(CWPoint1.Bv, 4).ToString(), Math.Round(CWPoint1.B, 4).ToString());
            }

            if (CWPoint2 == null)
            {
                MeasuredPoints.AddItem(null, "", "", "", "", "");
            }
            else
            {
                MeasuredPoints.AddItem(CWPoint2, Math.Round(CWPoint2.CW1, 4).ToString(), Math.Round(CWPoint2.CW2, 4).ToString(), Math.Round(CWPoint2.Bp, 4).ToString(), Math.Round(CWPoint2.Bv, 4).ToString(), Math.Round(CWPoint2.B, 4).ToString());
            }
        }

        /// <summary>
        /// 刷新计算
        /// </summary>
        public void UpdateCalculate()
        {
            try
            {
                MagnetScanConfigParams Cp = new MagnetScanConfigParams();
                Cp.ReadFromPage(new FrameworkElement[] { ParentPage }, false);
                MagnetScanExpParams Pp = new MagnetScanExpParams();
                Pp.ReadFromPage(new FrameworkElement[] { ParentPage }, false);
                MagnetAutoScanHelper.CalculatePossibleLocs(Cp, Pp, out double x1, out double y1, out double z1, out double angle1, out double x2, out double y2, out double z2, out double angle2);
                UpdateCalculateView(x1, y1, z1, angle1, x2, y2, z2, angle2);
            }
            catch (Exception ex)
            {
                if (IsVisible == true)
                {
                    MessageWindow.ShowTipWindow("参数格式错误，磁场预测计算未完成", this);
                }
            }
        }

        public void UpdateCalculateView(double x1, double y1, double z1, double angle1, double x2, double y2, double z2, double angle2)
        {
            Dispatcher.Invoke(() =>
            {
                PredictParams.ClearItems();
                PredictParams.AddItem(null, x1, y1, z1, angle1);
                PredictParams.AddItem(null, x2, y2, z2, angle2);
            });
        }

        private void SelectCW(int arg1, object arg2)
        {
            CWPointObject obj = (arg2 as CWPointObject);
            Freq1.Data = obj.CW1Freqs;
            Freq2.Data = obj.CW2Freqs;
            LineCW1.Data = obj.CW1Values;
            LineCW2.Data = obj.CW2Values;
            UpdateChartAndDataFlow(true);
        }

        /// <summary>
        /// 刷新图表显示
        /// </summary>
        public void UpdateChartAndDataFlow(bool isAutoScale)
        {
            if (MeasuredPoints.GetSelectedTag() == null) return;
            CWPointObject point = MeasuredPoints.GetSelectedTag() as CWPointObject;

            LineCW1.Data = point.CW1Values;
            LineCW2.Data = point.CW2Values;
            Freq1.Data = point.CW1Freqs;
            Freq2.Data = point.CW2Freqs;

            CWChart.UpdateChartAndDataFlow(isAutoScale);
        }
        #endregion

        #region 移动并测量
        private void MovetoP1(object sender, RoutedEventArgs e)
        {
            double x = 0, y = 0, z = 0, angle = 0;
            try
            {
                x = double.Parse(PredictParams.GetCellValue(0, 0).ToString());
                y = double.Parse(PredictParams.GetCellValue(0, 1).ToString());
                z = double.Parse(PredictParams.GetCellValue(0, 2).ToString());
                angle = double.Parse(PredictParams.GetCellValue(0, 3).ToString());
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("移动目标位置非法，无法进行移动", Window.GetWindow(this));
                return;
            }

            Thread t = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    P1Btn.IsEnabled = false;
                    P2Btn.IsEnabled = false;
                });
                CWPointObject point = MoveAndScan(x, y, z, angle, null);
                if (point != null)
                {
                    CWPoint1 = point;
                }
                Dispatcher.Invoke(() =>
                {
                    UpdateChartAndDataFlow(true);
                    P1Btn.IsEnabled = true;
                    P2Btn.IsEnabled = true;
                });
            });
            t.Start();
        }

        private void MovetoP2(object sender, RoutedEventArgs e)
        {
            double x = 0, y = 0, z = 0, angle = 0;
            try
            {
                x = double.Parse(PredictParams.GetCellValue(1, 0).ToString());
                y = double.Parse(PredictParams.GetCellValue(1, 1).ToString());
                z = double.Parse(PredictParams.GetCellValue(1, 2).ToString());
                angle = double.Parse(PredictParams.GetCellValue(1, 3).ToString());
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("移动目标位置非法，无法进行移动", Window.GetWindow(this));
                return;
            }

            Thread t = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    P1Btn.IsEnabled = false;
                    P2Btn.IsEnabled = false;
                });
                CWPointObject point = MoveAndScan(x, y, z, angle, null);
                if (point != null)
                {
                    CWPoint2 = point;
                }
                Dispatcher.Invoke(() =>
                {
                    UpdateChartAndDataFlow(true);
                    P1Btn.IsEnabled = true;
                    P2Btn.IsEnabled = true;
                });
            });
            t.Start();
        }

        /// <summary>
        /// 移动到目标位置并进行实验
        /// </summary>
        public CWPointObject MoveAndScan(double x, double y, double z, double a, Action StopMethod)
        {
            NanoStageInfo moverx = null;
            NanoStageInfo movery = null;
            NanoStageInfo moverz = null;
            NanoStageInfo movera = null;

            try
            {
                MagnetScanConfigParams P = new MagnetScanConfigParams();
                Dispatcher.Invoke(() =>
                {
                    P.ReadFromPage(new FrameworkElement[] { ParentPage });
                });
                //移动位移台
                moverx = DeviceDispatcher.GetMoverDevice(P.XRelate.Value, PartTypes.Magnnet);
                if (moverx == null)
                {
                    return null;
                }
                movery = DeviceDispatcher.GetMoverDevice(P.YRelate.Value, PartTypes.Magnnet);
                if (movery == null)
                {
                    return null;
                }
                moverz = DeviceDispatcher.GetMoverDevice(P.ZRelate.Value, PartTypes.Magnnet);
                if (moverz == null)
                {
                    return null;
                }
                movera = DeviceDispatcher.GetMoverDevice(P.ARelate.Value, PartTypes.Magnnet);
                if (movera == null)
                {
                    return null;
                }

                DeviceDispatcher.UseDevices(moverx, movery, moverz, movera);

                ScanHelper.Move(moverx, StopMethod, P.XRangeLo.Value, P.XRangeHi.Value, x, 10000);
                ScanHelper.Move(movery, StopMethod, P.YRangeLo.Value, P.YRangeHi.Value, y, 10000);
                ScanHelper.Move(moverz, StopMethod, P.ZRangeLo.Value, P.ZRangeHi.Value, z, 10000);
                ScanHelper.Move(movera, StopMethod, -150, 150, a, 10000);
                //测量
                LabviewConverter.AutoTrace(out Exception e);
                MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out List<double> freqs1, out List<double> contracts1, out List<double> freqs2, out List<double> contracts2);

                CWPointObject point = new CWPointObject(0, Math.Min(peaks[0], peaks[1]), Math.Max(peaks[0], peaks[1]), P.D.Value, freqs1, contracts1, freqs2, contracts2);

                moverx.EndUse();
                movery.EndUse();
                moverz.EndUse();
                movera.EndUse();

                return point;

            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("移动并测量未完成:" + ex.Message, Window.GetWindow(this));
            }

            return null;

        }
        #endregion

        private void ReCalculate(object sender, RoutedEventArgs e)
        {
            UpdateCalculate();
        }
    }
}
