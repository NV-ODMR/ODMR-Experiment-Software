using ODMR_Lab.Python.LbviewHandler;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.磁场调节;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ODMR_Lab.设备部分.位移台部分;

namespace ODMR_Lab.实验部分.磁场调节
{
    public partial class MagnetScanExpObject : ExperimentObject<MagnetScanExpParams, MagnetScanConfigParams>
    {
        #region 扫描实验步骤(扫谱后保存数据点)
        private List<object> ScanEvent(NanoStageInfo stage, D1ScanRangeBase r, double loc, List<object> originOutput)
        {
            return Experiment(stage, loc, 10, originOutput);
        }

        private List<object> ScanAngleEvent(NanoStageInfo stage, D1ScanRangeBase r, double loc, List<object> originOutput)
        {
            // 获取偏心修正后的x,y位置
            List<double> xy = GetRealXYLoc(loc, Param.XLoc.Value, Param.YLoc.Value);
            // 设置XY
            ScanHelper.Move(XStage, JudgeThreadEndOrResumeAction, Config.XRangeLo.Value, Config.XRangeHi.Value, xy[0], 10000);
            ScanHelper.Move(YStage, JudgeThreadEndOrResumeAction, Config.YRangeLo.Value, Config.YRangeHi.Value, xy[0], 10000);

            return Experiment(stage, loc, 30, originOutput);
        }

        private List<object> Experiment(NanoStageInfo stage, double loc, double scanWidth, List<object> originOutput)
        {
            //移动位移台
            if (stage == XStage)
            {
                ScanHelper.Move(stage, JudgeThreadEndOrResumeAction, Config.XRangeLo.Value, Config.XRangeHi.Value, loc, 5000);
            }
            if (stage == YStage)
            {
                ScanHelper.Move(stage, JudgeThreadEndOrResumeAction, Config.YRangeLo.Value, Config.YRangeHi.Value, loc, 5000);
            }
            if (stage == ZStage)
            {
                ScanHelper.Move(stage, JudgeThreadEndOrResumeAction, Config.ZRangeLo.Value, Config.ZRangeHi.Value, loc, 5000);
            }

            if (stage == AStage)
            {
                ScanHelper.Move(stage, JudgeThreadEndOrResume, -150, 150, loc, 5000);
            }

            List<double> freqs1 = new List<double>();
            List<double> contracts1 = new List<double>();
            List<double> freqs2 = new List<double>();
            List<double> contracts2 = new List<double>();
            //频率未确定
            if ((double)originOutput[1] == 0 && (double)originOutput[2] == 0)
            {
                LabviewConverter.AutoTrace(out Exception e);
                JudgeThreadEndOrResumeAction();
                MagnetAutoScanHelper.TotalCWPeaks2OrException(out List<double> peaks, out freqs1, out contracts1, out freqs2, out contracts2);
                JudgeThreadEndOrResumeAction();

                originOutput[1] = Math.Min(peaks[0], peaks[1]);
                originOutput[2] = Math.Max(peaks[0], peaks[1]);
            }
            else
            {
                LabviewConverter.AutoTrace(out Exception exc);
                JudgeThreadEndOrResumeAction();
                MagnetAutoScanHelper.ScanCW2(out double cw1, out double cw2, out freqs1, out contracts1, out freqs2, out contracts2, (double)originOutput[1], (double)originOutput[2], scanWidth);
                JudgeThreadEndOrResumeAction();
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

            MagnetScanExpObject obj = originOutput[0] as MagnetScanExpObject;
            CWPointObject point = new CWPointObject(loc, (double)originOutput[1], (double)originOutput[2], obj.Config.D.Value, freqs1, contracts1, freqs2, contracts2);

            if (stage == XStage)
            {
                obj.XPoints.Add(point);
                App.Current.Dispatcher.Invoke(() =>
                {
                    ExpPage.XWin.CWPoints.Add(point);
                    ExpPage.XWin.UpdateChartAndDataFlow(true);
                });
            }
            if (stage == YStage)
            {
                obj.YPoints.Add(point);
                App.Current.Dispatcher.Invoke(() =>
                {
                    ExpPage.YWin.CWPoints.Add(point);
                    ExpPage.YWin.UpdateChartAndDataFlow(true);
                });
            }
            if (stage == ZStage)
            {
                obj.ZPoints.Add(point);
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (obj.ZPoints.Count == 1)
                    {
                        ExpPage.ZWin.CWPoint1 = point;
                    }
                    else
                    {
                        ExpPage.ZWin.CWPoint2 = point;
                    }
                    ExpPage.ZWin.UpdateChartAndDataFlow(true);
                });
            }
            if (stage == AStage)
            {
                obj.AnglePoints.Add(point);
                App.Current.Dispatcher.Invoke(() =>
                {
                    ExpPage.AngleWin.CWPoints.Add(point);
                    ExpPage.AngleWin.UpdateChartAndDataFlow(true);
                });
            }

            return originOutput;
        }

        private List<double> GetRealXYLoc(double angle, double centerx, double centery)
        {
            List<double> xy = MagnetAutoScanHelper.GetTargetOffset(Config, angle);
            return new List<double>() { centerx + xy[0], centery + xy[1] };
        }
        #endregion
    }
}
