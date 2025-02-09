using System;
using System.IO;
using MathNet.Numerics;
using System.Security.Cryptography;
using MathNet.Numerics.RootFinding;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Collections.Generic;
using ODMR_Lab.Python.LbviewHandler;
using static System.Net.WebRequestMethods;
using System.Linq;
using PythonHandler;
using ODMR_Lab.磁场调节;
using System.Windows;
using ODMR_Lab.实验部分.磁场调节.主窗口;

namespace ODMR_Lab.实验部分.磁场调节
{
    /// <summary>
    /// 圆柱形磁铁类
    /// </summary>
    public class MagnetAutoScanHelper
    {

        // 旋磁比(Mhz/Gauss)
        private static double gammaE = 2.803;

        /// <summary>
        /// 根据谱峰位置计算横向和纵向的磁场大小
        /// </summary>
        /// <param name="freq1"></param>
        /// <param name="freq2"></param>
        /// <param name="D"></param>
        /// <param name="Bp"></param>
        /// <param name="Bv"></param>
        public static void CalculateB(double freq1, double freq2, out double Bp, out double Bv, double D = 2870)
        {
            double detplus = (freq2 + freq1) / 2;
            double detminus = Math.Abs(freq2 - freq1);
            double det1 = 2 * D * (detplus - D) / 3;
            if (det1 < 0)
                det1 = 0;
            Bv = Math.Sqrt(det1) / gammaE;
            double det2 = detminus / Math.Pow(2 * gammaE, 2) - Math.Pow(Bv * Bv * gammaE / (2 * D), 2);
            if (det2 < 0)
                det2 = 0;
            Bp = Math.Sqrt(det2);
        }

        /// <summary>
        /// 获取初始偏移量
        /// </summary>
        /// <param name="angle1">初始角度</param>
        /// <param name="x1">初始位置X</param>
        /// <param name="y1">初始位置Y</param>
        /// <param name="angle2">转180度的角度</param>
        /// <param name="x2">之后的位置X</param>
        /// <param name="y2">之后的位置Y</param>
        /// <param name="reverseX">是否反转X</param>
        /// <param name="reverseY">是否反转Y</param>
        /// <param name="reverseAngle">是否反转角度</param>
        /// <param name="initAngle">角度起始点</param>
        /// <param name="offsetx">X偏移量</param>
        /// <param name="offsety">Y偏移量</param>
        public static void GetZeroOffset(double angle1, double x1, double y1, double angle2, double x2, double y2, bool reverseX, bool reverseY, bool reverseAngle, double initAngle, out double offsetx, out double offsety)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;

            double reverseFacX = reverseX ? -1 : 1;
            double reverseFacY = reverseY ? -1 : 1;
            double reverseFacAngle = reverseAngle ? -1 : 1;

            double x = 0.5 * reverseFacX * dx;
            double y = 0.5 * reverseFacY * dy;

            double r = Math.Sqrt(x * x + y * y);

            double theta = Math.Atan2(y, x) * 180 / Math.PI;

            double angle = theta + reverseFacAngle * (initAngle - angle1);


            offsetx = r * Math.Cos(angle * Math.PI / 180);
            offsety = r * Math.Sin(angle * Math.PI / 180);
        }

        public static List<double> GetTargetOffset(MagnetScanConfigParams config, double targetAngle)
        {

            // 计算随体坐标系下的极坐标
            double r = Math.Sqrt(config.OffsetX.Value * config.OffsetX.Value + config.OffsetY.Value * config.OffsetY.Value);
            double the = Math.Atan2(config.OffsetY.Value, config.OffsetX.Value) * 180 / Math.PI;

            double the1 = the + GetReverseNum(config.ReverseA.Value) * (targetAngle - config.AngleStart.Value);

            double dx = GetReverseNum(config.ReverseX.Value) * r * (Math.Cos(the / 180 * Math.PI) - Math.Cos(the1 / 180 * Math.PI));
            double dy = GetReverseNum(config.ReverseY.Value) * r * (Math.Sin(the / 180 * Math.PI) - Math.Sin(the1 / 180 * Math.PI));

            return new List<double>() { dx, dy };
        }


        private static void TotalCW(out List<double> peaks, out List<double> freqs1, out List<double> contracts1, out List<double> freqs2, out List<double> contracts2)
        {
            //粗扫
            LabviewConverter.CW(out List<double> freqs, out List<double> contracts, out List<double> ps, out List<double> fitcontracts, out Exception exc, 2600, 3200, 3, 500, 2);

            if (ps.Count == 1)
            {
                peaks = new List<double>();
                //细扫
                LabviewConverter.CW(out freqs1, out contracts1, out List<double> peaks1, out List<double> fitcontracts1, out exc, ps[0] - 10, ps[0] + 10, 1, 2000, 1, isReverse: false);
                if (peaks1.Count != 0)
                {
                    peaks.Add(peaks1.Min());
                }
                LabviewConverter.CW(out freqs2, out contracts2, out peaks1, out fitcontracts1, out exc, ps[0] - 10, ps[0] + 10, 1, 2000, 1, isReverse: true);
                if (peaks1.Count != 0)
                {
                    peaks.Add(peaks1.Max());
                }
                return;
            }
            if (ps.Count == 2)
            {
                peaks = new List<double>();
                double minfreq = Math.Min(ps[0], ps[1]);
                double maxfreq = Math.Max(ps[0], ps[1]);
                LabviewConverter.CW(out freqs1, out contracts1, out List<double> peaks1, out List<double> fitcontracts1, out exc, minfreq - 5, minfreq + 5, 1, 2000, 1, scanRangeAfterstop: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    peaks.Add(peaks1.Min());
                }
                LabviewConverter.CW(out freqs2, out contracts2, out peaks1, out fitcontracts1, out exc, maxfreq - 5, maxfreq + 5, 1, 2000, 1, scanRangeAfterstop: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    peaks.Add(peaks1.Max());
                }
                return;
            }
            peaks = new List<double>();
            freqs1 = new List<double>();
            contracts1 = new List<double>();
            freqs2 = new List<double>();
            contracts2 = new List<double>();
            exc = null;
            return;
        }

        /// <summary>
        /// 全谱扫两个共振峰，扫不全则报错
        /// </summary>
        /// <param name="peaks"></param>
        /// <param name="freqs1"></param>
        /// <param name="contracts1"></param>
        /// <param name="freqs2"></param>
        /// <param name="contracts2"></param>
        /// <exception cref="Exception"></exception>
        public static void TotalCWPeaks2OrException(out List<double> peaks, out List<double> freqs1, out List<double> contracts1, out List<double> freqs2, out List<double> contracts2)
        {
            TotalCW(out peaks, out freqs1, out contracts1, out freqs2, out contracts2);
            if (peaks.Count == 0)
            {
                //没有扫到峰，报错
                throw new Exception("未扫描到完整的谱峰");
            }
            if (peaks.Count == 1)
            {
                peaks = new List<double>() { peaks[0], peaks[0] };
                return;
            }
        }

        // 扫描出CW谱的准确峰位置
        // 先粗扫后细扫(步长先设置为2MHz,确定峰位置之后按步长0.2MHz扫描)
        // 范围限制,扫描范围在2400Mhz到3400Mhz之间
        private static void ScanCW(out List<double> peaks, out List<double> freqvalues, out List<double> contractvalues, double peakapprox, double scanWidth = 30, bool isReverse = false)
        {
            if (peakapprox < 2100 || peakapprox > 3500)
            {
                peaks = new List<double>();
                freqvalues = new List<double>();
                contractvalues = new List<double>();
                return;
            }
            peaks = new List<double>();
            LabviewConverter.CW(out freqvalues, out contractvalues, out List<double> freqs, out List<double> fitcontracts, out Exception exc, startFreq: peakapprox - scanWidth, endFreq: peakapprox + scanWidth, step: 2, averagetime: 500, peakcount: 1, isReverse: isReverse);

            double f1 = 0;
            if (freqs.Count != 0)
            {
                f1 = freqs[0];
                if (freqs.Count == 2)
                {
                    f1 = isReverse ? Math.Max(freqs[0], freqs[1]) : Math.Min(freqs[0], freqs[1]);
                }
                //如果扫到的峰在范围边缘则扩大范围扫描
                if (Math.Abs(f1 - peakapprox + scanWidth) < 3)
                {
                    LabviewConverter.CW(out freqvalues, out contractvalues, out freqs, out fitcontracts, out exc, startFreq: peakapprox - scanWidth - 20, endFreq: peakapprox - scanWidth + 20, step: 2, averagetime: 500, peakcount: 1, isReverse: isReverse);
                    if (freqs.Count != 0)
                    {
                        //细扫
                        LabviewConverter.CW(out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, out exc, startFreq: freqs[0] - 5, endFreq: freqs[0] + 5, step: 1, averagetime: 2000, peakcount: 1, scanRangeAfterstop: 10, isReverse: isReverse);
                        if (f1s.Count != 0)
                        {
                            peaks.Add(f1s[0]);
                        }
                    }
                }
                if (Math.Abs(f1 - peakapprox - scanWidth) < 3)
                {
                    LabviewConverter.CW(out freqvalues, out contractvalues, out freqs, out fitcontracts, out exc, startFreq: peakapprox + scanWidth - 20, endFreq: peakapprox + scanWidth + 20, step: 2, averagetime: 500, peakcount: 1, isReverse: isReverse);
                    if (freqs.Count != 0)
                    {
                        LabviewConverter.CW(out freqvalues, out fitcontracts, out List<double> f1s, out fitcontracts, out exc, startFreq: freqs[0] - 5, endFreq: freqs[0] + 5, step: 1, averagetime: 2000, peakcount: 1, scanRangeAfterstop: 10, isReverse: isReverse);
                        if (f1s.Count != 0)
                        {
                            peaks.Add(f1s[0]);
                        }
                    }
                }
                if (Math.Abs(f1 - peakapprox + scanWidth) >= 3 && Math.Abs(f1 - peakapprox - scanWidth) >= 3)
                {
                    LabviewConverter.CW(out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, out exc, startFreq: f1 - 5, endFreq: f1 + 5, step: 1, averagetime: 2000, peakcount: 1, scanRangeAfterstop: 10, isReverse: isReverse);
                    if (f1s.Count != 0)
                    {
                        peaks.Add(f1s[0]);
                    }
                }
            }
        }


        /// <summary>
        /// 扫两频点的CW谱
        /// </summary>
        public static void ScanCW2(out double peakout1, out double peakout2, out List<double> freqsout1, out List<double> contrastout1, out List<double> freqsout2, out List<double> contrastout2, double peakapprox1, double peakapprox2, double scanWidth = 30)
        {
            //寻找峰值较小值，正向扫描
            double p1 = Math.Min(peakapprox1, peakapprox2);
            double p2 = Math.Max(peakapprox1, peakapprox2);
            ScanCW(out List<double> peak1, out freqsout1, out contrastout1, p1, scanWidth, isReverse: false);
            ScanCW(out List<double> peak2, out freqsout2, out contrastout2, p2, scanWidth, isReverse: true);

            if (peak1.Count == 0 || peak2.Count == 0)
            {
                peakout1 = 0;
                peakout2 = 0;
                freqsout1 = new List<double>();
                freqsout2 = new List<double>();
                contrastout1 = new List<double>();
                contrastout2 = new List<double>();
                return;
            }
            peakout1 = peak1.Min();
            peakout2 = peak2.Max();
        }


        /// <summary>
        /// 用二次函数拟合
        /// </summary>
        public static List<double> FitDataWithPow2(List<double> xdata, List<double> ydata)
        {
            if (xdata.Count == 0 || ydata.Count == 0)
            {
                return new List<double>();
            }
            List<double> result = Fit.Polynomial(xdata.ToArray(), ydata.ToArray(), 2).ToList();
            return result;
        }

        public static int GetNearestDataIndex(List<double> dataarray, double targetvalue)
        {
            List<double> cal = new List<double>();
            for (int i = 0; i < dataarray.Count; i++)
            {
                cal.Add(Math.Abs(dataarray[i] - targetvalue));
            }
            return cal.IndexOf(cal.Min());
        }

        /// <summary>
        /// 根据参数计算可能的两个NV朝向对应的XYZ和角度位移台位置
        /// </summary>
        /// <param name="P"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="angle1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <param name="angle2"></param>
        public static void CalculatePossibleLocs(MagnetScanConfigParams Config, MagnetScanExpParams Param, out double x1, out double y1, out double z1, out double angle1, out double x2, out double y2, out double z2, out double angle2)
        {
            Magnet m = new Magnet(Config.MRadius.Value, Config.MLength.Value, 1);
            #region 第一个方向
            List<double> res = m.FindDire(Param.Theta1.Value, Param.Phi1.Value, Param.ZDistance.Value);
            double ang = res[0] * GetReverseNum(Config.ReverseA.Value);
            double dx = res[1] * GetReverseNum(Config.ReverseX.Value);
            double dy = res[2] * GetReverseNum(Config.ReverseY.Value);
            ang = Config.AngleStart.Value + ang;
            while (ang > 360)
            {
                ang -= 360;
            }
            while (ang < -360)
            {
                ang += 360;
            }
            if (Math.Abs(ang) > 150)
            {
                res = m.FindDire(Math.PI - Param.Theta1.Value, Param.Phi1.Value - Math.PI, Param.ZDistance.Value);
                ang = res[0] * GetReverseNum(Config.ReverseA.Value);
                dx = res[1] * GetReverseNum(Config.ReverseX.Value);
                dy = res[2] * GetReverseNum(Config.ReverseY.Value);
                ang = Config.AngleStart.Value + ang;
                while (ang > 360)
                {
                    ang -= 360;
                }
                while (ang < -360)
                {
                    ang += 360;
                }
            }
            //根据需要移动的角度进行偏心修正
            List<double> re = GetTargetOffset(Config, ang);
            x1 = Param.XLoc.Value + re[0] + dx;
            y1 = Param.YLoc.Value + re[1] + dy;
            z1 = Param.ZLoc.Value;
            angle1 = ang;

            #endregion

            #region 第二个方向
            res = m.FindDire(Param.Theta2.Value, Param.Phi1.Value, Param.ZDistance.Value);
            ang = res[0] * GetReverseNum(Config.ReverseA.Value);
            dx = res[1] * GetReverseNum(Config.ReverseX.Value);
            dy = res[2] * GetReverseNum(Config.ReverseY.Value);
            ang = Config.AngleStart.Value + ang;
            while (ang > 360)
            {
                ang -= 360;
            }
            while (ang < -360)
            {
                ang += 360;
            }
            if (Math.Abs(ang) > 150)
            {
                res = m.FindDire(Math.PI - Param.Theta2.Value, Param.Phi1.Value - Math.PI, Param.ZDistance.Value);
                ang = res[0] * GetReverseNum(Config.ReverseA.Value);
                dx = res[1] * GetReverseNum(Config.ReverseX.Value);
                dy = res[2] * GetReverseNum(Config.ReverseY.Value);
                ang = Config.AngleStart.Value + ang;
                while (ang > 360)
                {
                    ang -= 360;
                }
                while (ang < -360)
                {
                    ang += 360;
                }
            }
            //根据需要移动的角度进行偏心修正
            re = MagnetAutoScanHelper.GetTargetOffset(Config, ang);
            x2 = Param.XLoc.Value + re[0] + dx;
            y2 = Param.YLoc.Value + re[1] + dy;
            z2 = Param.ZLoc.Value;
            angle2 = ang;

            #endregion
        }

        private static int GetReverseNum(bool value)
        {
            return value ? 1 : -1;
        }

        /// <summary>
        /// 计算预测磁场
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CalculatePredictField(PredictData PData)
        {
            try
            {
                double the = PData.ThetaPredictInput;
                double phi = PData.PhiPredictInput;
                double currentzloc = PData.ZPredictHeightInput;

                MagnetScanConfigParams CP = PData.ConfigParams;
                MagnetScanExpParams EP = PData.Params;


                double zdis = GetReverseNum(CP.ReverseZ.Value) * (currentzloc - EP.ZLoc.Value) + EP.ZDistance.Value;

                Magnet m = new Magnet(CP.MRadius.Value, CP.MLength.Value, 1);

                List<double> res = m.FindDire(the, phi, currentzloc);
                double ang = res[0];
                double dx = res[1];
                double dy = res[2];
                double B = res[3];
                double dz = zdis - EP.ZDistance.Value;
                dx *= GetReverseNum(CP.ReverseX.Value);
                dy *= GetReverseNum(CP.ReverseY.Value);
                dz *= GetReverseNum(CP.ReverseZ.Value);
                ang *= GetReverseNum(CP.ReverseA.Value);
                double ang1 = CP.AngleStart.Value + ang;

                List<double> doffs = GetTargetOffset(CP, ang1);
                double doffx = doffs[0];
                double doffy = doffs[1];

                if (ang1 > 150)
                    ang1 -= 360;
                if (ang1 < -150)
                    ang1 += 360;

                //角度超量程,取等效位置
                if (Math.Abs(ang1) > 150)
                {
                    res = m.FindDire(180 - the, phi + 180, zdis);
                    ang = res[0];
                    dx = res[1];
                    dy = res[2];
                    B = res[3];
                    dz = zdis - EP.ZDistance.Value;
                    dx *= GetReverseNum(CP.ReverseX.Value);
                    dy *= GetReverseNum(CP.ReverseY.Value);
                    dz *= GetReverseNum(CP.ReverseZ.Value);
                    ang *= GetReverseNum(CP.ReverseA.Value);
                    ang1 = CP.AngleStart.Value + ang;

                    if (ang1 > 150)
                        ang1 -= 360;
                    if (ang1 < -150)
                        ang1 += 360;

                    //根据需要移动的角度进行偏心修正
                    doffs = MagnetAutoScanHelper.GetTargetOffset(CP, ang1);
                    doffx = doffs[0];
                    doffy = doffs[1];
                }

                PData.XLocPredictOutPut = EP.XLoc.Value + dx + doffx;
                PData.YLocPredictOutPut = EP.YLoc.Value + dy + doffy;
                PData.ZLocPredictOutPut = currentzloc;
                PData.ALocPredictOutPut = ang1;

                try
                {
                    //如果可能的话计算预测的磁场强度 
                    double Intensity = CP.MIntensity.Value;
                    PData.PredictB = Intensity * B * 10000;
                }
                catch (Exception) { }
            }
            catch (Exception ex) { return; }
        }
    }
}
