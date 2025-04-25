using Controls.Charts;
using MathLib.NormalMath.Decimal;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {
        private double SinFunc(double x, double[] ps)
        {
            double a = ps[0];
            double b = ps[1];
            double c = ps[2];
            return a * Math.Cos((x - b) * Math.PI / 180) + c;
        }

        /// <summary>
        /// 角度扫描
        /// </summary>
        private void ScanAngle(double progresslo, double progresshi)
        {
            Scan1DSession<NanoStageInfo> session = new Scan1DSession<NanoStageInfo>();
            session.ProgressBarMethod = new Action<NanoStageInfo, double>((dev, v) =>
            {
                SetProgress(v);
            });
            session.FirstScanEvent = ScanAngleEvent;
            session.ScanEvent = ScanAngleEvent;
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            session.ScanSource = GetDeviceByName("MagnetAngle") as NanoStageInfo;

            session.BeginScan(new D1NumricLinearScanRange(-140, 140, 15), progresslo, progresshi, this, 0.0, 0.0);

            #region 进行拟合得到方位角
            List<double> locs = Get1DChartDataSource("位置", "角度方向磁场信息");
            List<double> bs = Get1DChartDataSource("总磁场(G)", "角度方向磁场信息");
            List<double> sindata = Get1DChartDataSource("沿轴磁场(G)", "角度方向磁场信息");
            UpdatePlotChartFlow(true);
            double reverse = GetReverseNum(GetInputParamValueByName("ReverseA"));
            double anglestart = GetInputParamValueByName("AngleStart");
            var newloc = locs.Select(x => reverse * (x - anglestart)).ToList();
            ConvertAbsDataToSin(newloc, sindata, out var xx, out var yy);

            double[] result = CurveFitting.FitCurveWithFunc(xx, yy, new List<double>() { (yy.Max() - yy.Min()) / 2, 180, yy.Average() }, new List<double>() { 180, 180, 180 }, SinFunc, AlgorithmType.LevenbergMarquardt, 5000);

            //添加拟合曲线
            var xs = new D1NumricLinearScanRange(locs.Min(), locs.Max(), 500).ScanPoints;
            var ys = xs.Select(x => Math.Abs(result[0] * Math.Cos((reverse * (x - anglestart) - result[1]) * Math.PI / 180) + result[2])).ToList();

            D1FitDatas.Add(new FittedData1D("abs(a*cos((x-b)*pi/180)+c)", "x", new List<string>() { "a", "b", "c" }, result.ToList(), "位置", "角度方向磁场信息",
                new NumricDataSeries("拟合数据", xs, ys) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);

            double amplitude = result[0];
            double phase = result[1];
            double B = bs.Average();
            //拟合参数A
            double sint = amplitude / B;
            if (sint > 1) sint = 1;
            if (sint < -1) sint = -1;
            double t1 = Math.Abs(Math.Asin(sint) * 180 / Math.PI);
            OutputParams.Add(new Param<double>("θ值1", t1, "Theta1"));
            OutputParams.Add(new Param<double>("θ值2", 180 - t1, "Theta2"));
            double phi2 = phase + 180;
            while (phi2 > 360)
            {
                phi2 -= 360;
            }
            while (phi2 < -360)
            {
                phi2 += 360;
            }
            OutputParams.Add(new Param<double>("φ值1", phase, "Phi1"));
            OutputParams.Add(new Param<double>("φ值2", phi2, "Phi2"));
            #endregion
        }
    }
}
