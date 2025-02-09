using ODMR_Lab.Python.LbviewHandler;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.磁场调节;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ODMR_Lab.实验部分.磁场调节
{
    public partial class MagnetScanExpObject : ExperimentObject<MagnetScanExpParams, MagnetScanConfigParams>
    {
        /// <summary>
        /// 角度检查
        /// </summary>
        private void CheckAngle()
        {
            ///刷新计算结果
            MagnetAutoScanHelper.CalculatePossibleLocs(Config, Param, out double x1, out double y1, out double z1, out double a1, out double x2, out double y2, out double z2, out double a2);

            App.Current.Dispatcher.Invoke(() =>
            {
                ExpPage.CheckWin.UpdateCalculateView(x1, y1, z1, a1, x2, y2, z2, a2);
                ExpPage.CheckWin.UpdateChartAndDataFlow(true);
            });

            #region 移动并测量第一个点
            ScanHelper.Move(XStage, JudgeThreadEndOrResume, Config.XRangeLo.Value, Config.XRangeHi.Value, x1, 10000);
            ScanHelper.Move(YStage, JudgeThreadEndOrResume, Config.YRangeLo.Value, Config.YRangeHi.Value, y1, 10000);
            ScanHelper.Move(ZStage, JudgeThreadEndOrResume, Config.ZRangeLo.Value, Config.ZRangeHi.Value, z1, 10000);
            ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, a1, 10000);
            //测量
            LabviewConverter.AutoTrace(out Exception e);
            MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out List<double> freqs1, out List<double> contracts1, out List<double> freqs2, out List<double> contracts2);

            CWPointObject p1 = new CWPointObject(0, Math.Min(peaks[0], peaks[1]), Math.Max(peaks[0], peaks[1]), Config.D.Value, freqs1, contracts1, freqs2, contracts2);
            CheckPoints.Add(p1);
            App.Current.Dispatcher.Invoke(() =>
            {
                ExpPage.CheckWin.CWPoint1 = p1;
                ExpPage.CheckWin.UpdateChartAndDataFlow(true);
            });
            #endregion

            #region 移动并测量第二个点
            ScanHelper.Move(XStage, JudgeThreadEndOrResume, Config.XRangeLo.Value, Config.XRangeHi.Value, x2, 10000);
            ScanHelper.Move(YStage, JudgeThreadEndOrResume, Config.YRangeLo.Value, Config.YRangeHi.Value, y2, 10000);
            ScanHelper.Move(ZStage, JudgeThreadEndOrResume, Config.ZRangeLo.Value, Config.ZRangeHi.Value, z2, 10000);
            ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, a2, 10000);
            //测量
            LabviewConverter.AutoTrace(out e);
            MagnetAutoScanHelper.TotalCWPeaks2OrException(out peaks, out freqs1, out contracts1, out freqs2, out contracts2);

            CWPointObject p2 = new CWPointObject(0, Math.Min(peaks[0], peaks[1]), Math.Max(peaks[0], peaks[1]), Config.D.Value, freqs1, contracts1, freqs2, contracts2);
            CheckPoints.Add(p2);
            App.Current.Dispatcher.Invoke(() =>
            {
                ExpPage.CheckWin.CWPoint2 = p2;
                ExpPage.CheckWin.UpdateChartAndDataFlow(true);
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
                Param.CheckedPhi.Value = Param.Phi1.Value;
                Param.CheckedTheta.Value = Param.Theta1.Value;
            }
            else
            {
                Param.CheckedPhi.Value = Param.Phi2.Value;
                Param.CheckedTheta.Value = Param.Theta2.Value;
            }
            #endregion 

            #region 刷新界面
            App.Current.Dispatcher.Invoke(() =>
            {
                ExpPage.CheckWin.CWPoint1 = p1;
                ExpPage.CheckWin.CWPoint2 = p2;
                ExpPage.CheckWin.UpdateChartAndDataFlow(true);
            });
            #endregion
        }

    }
}
