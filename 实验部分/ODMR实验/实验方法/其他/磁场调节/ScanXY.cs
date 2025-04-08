using Controls.Charts;
using MathLib.NormalMath.Decimal;
using MathNet.Numerics;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;

using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {
        /// <summary>
        /// X方向扫描
        /// </summary>
        private void ScanX(double progresslo, double progresshi)
        {
            double loc = LineScanCore("X", progresslo, progresshi);
            // 读取磁铁角度，计算偏移量
            double angle = GetAngleX();
            List<double> xy = GetTargetOffset(angle);
            OutputParams.Add(new Param<double>("X方向磁场最大位置", loc - xy[0], "XLoc"));
        }

        /// <summary>
        /// Y方向扫描
        /// </summary>
        private void ScanY(double progresslo, double progresshi)
        {
            OutputParams.Add(new Param<double>("Y方向磁场最大位置", LineScanCore("Y", progresslo, progresshi), "YLoc"));
        }

        /// <summary>
        /// 用二次函数拟合
        /// </summary>
        private double Pow2(double x, double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            return a * (x - b) * (x - b) + c;
        }
        private double LineScanCore(string ScanDir, double progresslo, double progresshi)
        {
            int ind = 0;
            Scan1DSession<NanoStageInfo> session = new Scan1DSession<NanoStageInfo>();
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.ProgressBarMethod = new Action<NanoStageInfo, double>((dev, v) =>
            {
                SetProgress(v);
            });
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;

            double restrictlo = 0;
            double restricthi = 0;

            if (ScanDir == "X")
            {
                session.ScanSource = GetDeviceByName("MagnetX") as NanoStageInfo;
                restrictlo = GetInputParamValueByName("XScanLo");
                restricthi = GetInputParamValueByName("XScanHi");
            }
            if (ScanDir == "Y")
            {
                session.ScanSource = GetDeviceByName("MagnetY") as NanoStageInfo;
                restrictlo = GetInputParamValueByName("YScanLo");
                restricthi = GetInputParamValueByName("YScanHi");
            }

            //根据点数设置位移台（二分法,扫描点数始终为6)
            //遍历一遍范围，之后拟合得到最大值位置，之后范围减半
            //重复上一步骤，直到步长小于0.05mm
            //计算总点数
            double scanrange = Math.Abs(restricthi - restrictlo);
            double scancount = 6;
            double countApprox = 0;
            double step = scanrange / (scancount - 1);
            while (step >= 0.5)
            {
                countApprox += scancount;
                step /= 2;
            }

            double scanmin = restrictlo;
            double scanmax = restricthi;
            double cw1 = 0, cw2 = 0;
            double peak = 0;

            List<double> xdata = new List<double>();
            List<double> ydata = new List<double>();
            List<double> cw1s = new List<double>();
            List<double> cw2s = new List<double>();
            double[] param = new double[3];

            step = (scanrange) / (scancount - 1);
            while (step >= 0.5)
            {
                session.BeginScan(new D1NumricLinearScanRange(scanmin, scanmax, 6),
                    ind * (progresshi - progresslo) / countApprox + progresslo,
                    (ind + 5) * (progresshi - progresslo) / countApprox + progresslo, this, cw1, cw2);
                ind += 6;

                #region 根据二次函数计算当前峰值

                if (ScanDir == "X")
                {
                    xdata = Get1DChartDataSource("位置", "X方向磁场信息");
                    ydata = Get1DChartDataSource("总磁场(G)", "X方向磁场信息");
                    cw1s = Get1DChartDataSource("CW谱位置1", "X方向磁场信息");
                    cw2s = Get1DChartDataSource("CW谱位置2", "X方向磁场信息");
                }
                if (ScanDir == "Y")
                {
                    xdata = Get1DChartDataSource("位置", "Y方向磁场信息");
                    ydata = Get1DChartDataSource("总磁场(G)", "Y方向磁场信息");
                    cw1s = Get1DChartDataSource("CW谱位置1", "Y方向磁场信息");
                    cw2s = Get1DChartDataSource("CW谱位置2", "Y方向磁场信息");
                }

                param = CurveFitting.FitCurveWithFunc(xdata, ydata,
                   new List<double>() { (ydata.Min() - ydata.Max()) / Math.Pow(Math.Abs(restricthi - restrictlo) / 2, 2), xdata[ydata.IndexOf(ydata.Max())], ydata.Max() },
                   new List<double>() { 10, 10, 10 }, Pow2, AlgorithmType.LevenbergMarquardt, 5000);
                //计算峰值
                peak = param[1];
                //步长减半，范围减半
                step /= 2;
                scanrange = Math.Abs(scanmax - scanmin);
                scanmin = peak - scanrange / 4;
                scanmax = peak + scanrange / 4;

                int peakind = GetNearestDataIndex(xdata, scanmin);
                cw1 = cw1s[peakind];
                cw2 = cw2s[peakind];
                #endregion
            }

            var xs = new D1NumricLinearScanRange(xdata.Min(), xdata.Max(), 500).ScanPoints;
            var ys = xs.Select(x => Pow2(x, param)).ToList();
            if (ScanDir == "X")
            {
                D1FitDatas.Add(new FittedData1D("a*(x-b)*(x-b)+c", "x", new List<string>() { "a", "b", "c" }, param.ToList(), "位置", "X方向磁场信息",
   new NumricDataSeries("拟合数据", xs, ys) { LineColor = Colors.LightSkyBlue }));
            }
            if (ScanDir == "Y")
            {
                D1FitDatas.Add(new FittedData1D("a*(x-b)*(x-b)+c", "x", new List<string>() { "a", "b", "c" }, param.ToList(), "位置", "Y方向磁场信息",
   new NumricDataSeries("拟合数据", xs, ys) { LineColor = Colors.LightSkyBlue }));
            }
            UpdatePlotChart();
            UpdatePlotChartFlow(true);

            return peak;
        }
    }
}
