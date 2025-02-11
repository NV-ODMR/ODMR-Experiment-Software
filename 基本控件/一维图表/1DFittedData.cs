using CodeHelper;
using Controls.Charts;
using MathLib.NormalMath.Decimal.Function;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ODMR_Lab.基本控件.一维图表
{
    public class FittedData1D
    {
        public ChartData1D XData { get; set; } = null;

        public ChartData1D YData { get; set; } = null;

        public NumricDataSeries FittedData { get; set; } = new NumricDataSeries("", new List<double>(), new List<double>()) { LineColor = ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF1F1F1F")) };

        public NormalRealFunction FitFunction { get; set; } = null;

        public string FitName { get; set; } = "";

        public FittedData1D(string expression, ChartData1D xData, ChartData1D yData, NormalRealFunction fitFunction, string fitName)
        {
            Expression = expression;
            XData = xData;
            YData = yData;
            FitFunction = fitFunction;
            FitName = fitName;
        }

        public string Expression { get; set; } = "";

        public bool IsDisplay { get; set; } = false;

        public double LoRange { get; private set; } = 0;

        public double HiRange { get; private set; } = 1;

        public int SampleCount { get; private set; } = 200;

        public override string ToString()
        {
            string xname = "";
            if (XData != null) xname = XData.Name;
            string yname = "";
            if (YData != null) yname = YData.Name;
            string fit = "";
            if (FitFunction != null) fit = FitFunction.Expression;
            return "X:" + xname + "Y:" + yname + "Fit:" + fit;
        }

        public void UpdatePoint(double lo, double hi, int count)
        {
            FitFunction.ChangeScanArea(0, new RealDomain(lo, hi, count));
            LoRange = lo;
            HiRange = hi;
            SampleCount = count;
            var xs = Enumerable.Range(0, count).Select(x => lo + x * (hi - lo) / (count - 1));
            var res = xs.Select(x => FitFunction.GetRawValueAt(new List<double>() { x }).Content.Last());
            FittedData.X = xs.ToList();
            FittedData.Y = res.ToList();
        }
    }
}
