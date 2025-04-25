using System;
using System.IO;
using MathNet.Numerics;
using System.Security.Cryptography;
using MathNet.Numerics.RootFinding;
using System.Windows.Interop;
using System.Windows.Documents;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;
using System.Linq;
using PythonHandler;
using System.Windows;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.磁场调节;
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他.磁场调节.通用方法;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// AutoTrace定位NV
    /// </summary>
    public class MagnetScanTool
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
        public static void CalculateB(double D, double freq1, double freq2, out double Bp, out double Bv, out double B)
        {
            double detplus = (freq2 + freq1) / 2;
            double detminus = Math.Abs(freq2 - freq1);
            double det1 = 2 * D * (detplus - D) / 3;
            if (det1 < 0)
                det1 = 0;
            Bv = Math.Sqrt(det1) / gammaE;
            double det2 = Math.Pow(detminus / (2 * gammaE), 2) - Math.Pow(Bv * Bv * gammaE / (2 * D), 2);
            if (det2 < 0)
                det2 = 0;
            Bp = Math.Sqrt(det2);
            B = Math.Sqrt(Bp * Bp + Bv * Bv);
        }

        private static List<double> GetTargetOffset(MagnetLocParams Ps, double targetAngle)
        {

            // 计算随体坐标系下的极坐标
            double r = Math.Sqrt(Ps.OffsetX * Ps.OffsetX + Ps.OffsetY * Ps.OffsetY);
            double the = Math.Atan2(Ps.OffsetY, Ps.OffsetX) * 180 / Math.PI;

            double the1 = the + GetReverseNum(Ps.AReverse) * (targetAngle - Ps.AngleStart);

            double dx = GetReverseNum(Ps.XReverse) * r * (Math.Cos(the / 180 * Math.PI) - Math.Cos(the1 / 180 * Math.PI));
            double dy = GetReverseNum(Ps.YReverse) * r * (Math.Sin(the / 180 * Math.PI) - Math.Sin(the1 / 180 * Math.PI));

            return new List<double>() { dx, dy };
        }


        private static void TotalCW(ODMRExpObject parentexp, int CWExpIndex, out List<double> peaks, out List<double> fitcontract, out List<double> freqs, out List<double> contracts)
        {
            //粗扫
            CW(parentexp, CWExpIndex, out freqs, out contracts, out List<double> ps, out List<double> fitcontracts, 2600, 3200, 3, 500, 2, EndPointCount: 5);

            if (ps.Count == 1)
            {
                peaks = new List<double>();
                fitcontract = new List<double>();
                //细扫
                CW(parentexp, CWExpIndex, out List<double> freqs1, out List<double> contracts1, out List<double> peaks1, out List<double> fitcontracts1, ps[0] - 10, ps[0] + 10, 1, 2000, 1, EndPointCount: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs1, contracts, contracts1, out freqs, out contracts);
                    peaks.Add(peaks1.Min());
                    fitcontract.Add(fitcontracts1[peaks1.FindIndex(x => x == peaks1.Min())]);
                }
                CW(parentexp, CWExpIndex, out List<double> freqs2, out List<double> contracts2, out peaks1, out fitcontracts1, ps[0] - 10, ps[0] + 10, 1, 2000, 1, EndPointCount: 10, isReverse: true);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs2, contracts, contracts2, out freqs, out contracts);
                    peaks.Add(peaks1.Max());
                    fitcontract.Add(fitcontracts1[peaks1.FindIndex(x => x == peaks1.Max())]);
                }
                return;
            }
            if (ps.Count == 2)
            {
                peaks = new List<double>();
                fitcontract = new List<double>();
                double minfreq = Math.Min(ps[0], ps[1]);
                double maxfreq = Math.Max(ps[0], ps[1]);
                CW(parentexp, CWExpIndex, out List<double> freqs1, out List<double> contracts1, out List<double> peaks1, out List<double> fitcontracts1, minfreq - 5, minfreq + 5, 1, 2000, 1, EndPointCount: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs1, contracts, contracts1, out freqs, out contracts);
                    peaks.Add(peaks1.Min());
                    fitcontract.Add(fitcontracts1[peaks1.FindIndex(x => x == peaks1.Min())]);
                }
                CW(parentexp, CWExpIndex, out List<double> freqs2, out List<double> contracts2, out peaks1, out fitcontracts1, maxfreq - 5, maxfreq + 5, 1, 2000, 1, EndPointCount: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs2, contracts, contracts2, out freqs, out contracts);

                    peaks.Add(peaks1.Max());
                    fitcontract.Add(fitcontracts1[peaks1.FindIndex(x => x == peaks1.Max())]);
                }
                return;
            }

            peaks = new List<double>();
            freqs = new List<double>();
            contracts = new List<double>();
            fitcontract = new List<double>();
            return;
        }

        Random r = new Random();
        private static void CW(ODMRExpObject parentexp, int CWExpIndex, out List<double> Frequences, out List<double> Contracts, out List<double> fitpeaks, out List<double> fitcontracts, double startFreq, double endFreq, double step, int averagetime, int peakcount, int EndPointCount = 5, bool isReverse = false)
        {
            fitpeaks = new List<double>();
            fitcontracts = new List<double>();
            Frequences = new List<double>();
            Contracts = new List<double>();
            #region 测试代码，生成随机结果
            //double cc = r.Next(0, 40);
            //fitpeaks.Add(2870 - cc);
            //fitpeaks.Add(2870 + cc);
            //fitcontracts.Add(0.7);
            //fitcontracts.Add(0.7);
            //for (int i = 0; i < 10; i++)
            //{
            //    Frequences.Add(r.NextDouble());
            //    Contracts.Add(r.NextDouble());
            //}
            //return;
            #endregion
            List<ParamB> InputParams = new List<ParamB>()
            {
                new Param<double>("频率起始点(MHz)",startFreq,"RFFreqLo"),
                new Param<double>("频率中止点(MHz)",endFreq,"RFFreqHi"),
                new Param<double>("扫描步长(MHz)",step,"RFStep"),
                new Param<int>("扫描峰个数",peakcount,"PeakCount"),
                new Param<int>("循环次数",averagetime,"LoopCount"),
                new Param<int>("结束前扫描点数", EndPointCount, "BeforeEndScanRange"),
                new Param<bool>("反向扫描", isReverse, "Reverse"),
            };
            var exp = parentexp.RunSubExperimentBlock(CWExpIndex, true, InputParams);

            //限定对比度范围
            if (peakcount == 1)
            {
                double peak1 = exp.GetOutputParamValueByName("PeakLoc1");
                double contrast1 = exp.GetOutputParamValueByName("PeakContrast1");
                if (0.05 < Math.Abs(contrast1) && Math.Abs(contrast1) < 0.4)
                {
                    fitpeaks.Add(peak1);
                    fitcontracts.Add(contrast1);
                }
            }
            if (peakcount == 2)
            {
                double peak1 = exp.GetOutputParamValueByName("PeakLoc1");
                double contrast1 = exp.GetOutputParamValueByName("PeakContrast1");
                double peak2 = exp.GetOutputParamValueByName("PeakLoc2");
                double contrast2 = exp.GetOutputParamValueByName("PeakContrast2");
                if (0.05 < Math.Abs(contrast1) && Math.Abs(contrast1) < 0.4)
                {
                    fitpeaks.Add(peak1);
                    fitcontracts.Add(contrast1);
                }
                if (0.05 < Math.Abs(contrast2) && Math.Abs(contrast2) < 0.4)
                {
                    fitpeaks.Add(peak2);
                    fitcontracts.Add(contrast2);
                }
            }

            Frequences.AddRange(exp.Get1DChartDataSource("频率", "CW对比度数据"));
            Contracts.AddRange(exp.Get1DChartDataSource("对比度", "CW对比度数据"));
            return;
        }

        /// <summary>
        /// 全谱扫两个共振峰，扫不全则返回False
        /// </summary>
        /// <param name="peaks"></param>
        /// <param name="freqs1"></param>
        /// <param name="contracts1"></param>
        /// <param name="freqs2"></param>
        /// <param name="contracts2"></param>
        /// <exception cref="Exception"></exception>
        public static bool TotalCWPeaks2(ODMRExpObject parentexp, int CWExpIndex, out List<double> peaks, out List<double> fitcontrasts, out List<double> freqs, out List<double> contracts)
        {
            TotalCW(parentexp, CWExpIndex, out peaks, out fitcontrasts, out freqs, out contracts);
            if (peaks.Count == 0)
            {
                return false;
            }
            if (peaks.Count == 1)
            {
                peaks = new List<double>() { peaks[0], peaks[0] };
                return true;
            }
            return true;
        }

        // 扫描出CW谱的准确峰位置
        // 先粗扫后细扫(步长先设置为2MHz,确定峰位置之后按步长0.2MHz扫描)
        // 范围限制,扫描范围在2400Mhz到3400Mhz之间
        public static void ScanCW(ODMRExpObject parentexp, int CWExpIndex, out List<double> peaks, out List<double> fitcontrasts, out List<double> freqvalues, out List<double> contractvalues, double peakapprox, int EndPointCount, double scanWidth = 30, bool isReverse = false)
        {
            if (peakapprox < 2100 || peakapprox > 3500)
            {
                peaks = new List<double>();
                freqvalues = new List<double>();
                contractvalues = new List<double>();
                fitcontrasts = new List<double>();
                return;
            }
            peaks = new List<double>();
            fitcontrasts = new List<double>();
            CW(parentexp, CWExpIndex, out freqvalues, out contractvalues, out List<double> freqs, out List<double> fitcontracts, startFreq: peakapprox - scanWidth, endFreq: peakapprox + scanWidth, step: 2, averagetime: 500, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);

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
                    CW(parentexp, CWExpIndex, out freqvalues, out contractvalues, out freqs, out fitcontracts, startFreq: peakapprox - scanWidth - 20, endFreq: peakapprox - scanWidth + 20, step: 2, averagetime: 500, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                    if (freqs.Count != 0)
                    {
                        //细扫
                        CW(parentexp, CWExpIndex, out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, startFreq: freqs[0] - 5, endFreq: freqs[0] + 5, step: 1, averagetime: 2000, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                        if (f1s.Count != 0)
                        {
                            peaks.Add(f1s[0]);
                            fitcontrasts.Add(fitcontracts[0]);
                        }
                    }
                }
                if (Math.Abs(f1 - peakapprox - scanWidth) < 3)
                {
                    CW(parentexp, CWExpIndex, out freqvalues, out contractvalues, out freqs, out fitcontracts, startFreq: peakapprox + scanWidth - 20, endFreq: peakapprox + scanWidth + 20, step: 2, averagetime: 500, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                    if (freqs.Count != 0)
                    {
                        CW(parentexp, CWExpIndex, out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, startFreq: freqs[0] - 5, endFreq: freqs[0] + 5, step: 1, averagetime: 2000, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                        if (f1s.Count != 0)
                        {
                            peaks.Add(f1s[0]);
                            fitcontrasts.Add(fitcontracts[0]);
                        }
                    }
                }
                if (Math.Abs(f1 - peakapprox + scanWidth) >= 3 && Math.Abs(f1 - peakapprox - scanWidth) >= 3)
                {
                    CW(parentexp, CWExpIndex, out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, startFreq: f1 - 5, endFreq: f1 + 5, step: 1, averagetime: 2000, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                    if (f1s.Count != 0)
                    {
                        peaks.Add(f1s[0]);
                        fitcontrasts.Add(fitcontracts[0]);
                    }
                }
            }
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

        public static List<double> GetTargetOffset(double targetAngle, double offsetx, double offsety, bool reverseX, bool reverseY, bool reverseA, double angleStart)
        {

            // 计算随体坐标系下的极坐标
            double r = Math.Sqrt(offsetx * offsetx + offsety * offsety);
            double the = Math.Atan2(offsety, offsetx) * 180 / Math.PI;

            double the1 = the + GetReverseNum(reverseA) * (targetAngle - angleStart);

            double dx = GetReverseNum(reverseX) * r * (Math.Cos(the / 180 * Math.PI) - Math.Cos(the1 / 180 * Math.PI));
            double dy = GetReverseNum(reverseY) * r * (Math.Sin(the / 180 * Math.PI) - Math.Sin(the1 / 180 * Math.PI));

            return new List<double>() { dx, dy };
        }


        /// <summary>
        /// 扫两频点的CW谱
        /// </summary>
        public static void ScanCW2(ODMRExpObject parentexp, int CWExpIndex, out double peakout1, out double peakout2, out double contract1, out double contract2, out List<double> freqsout, out List<double> contrastout, double peakapprox1, double peakapprox2, int EndPointCount, double scanWidth = 30)
        {
            //寻找峰值较小值，正向扫描
            double p1 = Math.Min(peakapprox1, peakapprox2);
            double p2 = Math.Max(peakapprox1, peakapprox2);

            ScanCW(parentexp, CWExpIndex, out List<double> peak1, out List<double> fcs1, out List<double> freqsout1, out List<double> contrastout1, p1, 10, scanWidth, isReverse: false);
            ScanCW(parentexp, CWExpIndex, out List<double> peak2, out List<double> fcs2, out List<double> freqsout2, out List<double> contrastout2, p2, 10, scanWidth, isReverse: true);

            if (peak1.Count == 0 || peak2.Count == 0)
            {
                peakout1 = 0;
                peakout2 = 0;
                contract1 = 0;
                contract2 = 0;
                freqsout = new List<double>();
                contrastout = new List<double>();
                return;
            }

            ResortCWData(freqsout1, freqsout2, contrastout1, contrastout2, out freqsout, out contrastout);

            peakout1 = peak1.Min();
            contract1 = fcs1[peak1.FindIndex(x => x == peak1.Min())];
            peakout2 = peak2.Max();
            contract2 = fcs2[peak2.FindIndex(x => x == peak2.Max())];

            if (Math.Abs(peakout1 - peakout2) < 0.5)
            {
                peakout1 = 0;
                peakout2 = 0;
                contract1 = 0;
                contract2 = 0;
                freqsout = new List<double>();
                contrastout = new List<double>();
                return;
            }
        }

        private static int GetNearestDataIndex(List<double> dataarray, double targetvalue)
        {
            List<double> cal = new List<double>();
            for (int i = 0; i < dataarray.Count; i++)
            {
                cal.Add(Math.Abs(dataarray[i] - targetvalue));
            }
            return cal.IndexOf(cal.Min());
        }

        /// <summary>
        /// 重新排列CW数据
        /// </summary>
        private static void ResortCWData(List<double> Freq1, List<double> Freq2, List<double> Contract1, List<double> Contract2, out List<double> MergedFreq, out List<double> MergedContrast)
        {
            Freq1.AddRange(Freq2);
            Contract1.AddRange(Contract2);
            SortedList<int, double> l = new SortedList<int, double>();
            for (int i = 0; i < Freq1.Count; i++)
            {
                l.Add(i, Contract1[i]);
            }
            MergedFreq = l.Keys.Select(x => Freq1[x]).ToList();
            MergedContrast = l.Values.ToList();
        }

        private static int GetReverseNum(bool value)
        {
            return value ? -1 : 1;
        }
        /// <summary>
        /// 计算目标位置,无法计算则报错(角度单位:度)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CalculatepPredictLoc(MagnetLocParams Ps, double zloc, double theta, double phi, out double x, out double y, out double z, out double angle)
        {
            try
            {
                double zdis = GetReverseNum(Ps.ZReverse) * (zloc - Ps.ZLoc) + Ps.ZDistance;

                Magnet m = new Magnet(Ps.MRadius, Ps.MLength, 1);

                List<double> res = m.FindDire(theta, phi, zdis);
                double ang = res[0];
                double dx = -res[1];
                double dy = -res[2];
                double B = res[3];
                double dz = zdis - Ps.ZDistance;
                dx *= GetReverseNum(Ps.XReverse);
                dy *= GetReverseNum(Ps.YReverse);
                dz *= GetReverseNum(Ps.ZReverse);
                ang *= GetReverseNum(Ps.AReverse);
                double ang1 = Ps.AngleStart + ang;

                List<double> doffs = GetTargetOffset(Ps, ang1);
                double doffx = doffs[0];
                double doffy = doffs[1];

                if (ang1 > 150)
                    ang1 -= 360;
                if (ang1 < -150)
                    ang1 += 360;

                //角度超量程,取等效位置
                if (Math.Abs(ang1) > 150)
                {
                    res = m.FindDire(180 - theta, phi + 180, zdis);
                    ang = res[0];
                    dx = -res[1];
                    dy = -res[2];
                    B = res[3];
                    dz = zdis - Ps.ZDistance;
                    dx *= GetReverseNum(Ps.XReverse);
                    dy *= GetReverseNum(Ps.YReverse);
                    dz *= GetReverseNum(Ps.ZReverse);
                    ang *= GetReverseNum(Ps.AReverse);
                    ang1 = Ps.AngleStart + ang;

                    if (ang1 > 150)
                        ang1 -= 360;
                    if (ang1 < -150)
                        ang1 += 360;

                    //根据需要移动的角度进行偏心修正
                    doffs = GetTargetOffset(Ps, ang1);
                    doffx = doffs[0];
                    doffy = doffs[1];
                }

                x = Ps.XLoc + dx + doffx;
                y = Ps.YLoc + dy + doffy;
                z = zloc;
                angle = ang1;
            }
            catch (Exception ex)
            {
                x = double.NaN;
                y = double.NaN;
                z = double.NaN;
                angle = double.NaN;
                return;
            }
        }
    }
}
