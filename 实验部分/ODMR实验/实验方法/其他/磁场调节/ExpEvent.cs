using ODMR_Lab.实验部分.扫描基方法;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.磁场调节;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using System.Threading;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {
        #region 扫描实验步骤(扫谱后保存数据点)
        private List<object> ScanEvent(NanoStageInfo stage, D1NumricScanRangeBase r, double loc, List<object> originOutput)
        {
            return Experiment(stage, loc, 10, originOutput);
        }

        private List<object> ScanAngleEvent(NanoStageInfo stage, D1NumricScanRangeBase r, double loc, List<object> originOutput)
        {
            // 获取偏心修正后的x,y位置
            List<double> xy = GetRealXYLoc(loc, GetOutputParamValueByName("XLoc"), GetOutputParamValueByName("YLoc"));
            // 设置XY
            (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(xy[0], 5000);
            (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(xy[1], 5000);

            return Experiment(stage, loc, 150, originOutput);
        }

        private List<object> Experiment(NanoStageInfo stage, double loc, double scanWidth, List<object> originOutput)
        {
            stage.Device.MoveToAndWait(loc, 60000);

            //画图
            if (stage == GetDeviceByName("MagnetX"))
            {
                SetExpState("正在扫描X轴，当前位置:" + Math.Round(loc, 5).ToString());
            }
            if (stage == GetDeviceByName("MagnetY"))
            {
                SetExpState("正在扫描Y轴，当前位置:" + Math.Round(loc, 5).ToString());
            }
            if (stage == GetDeviceByName("MagnetZ"))
            {
                SetExpState("正在扫描Z轴，当前位置:" + Math.Round(loc, 5).ToString());
            }
            if (stage == GetDeviceByName("MagnetAngle"))
            {
                SetExpState("正在扫描角度，当前位置:" + Math.Round(loc, 5).ToString());
            }

            List<double> freqs = new List<double>();
            List<double> contracts = new List<double>();
            //频率未确定
            if ((double)originOutput[1] == 0 && (double)originOutput[2] == 0)
            {
                //AutoTrace
                RunSubExperimentBlock(0, true);
                JudgeThreadEndOrResumeAction();
                TotalCWPeaks2OrException(out List<double> peaks, out freqs, out contracts);
                JudgeThreadEndOrResumeAction();

                originOutput[1] = Math.Min(peaks[0], peaks[1]);
                originOutput[2] = Math.Max(peaks[0], peaks[1]);
            }
            else
            {
                //AutoTrace
                RunSubExperimentBlock(0, true);
                JudgeThreadEndOrResumeAction();
                ScanCW2(out double cw1, out double cw2, out freqs, out contracts, (double)originOutput[1], (double)originOutput[2], 10, scanWidth);
                JudgeThreadEndOrResumeAction();
                if (cw1 == 0 || cw2 == 0 || Math.Abs(cw1 - cw2) < 5)
                {
                    TotalCWPeaks2OrException(out List<double> peaks, out freqs, out contracts);
                    originOutput[1] = Math.Min(peaks[0], peaks[1]);
                    originOutput[2] = Math.Max(peaks[0], peaks[1]);
                    //未扫描到谱峰，添加提示信息
                    MessageLogger.AddLogger("磁场定位", "位移台" + stage.MoverType.ToString() + "=" + Math.Round(loc, 5).ToString() + "时未扫描到完整的共振峰谱", MessageTypes.Information);
                }
                else
                {
                    originOutput[1] = cw1;
                    originOutput[2] = cw2;
                }
            }

            //画图
            if (stage == GetDeviceByName("MagnetX"))
            {
                AddMagnetDataToChart(loc, (double)originOutput[1], (double)originOutput[2], "X方向磁场信息", out double Bp, out double Bv, out double B);
                AddScanDataToChart(loc, "X", freqs, contracts);
            }
            if (stage == GetDeviceByName("MagnetY"))
            {
                AddMagnetDataToChart(loc, (double)originOutput[1], (double)originOutput[2], "Y方向磁场信息", out double Bp, out double Bv, out double B);
                AddScanDataToChart(loc, "Y", freqs, contracts);
            }
            if (stage == GetDeviceByName("MagnetZ"))
            {
                AddMagnetDataToChart(loc, (double)originOutput[1], (double)originOutput[2], "Z方向磁场信息", out double Bp, out double Bv, out double B);
                AddScanDataToChart(loc, "Z", freqs, contracts);
            }
            if (stage == GetDeviceByName("MagnetAngle"))
            {
                AddMagnetDataToChart(loc, (double)originOutput[1], (double)originOutput[2], "角度方向磁场信息", out double Bp, out double Bv, out double B);
                AddScanDataToChart(loc, "角度", freqs, contracts);
            }

            return originOutput;
        }

        private List<double> GetRealXYLoc(double angle, double centerx, double centery)
        {
            List<double> xy = GetTargetOffset(angle);
            return new List<double>() { centerx + xy[0], centery + xy[1] };
        }
        #endregion
    }
}
