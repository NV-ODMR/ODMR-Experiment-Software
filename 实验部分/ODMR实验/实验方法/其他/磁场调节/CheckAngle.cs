using ODMR_Lab.IO操作;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.设备部分.位移台部分;
using System.Collections.Generic;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;

        /// <summary>
        /// 角度检查
        /// </summary>
        private void CheckAngle(double progresslo, double progresshi)
        {
            SetProgress(progresslo);
            ///刷新计算结果
            CalculatePossibleLocs(out double x1, out double y1, out double z1, out double a1, out double x2, out double y2, out double z2, out double a2);


            List<double> peaks = new List<double>();
            List<double> freqs = new List<double>();
            List<double> contracts = new List<double>();
            double Bp1 = double.NaN;
            double Bv1 = double.NaN;
            double B1 = double.NaN;
            double Bp2 = double.NaN;
            double Bv2 = double.NaN;
            double B2 = double.NaN;

            #region 移动并测量第一个点
            try
            {
                SetExpState("移动并测量第一个点...");
                (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(x1, 10000);
                JudgeThreadEndOrResumeAction?.Invoke();
                (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(y1, 10000);
                JudgeThreadEndOrResumeAction?.Invoke();
                (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(z1, 10000);
                JudgeThreadEndOrResumeAction?.Invoke();
                (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(a1, 60000);
                JudgeThreadEndOrResumeAction?.Invoke();
                //测量
                RunSubExperimentBlock(0, true);
                JudgeThreadEndOrResumeAction?.Invoke();
                TotalCWPeaks2OrException(out peaks, out freqs, out contracts);
                JudgeThreadEndOrResumeAction?.Invoke();
                AddMagnetDataToChart(1, peaks[0], peaks[1], "角度检查信息", out Bp1, out Bv1, out B1);
                AddScanDataToChart(1, "Z", freqs, contracts);
            }
            catch (System.Exception)
            {
            }
            #endregion

            SetProgress((progresslo + progresshi) / 2);

            #region 移动并测量第二个点
            try
            {
                SetExpState("移动并测量第二个点...");
                (GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(x2, 10000);
                JudgeThreadEndOrResumeAction?.Invoke();
                (GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(y2, 10000);
                JudgeThreadEndOrResumeAction?.Invoke();
                (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(z2, 10000);
                JudgeThreadEndOrResumeAction?.Invoke();
                (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(a2, 60000);
                JudgeThreadEndOrResumeAction?.Invoke();
                //测量
                RunSubExperimentBlock(0, true);
                JudgeThreadEndOrResumeAction?.Invoke();
                TotalCWPeaks2OrException(out peaks, out freqs, out contracts);
                JudgeThreadEndOrResumeAction?.Invoke();
                AddMagnetDataToChart(2, peaks[0], peaks[1], "角度检查信息", out Bp2, out Bv2, out B2);
                AddScanDataToChart(2, "Z", freqs, contracts);
            }
            catch (System.Exception)
            {
            }
            #endregion

            #region 计算结果
            if (double.IsNaN(B1))
            {
                OutputParams.Add(new Param<double>("NV朝向θ", GetOutputParamValueByName("Theta1"), "CheckedTheta"));
                OutputParams.Add(new Param<double>("NV朝向φ", GetOutputParamValueByName("Phi2"), "CheckedPhi"));
                SetProgress(progresslo);
                return;

            }
            if (double.IsNaN(B2))
            {
                OutputParams.Add(new Param<double>("NV朝向θ", GetOutputParamValueByName("Theta1"), "CheckedTheta"));
                OutputParams.Add(new Param<double>("NV朝向φ", GetOutputParamValueByName("Phi1"), "CheckedPhi"));
                SetProgress(progresslo);
                return;

            }
            double v1 = Bp1 / B1;
            double v2 = Bp2 / B2;
            if (v1 > v2)
            {
                OutputParams.Add(new Param<double>("NV朝向θ", GetOutputParamValueByName("Theta1"), "CheckedTheta"));
                OutputParams.Add(new Param<double>("NV朝向φ", GetOutputParamValueByName("Phi1"), "CheckedPhi"));
            }
            else
            {
                OutputParams.Add(new Param<double>("NV朝向θ", GetOutputParamValueByName("Theta1"), "CheckedTheta"));
                OutputParams.Add(new Param<double>("NV朝向φ", GetOutputParamValueByName("Phi2"), "CheckedPhi"));
            }
            SetProgress(progresslo);
            #endregion
        }

    }
}
