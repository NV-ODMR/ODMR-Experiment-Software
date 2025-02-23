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
        public string XAxisName { get; set; } = "";

        public string GroupName { get; set; } = "";

        public NumricDataSeries FittedData { get; set; } = new NumricDataSeries("", new List<double>(), new List<double>()) { LineColor = ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF1F1F1F")) };

        public NormalRealFunction FitFunction { get; set; } = null;

        public FittedData1D(string xName, string groupName, NormalRealFunction fitFunction, NumricDataSeries fittedData = null)
        {
            XAxisName = xName;
            GroupName = groupName;
            Expression = FitFunction.Expression;
            FitFunction = fitFunction;
            if (fittedData != null)
                FittedData = fittedData;
        }

        public FittedData1D(string expression, string var, List<string> Params, List<double> ParamValues, string xName, string groupName, NumricDataSeries fittedData = null)
        {
            XAxisName = xName;
            GroupName = groupName;
            Expression = expression;
            FitFunction = new NormalRealFunction(expression);
            FitFunction.AddVariable(var, new RealDomain(0, 1, 1));
            for (int i = 0; i < Params.Count; i++)
            {
                FitFunction.AddParam(new KeyValuePair<string, double>(Params[i], ParamValues[i]));
            }
            FitFunction.Process();
            if (fittedData != null)
                FittedData = fittedData;
        }

        public string Expression { get; set; } = "";

        public bool IsDisplay { get; set; } = false;

        public double LoRange { get; private set; } = 0;

        public double HiRange { get; private set; } = 1;

        public int SampleCount { get; private set; } = 200;

        public override string ToString()
        {
            return FittedData.Name + "表达式:" + FitFunction.Expression;
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
