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
using ODMR_Lab.磁场调节;
using System.Windows;
using ODMR_Lab.实验部分.磁场调节.主窗口;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.磁场调节;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// AutoTrace定位NV
    /// </summary>
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {
        protected double GetAngleX()
        {
            return GetInputParamValueByName("AngleStart") + 90;
        }
        protected double GetAngleY()
        {
            return GetInputParamValueByName("AngleStart");
        }



        protected void AddMagnetDataToChart(double loc, double peak0, double peak1, string GroupName, out double Bp, out double Bv, out double B)
        {
            CalculateB(peak0, peak1, out Bp, out Bv, out B);
            Get1DChartDataSource("位置", GroupName).Add(loc);
            Get1DChartDataSource("CW谱位置1", GroupName).Add(peak0);
            Get1DChartDataSource("CW谱位置2", GroupName).Add(peak1);
            Get1DChartDataSource("沿轴磁场(G)", GroupName).Add(Bp);
            Get1DChartDataSource("垂直轴磁场(G)", GroupName).Add(Bv);
            Get1DChartDataSource("总磁场(G)", GroupName).Add(B);
            UpdatePlotChartFlow(true);
        }

        protected void ClearMagnetDataFromChart(string GroupName)
        {
            Get1DChartDataSource("CW谱位置1", GroupName).Clear();
            Get1DChartDataSource("CW谱位置2", GroupName).Clear();
            Get1DChartDataSource("沿轴磁场(G)", GroupName).Clear();
            Get1DChartDataSource("垂直轴磁场(G)", GroupName).Clear();
            Get1DChartDataSource("总磁场(G)", GroupName).Clear();
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }


        protected void AddScanDataToChart(double loc, string LocType, List<double> Freqs, List<double> Contracts)
        {
            D1ChartDatas.Add(new NumricChartData1D(LocType + ":" + Math.Round(loc, 5).ToString() + "频率", LocType + " " + "CW谱数据", ChartDataType.X) { Data = Freqs });
            D1ChartDatas.Add(new NumricChartData1D(LocType + ":" + Math.Round(loc, 5).ToString() + "对比度", LocType + " " + "CW谱数据", ChartDataType.Y) { Data = Contracts });
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

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
        protected void CalculateB(double freq1, double freq2, out double Bp, out double Bv, out double B)
        {
            double D = GetInputParamValueByName("D");
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

        public List<double> GetTargetOffset(double targetAngle)
        {

            // 计算随体坐标系下的极坐标
            double r = Math.Sqrt(GetInputParamValueByName("OffsetX") * GetInputParamValueByName("OffsetX") + GetInputParamValueByName("OffsetY") * GetInputParamValueByName("OffsetY"));
            double the = Math.Atan2(GetInputParamValueByName("OffsetY"), GetInputParamValueByName("OffsetX")) * 180 / Math.PI;

            double the1 = the + GetReverseNum(GetInputParamValueByName("ReverseA")) * (targetAngle - GetInputParamValueByName("AngleStart"));

            double dx = GetReverseNum(GetInputParamValueByName("ReverseX")) * r * (Math.Cos(the / 180 * Math.PI) - Math.Cos(the1 / 180 * Math.PI));
            double dy = GetReverseNum(GetInputParamValueByName("ReverseY")) * r * (Math.Sin(the / 180 * Math.PI) - Math.Sin(the1 / 180 * Math.PI));

            return new List<double>() { dx, dy };
        }

        private void TotalCW(out List<double> peaks, out List<double> freqs, out List<double> contracts)
        {
            //粗扫
            CW(out freqs, out contracts, out List<double> ps, out List<double> fitcontracts, 2600, 3200, 3, 500, 2, EndPointCount: 5);

            if (ps.Count == 1)
            {
                peaks = new List<double>();
                //细扫
                CW(out List<double> freqs1, out List<double> contracts1, out List<double> peaks1, out List<double> fitcontracts1, ps[0] - 10, ps[0] + 10, 1, 2000, 1, EndPointCount: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs1, contracts, contracts1, out freqs, out contracts);
                    peaks.Add(peaks1.Min());
                }
                CW(out List<double> freqs2, out List<double> contracts2, out peaks1, out fitcontracts1, ps[0] - 10, ps[0] + 10, 1, 2000, 1, EndPointCount: 10, isReverse: true);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs2, contracts, contracts2, out freqs, out contracts);
                    peaks.Add(peaks1.Max());
                }
                return;
            }
            if (ps.Count == 2)
            {
                peaks = new List<double>();
                double minfreq = Math.Min(ps[0], ps[1]);
                double maxfreq = Math.Max(ps[0], ps[1]);
                CW(out List<double> freqs1, out List<double> contracts1, out List<double> peaks1, out List<double> fitcontracts1, minfreq - 5, minfreq + 5, 1, 2000, 1, EndPointCount: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs1, contracts, contracts1, out freqs, out contracts);
                    peaks.Add(peaks1.Min());
                }
                CW(out List<double> freqs2, out List<double> contracts2, out peaks1, out fitcontracts1, maxfreq - 5, maxfreq + 5, 1, 2000, 1, EndPointCount: 10, isReverse: false);
                if (peaks1.Count != 0)
                {
                    ResortCWData(freqs, freqs2, contracts, contracts2, out freqs, out contracts);
                    peaks.Add(peaks1.Max());
                }
                return;
            }

            peaks = new List<double>();
            freqs = new List<double>();
            contracts = new List<double>();
            return;
        }

        Random r = new Random();
        public void CW(out List<double> Frequences, out List<double> Contracts, out List<double> fitpeaks, out List<double> fitcontracts, double startFreq, double endFreq, double step, int averagetime, int peakcount, int EndPointCount = 5, bool isReverse = false)
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
            var exp = RunSubExperimentBlock(1, true, InputParams);

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
        /// 全谱扫两个共振峰，扫不全则报错
        /// </summary>
        /// <param name="peaks"></param>
        /// <param name="freqs1"></param>
        /// <param name="contracts1"></param>
        /// <param name="freqs2"></param>
        /// <param name="contracts2"></param>
        /// <exception cref="Exception"></exception>
        protected void TotalCWPeaks2OrException(out List<double> peaks, out List<double> freqs, out List<double> contracts)
        {
            TotalCW(out peaks, out freqs, out contracts);
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
        protected void ScanCW(out List<double> peaks, out List<double> freqvalues, out List<double> contractvalues, double peakapprox, int EndPointCount, double scanWidth = 30, bool isReverse = false)
        {
            if (peakapprox < 2100 || peakapprox > 3500)
            {
                peaks = new List<double>();
                freqvalues = new List<double>();
                contractvalues = new List<double>();
                return;
            }
            peaks = new List<double>();
            CW(out freqvalues, out contractvalues, out List<double> freqs, out List<double> fitcontracts, startFreq: peakapprox - scanWidth, endFreq: peakapprox + scanWidth, step: 2, averagetime: 500, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);

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
                    CW(out freqvalues, out contractvalues, out freqs, out fitcontracts, startFreq: peakapprox - scanWidth - 20, endFreq: peakapprox - scanWidth + 20, step: 2, averagetime: 500, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                    if (freqs.Count != 0)
                    {
                        //细扫
                        CW(out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, startFreq: freqs[0] - 5, endFreq: freqs[0] + 5, step: 1, averagetime: 2000, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                        if (f1s.Count != 0)
                        {
                            peaks.Add(f1s[0]);
                        }
                    }
                }
                if (Math.Abs(f1 - peakapprox - scanWidth) < 3)
                {
                    CW(out freqvalues, out contractvalues, out freqs, out fitcontracts, startFreq: peakapprox + scanWidth - 20, endFreq: peakapprox + scanWidth + 20, step: 2, averagetime: 500, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                    if (freqs.Count != 0)
                    {
                        CW(out freqvalues, out fitcontracts, out List<double> f1s, out fitcontracts, startFreq: freqs[0] - 5, endFreq: freqs[0] + 5, step: 1, averagetime: 2000, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
                        if (f1s.Count != 0)
                        {
                            peaks.Add(f1s[0]);
                        }
                    }
                }
                if (Math.Abs(f1 - peakapprox + scanWidth) >= 3 && Math.Abs(f1 - peakapprox - scanWidth) >= 3)
                {
                    CW(out freqvalues, out contractvalues, out List<double> f1s, out fitcontracts, startFreq: f1 - 5, endFreq: f1 + 5, step: 1, averagetime: 2000, peakcount: 1, EndPointCount: EndPointCount, isReverse: isReverse);
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
        protected void ScanCW2(out double peakout1, out double peakout2, out List<double> freqsout, out List<double> contrastout, double peakapprox1, double peakapprox2, int EndPointCount, double scanWidth = 30)
        {
            //寻找峰值较小值，正向扫描
            double p1 = Math.Min(peakapprox1, peakapprox2);
            double p2 = Math.Max(peakapprox1, peakapprox2);

            ScanCW(out List<double> peak1, out List<double> freqsout1, out List<double> contrastout1, p1, EndPointCount, scanWidth, isReverse: false);
            ScanCW(out List<double> peak2, out List<double> freqsout2, out List<double> contrastout2, p2, EndPointCount, scanWidth, isReverse: true);

            if (peak1.Count == 0 || peak2.Count == 0)
            {
                peakout1 = 0;
                peakout2 = 0;
                freqsout = new List<double>();
                contrastout = new List<double>();
                return;
            }

            ResortCWData(freqsout1, freqsout2, contrastout1, contrastout2, out freqsout, out contrastout);

            peakout1 = peak1.Min();
            peakout2 = peak2.Max();
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
        private void ResortCWData(List<double> Freq1, List<double> Freq2, List<double> Contract1, List<double> Contract2, out List<double> MergedFreq, out List<double> MergedContrast)
        {
            Freq1.AddRange(Freq2);
            Contract1.AddRange(Contract2);
            SortedList<double, double> l = new SortedList<double, double>();
            for (int i = 0; i < Freq1.Count; i++)
            {
                l.Add(Freq1[i], Contract1[i]);
            }
            MergedFreq = l.Keys.ToList();
            MergedContrast = l.Values.ToList();
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
        protected void CalculatePossibleLocs(out double x1, out double y1, out double z1, out double angle1, out double x2, out double y2, out double z2, out double angle2)
        {
            Magnet m = new Magnet(GetInputParamValueByName("MRadius"), GetInputParamValueByName("MLength"), 1);
            #region 第一个方向
            List<double> res = m.FindDire(GetOutputParamValueByName("Theta1"), GetOutputParamValueByName("Phi1"), GetOutputParamValueByName("ZDistance"));

            double ang = res[0] * GetReverseNum(GetInputParamValueByName("ReverseA"));
            double dx = res[1] * GetReverseNum(GetInputParamValueByName("ReverseX"));
            double dy = res[2] * GetReverseNum(GetInputParamValueByName("ReverseY"));
            ang = GetInputParamValueByName("AngleStart") + ang;
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
                res = m.FindDire(Math.PI - GetOutputParamValueByName("Theta1"), GetOutputParamValueByName("Phi1") - Math.PI, GetOutputParamValueByName("ZDistance"));
                ang = res[0] * GetReverseNum(GetInputParamValueByName("ReverseA"));
                dx = res[1] * GetReverseNum(GetInputParamValueByName("ReverseX"));
                dy = res[2] * GetReverseNum(GetInputParamValueByName("ReverseY"));
                ang = GetInputParamValueByName("AngleStart") + ang;
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
            List<double> re = GetTargetOffset(ang);
            x1 = GetOutputParamValueByName("XLoc") + re[0] + dx;
            y1 = GetOutputParamValueByName("YLoc") + re[1] + dy;
            z1 = GetOutputParamValueByName("ZLoc");
            angle1 = ang;

            #endregion

            #region 第二个方向
            res = m.FindDire(GetOutputParamValueByName("Theta2"), GetOutputParamValueByName("Phi1"), GetOutputParamValueByName("ZDistance"));
            ang = res[0] * GetReverseNum(GetInputParamValueByName("ReverseA"));
            dx = res[1] * GetReverseNum(GetInputParamValueByName("ReverseX"));
            dy = res[2] * GetReverseNum(GetInputParamValueByName("ReverseY"));
            ang = GetInputParamValueByName("AngleStart") + ang;
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
                res = m.FindDire(Math.PI - GetOutputParamValueByName("Theta2"), GetOutputParamValueByName("Phi1") - Math.PI, GetOutputParamValueByName("ZDistance"));
                ang = res[0] * GetReverseNum(GetInputParamValueByName("ReverseA"));
                dx = res[1] * GetReverseNum(GetInputParamValueByName("ReverseX"));
                dy = res[2] * GetReverseNum(GetInputParamValueByName("ReverseY"));
                ang = GetInputParamValueByName("AngleStart") + ang;
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
            re = GetTargetOffset(ang);
            x2 = GetOutputParamValueByName("XLoc") + re[0] + dx;
            y2 = GetOutputParamValueByName("YLoc") + re[1] + dy;
            z2 = GetOutputParamValueByName("ZLoc");
            angle2 = ang;

            #endregion
        }

        private int GetReverseNum(bool value)
        {
            return value ? -1 : 1;
        }
        /// <summary>
        /// 将绝对值数据转换成三角函数
        /// </summary>
        private void ConvertAbsDataToSin(List<double> x, List<double> y, out List<double> xx, out List<double> yy)
        {
            yy = new List<double>();
            xx = new List<double>();
            if (y.Count < 3)
            {
                xx = x;
                yy = y;
            }

            List<int> revs = new List<int>();

            for (int i = 1; i < y.Count - 1; i++)
            {
                //找拐点
                if (y[i] <= y[i + 1] && y[i] <= y[i - 1])
                {
                    revs.Add(i);
                }
            }

            revs.Sort();

            for (int i = 0; i < y.Count; i++)
            {
                int sgn = 1;
                foreach (var item in revs)
                {
                    if (i > item) sgn *= -1;
                }
                if (revs.Where(v => v == i).Count() == 0)
                {
                    yy.Add(sgn * y[i]);
                    xx.Add(x[i]);
                }
            }
        }

        /// <summary>
        /// 计算预测磁场
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CalculatePredictField(PredictData PData)
        {
            try
            {
                double the = PData.ThetaPredictInput;
                double phi = PData.PhiPredictInput;
                double currentzloc = PData.ZPredictHeightInput;


                double zdis = GetReverseNum(GetInputParamValueByName("ReverseZ")) * (currentzloc - PData.ZLoc) + PData.ZDistance;

                Magnet m = new Magnet(GetInputParamValueByName("MRadius"), GetInputParamValueByName("MLength"), 1);

                List<double> res = m.FindDire(the, phi, currentzloc);
                double ang = res[0];
                double dx = res[1];
                double dy = res[2];
                double B = res[3];
                double dz = zdis - PData.ZDistance;
                dx *= GetReverseNum(GetInputParamValueByName("ReverseX"));
                dy *= GetReverseNum(GetInputParamValueByName("ReverseY"));
                dz *= GetReverseNum(GetInputParamValueByName("ReverseZ"));
                ang *= GetReverseNum(GetInputParamValueByName("ReverseA"));
                double ang1 = GetInputParamValueByName("AngleStart") + ang;

                List<double> doffs = GetTargetOffset(ang1);
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
                    dz = zdis - PData.ZDistance;
                    dx *= GetReverseNum(GetInputParamValueByName("ReverseX"));
                    dy *= GetReverseNum(GetInputParamValueByName("ReverseY"));
                    dz *= GetReverseNum(GetInputParamValueByName("ReverseZ"));
                    ang *= GetReverseNum(GetInputParamValueByName("ReverseA"));
                    ang1 = GetInputParamValueByName("AngleStart") + ang;

                    if (ang1 > 150)
                        ang1 -= 360;
                    if (ang1 < -150)
                        ang1 += 360;

                    //根据需要移动的角度进行偏心修正
                    doffs = GetTargetOffset(ang1);
                    doffx = doffs[0];
                    doffy = doffs[1];
                }

                PData.XLocPredictOutPut = PData.XLoc + dx + doffx;
                PData.YLocPredictOutPut = PData.YLoc + dy + doffy;
                PData.ZLocPredictOutPut = currentzloc;
                PData.ALocPredictOutPut = ang1;

                try
                {
                    //如果可能的话计算预测的磁场强度 
                    double Intensity = GetInputParamValueByName("MIntensity");
                    PData.PredictB = Intensity * B * 10000;
                }
                catch (Exception) { }
            }
            catch (Exception ex) { return; }
        }
    }
}
