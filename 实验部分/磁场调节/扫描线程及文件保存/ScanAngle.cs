using ODMR_Lab.位移台部分;
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

            session.BeginScan(-140, 140, -150, 150, 15, 0.1, 0, 100, this, 0.0, 0.0);

            #region 进行拟合得到方位角
            List<double> locs = AnglePoints.Select((x) => x.MoverLoc).ToList();
            List<double> bs = AnglePoints.Select((x) => x.B).ToList();
            List<double> sindata = ExpPage.AngleWin.ConvertAbsDataToSin(AnglePoints.Select((x) => x.Bp).ToList());
            for (int i = 0; i < locs.Count; i++)
            {
                locs[i] = GetReverseNum(Config.ReverseA.Value) * locs[i] - Config.AngleY;
            }
            List<double> result = MagnetAutoScanHelper.FitSinCurve(locs, sindata);
            double amplitude = result[0];
            double phase = result[1];
            double B = bs.Average();
            //拟合参数A
            double sint = amplitude / B;
            if (sint > 1) sint = 1;
            if (sint < -1) sint = -1;
            Param.Theta1.Value = Math.Abs(Math.Asin(sint) * 180 / Math.PI);
            Param.Theta2.Value = 180 - Param.Theta1.Value;
            Param.Phi1.Value = phase;
            Param.Phi2.Value = Param.Phi1.Value + 180;
            while (Param.Phi2.Value > 360)
            {
                Param.Phi2.Value -= 360;
            }
            while (Param.Phi2.Value < 360)
            {
                Param.Phi2.Value += 360;
            }
            #endregion

            #region 在界面上更新
            App.Current.Dispatcher.Invoke(() =>
            {
                ExpPage.AngleWin.CWPoints.Clear(false);
                ExpPage.AngleWin.CWPoints.AddRange(AnglePoints);
                ExpPage.AngleWin.UpdateChartAndDataFlow(true);
            });
            #endregion
        }
    }
}
