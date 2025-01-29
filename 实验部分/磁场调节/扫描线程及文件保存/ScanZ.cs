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
        /// Z方向扫描
        /// </summary>
        private void ScanZ()
        {
            //扫第一个点
            ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, Config.AngleX, 60000, 360);
            Scan1DSession session = new Scan1DSession();
            session.ProgressBarMethod = SetProgressFromSession;
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.StateJudgeEvent = JudgeThreadEndOrResume;
            session.ScanMover = ZStage;

            double reslo = Config.ZRangeLo.Value;
            double reshi = Config.ZRangeHi.Value;

            //扫第一个点
            double height = Config.ZPlane.Value;
            session.BeginScan(height, height, reslo, reshi, 1, 0.1, 0, 25, this, 0.0, 0.0);
            //扫第二个点
            height = Config.ZPlane.Value + GetReverseNum(Config.ReverseZ.Value);
            session.BeginScan(height, height, reslo, reshi, 1, 0.1, 25, 50, this, 0.0, 0.0);

            CWPointObject cp1 = ZPoints[0];
            CWPointObject cp2 = ZPoints[1];

            #region 如果NV磁场分量太小则转90度再测
            if (cp1.Bv != 0 && cp2.Bv != 0)
            {
                if (cp1.Bp / cp1.Bv < 0.1 || cp2.Bp / cp2.Bv < 0.1)
                {
                    ScanHelper.Move(AStage, JudgeThreadEndOrResume, -150, 150, Config.AngleY, 60000, 360);

                    ZPoints.Clear();

                    //扫第一个点
                    height = Config.ZPlane.Value;
                    session.BeginScan(height, height, reslo, reshi, 1, 0.1, 50, 75, this, 0.0, 0.0);
                    //扫第二个点
                    height = Config.ZPlane.Value + GetReverseNum(Config.ReverseZ.Value);
                    session.BeginScan(height, height, reslo, reshi, 1, 0.1, 75, 100, this, 0.0, 0.0);

                    cp1 = ZPoints[0];
                    cp2 = ZPoints[1];
                }
            }
            #endregion


            #region 计算位置
            double ratio = cp1.Bp / cp2.Bp;
            Param.ZLoc.Value = cp1.MoverLoc;
            if (ratio < 1)
            {
                ratio = cp2.Bp / cp1.Bp;
                Param.ZLoc.Value = cp2.MoverLoc;
            }
            if (double.IsInfinity(ratio))
            {
                ratio = cp1.B / cp2.B;
                Param.ZLoc.Value = cp1.MoverLoc;
                if (ratio < 1)
                {
                    ratio = cp2.B / cp1.B;
                    Param.ZLoc.Value = cp2.MoverLoc;
                }
            }
            Param.ZDistance.Value = MagnetAutoScanHelper.FindRoot(Config.MRadius.Value, Config.MLength.Value, cp1.MoverLoc, cp2.MoverLoc, ratio);
            #endregion

            #region 刷新界面
            App.Current.Dispatcher.Invoke(() =>
            {
                ExpPage.ZWin.CWPoint1 = cp1;
                ExpPage.ZWin.CWPoint2 = cp2;
                ExpPage.ZWin.UpdateChartAndDataFlow(true);
            });
            #endregion
        }

    }
}
