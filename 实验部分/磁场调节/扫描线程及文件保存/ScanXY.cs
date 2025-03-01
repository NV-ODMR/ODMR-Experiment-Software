using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.磁场调节;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.磁场调节
{
    public partial class MagnetScanExpObject : ExperimentObject<MagnetScanExpParams, MagnetScanConfigParams>
    {
        /// <summary>
        /// X方向扫描
        /// </summary>
        private void ScanX()
        {
            double loc = LineScanCore("X");
            // 读取磁铁角度，计算偏移量
            double angle = Config.AngleX;
            List<double> xy = MagnetAutoScanHelper.GetTargetOffset(Config, angle);
            Param.XLoc.Value = loc - xy[0];
        }

        /// <summary>
        /// Y方向扫描
        /// </summary>
        private void ScanY()
        {
            Param.YLoc.Value = LineScanCore("Y");
        }

        private double LineScanCore(string ScanDir)
        {
            //根据点数设置位移台（二分法,扫描点数始终为6)
            //遍历一遍范围，之后拟合得到最大值位置，之后范围减半
            //重复上一步骤，直到步长小于0.05mm
            //计算总点数
            double scancount = 6;
            double countApprox = 0;
            double step = (Config.XScanHi.Value - Config.XScanLo.Value) / (scancount - 1);
            while (step >= 0.1)
            {
                countApprox += scancount;
                step /= 2;
            }

            int ind = 0;
            Scan1DSession<NanoStageInfo> session = new Scan1DSession<NanoStageInfo>();
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.ProgressBarMethod = SetProgressFromSession;
            session.StateJudgeEvent = JudgeThreadEndOrResume;

            double restrictlo = 0;
            double restricthi = 0;

            if (ScanDir == "X")
            {
                session.ScanSource = XStage;
                restrictlo = Config.XScanLo.Value;
                restricthi = Config.XScanHi.Value;
            }
            if (ScanDir == "Y")
            {
                session.ScanSource = YStage;
                restrictlo = Config.YScanLo.Value;
                restricthi = Config.YScanHi.Value;
            }

            double cw1 = 0, cw2 = 0;
            double peak = 0;
            double scanmin = Config.XScanLo.Value;
            double scanmax = Config.XScanHi.Value;
            double scanrange = Math.Abs(Config.XScanHi.Value - Config.XScanLo.Value);

            step = (scanrange) / (scancount - 1);
            while (step >= 0.1)
            {
                session.BeginScan(new D1NumricLinearScanRange(scanmin, scanmax, 6), ind * 100.0 / countApprox, (ind + 5) * 100.0 / countApprox, false, this, cw1, cw2);
                ind += 6;

                #region 根据二次函数计算当前峰值
                List<double> xdata = new List<double>();
                List<double> ydata = new List<double>();
                List<double> cw1s = new List<double>();
                List<double> cw2s = new List<double>();

                if (ScanDir == "X")
                {
                    xdata = XPoints.Select((x) => x.MoverLoc).ToList();
                    ydata = XPoints.Select((x) => x.B).ToList();
                    cw1s = XPoints.Select((x) => x.CW1).ToList();
                    cw2s = XPoints.Select((x) => x.CW2).ToList();
                }
                if (ScanDir == "Y")
                {
                    xdata = YPoints.Select((x) => x.MoverLoc).ToList();
                    ydata = YPoints.Select((x) => x.B).ToList();
                    cw1s = YPoints.Select((x) => x.CW1).ToList();
                    cw2s = YPoints.Select((x) => x.CW2).ToList();
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
    }
}
