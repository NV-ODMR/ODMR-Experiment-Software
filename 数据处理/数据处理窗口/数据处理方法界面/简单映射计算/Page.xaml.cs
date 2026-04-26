using Controls.Charts;
using Controls.Windows;
using MathLib.NormalMath.Decimal.Function;
using ODMR_Lab.基本控件;
using PythonHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace ODMR_Lab.数据处理.数据处理窗口.数据处理方法界面.简单映射计算
{
    /// <summary>
    /// Page.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessPage : ProcesspageBase
    {
        public override string ProcessName { get; set; } = "简单映射计算";

        public ProcessPage()
        {
            InitializeComponent();
        }

        public override ChartDataBase CalculateMethod()
        {
            #region 计算参数格式验证
            string expression = "";
            App.Current.Dispatcher.Invoke(() =>
            {
                expression = SimpleMappingBox.Text;
            });

            if (expression == "")
            {
                throw new Exception("计算表达式不能为空:");
            }
            #endregion

            #region 一维映射数据
            if (ProcessHelper.IsD1CalCulate(ParentDataSource, expression, out int count, out List<ChartData1D> ds, out List<string> dvs))
            {
                try
                {
                    NumricChartData1D data = null;
                    data = CalculateSimple1DMapping(expression, "", "计算结果", count, ChartDataType.XY, ds, dvs);
                    return data;
                }
                catch (Exception ex)
                {
                    throw new Exception("计算遇到问题:" + ex.Message);
                }
            }
            #endregion

            #region 二维映射数据
            if (ProcessHelper.IsD2CalCulate(ParentDataSource, expression, out int xcount, out int ycount, out double xlo, out double xhi, out double ylo, out double yhi, out List<ChartData2D> d2s, out List<string> d2vs))
            {
                try
                {
                    ChartData2D data = null;
                    data = CalculateSimple2DMapping(expression, "", "计算结果X", "计算结果Y", "计算结果值", xcount, ycount, xlo, xhi, ylo, yhi, d2s, d2vs);
                    return data;
                }
                catch (Exception ex)
                {
                    throw new Exception("计算遇到问题:" + ex.Message);
                }
            }
            #endregion

            throw new Exception("待计算数据格式不正确,请检查计算表达式");
        }

        /// <summary>
        /// 计算一维简单映射
        /// </summary>
        /// <param name="targetData"></param>
        /// <exception cref="Exception"></exception>
        private NumricChartData1D CalculateSimple1DMapping(string expression, string resultGroupname, string resultName, int count, ChartDataType type, List<ChartData1D> sourceData, List<string> sourceDataVs)
        {
            NumricChartData1D targetData = new NumricChartData1D(resultName, resultGroupname, type);
            NormalRealFunction func = null;

            Dispatcher.Invoke(() =>
            {
                func = new NormalRealFunction(expression) { };
                foreach (var item in sourceDataVs)
                {
                    func.AddVariable(item, new RealDomain(0, 1, 10));
                }
            });

            //函数预处理
            func.Process();

            if (sourceData.Count == 0) throw new Exception("待计算数据为空");

            for (int i = 0; i < count; i++)
            {
                List<double> datatocal = new List<double>();
                foreach (var item in sourceData)
                {
                    if (i > item.GetCount() - 1)
                    {
                        datatocal.Add(double.NaN);
                        continue;
                    }
                    if (item is TimeChartData1D)
                    {
                        datatocal.Add((item as TimeChartData1D).Data[i].ToOADate());
                    }
                    else
                    {
                        datatocal.Add((item as NumricChartData1D).Data[i]);
                    }
                }
                targetData.Data.Add(func.GetValueAt(datatocal).Content.Last());
            }
            return targetData;
        }


        /// <summary>
        /// 计算二维简单映射
        /// </summary>
        /// <param name="targetData"></param>
        /// <exception cref="Exception"></exception>
        private ChartData2D CalculateSimple2DMapping(string expression, string resultGroupname, string xName, string yName, string zName, int resultxCount, int resultyCount, double xlo, double xhi, double ylo, double yhi, List<ChartData2D> sourceData, List<string> sourceDataVs)
        {
            ChartData2D targetData = new ChartData2D(new FormattedDataSeries2D(resultxCount, xlo, xhi, resultyCount, ylo, yhi) { XName = xName, YName = yName, ZName = zName }) { GroupName = resultGroupname };
            NormalRealFunction func = null;

            Dispatcher.Invoke(() =>
            {
                func = new NormalRealFunction(expression) { };
                foreach (var item in sourceDataVs)
                {
                    func.AddVariable(item, new RealDomain(0, 1, 10));
                }
            });

            //函数预处理
            func.Process();

            if (sourceData.Count == 0) throw new Exception("待计算数据为空");

            for (int i = 0; i < resultxCount; i++)
            {
                for (int j = 0; j < resultyCount; j++)
                {
                    List<double> datatocal = new List<double>();
                    foreach (var item in sourceData)
                    {
                        datatocal.Add(item.Data.GetValue(i, j));
                    }
                    targetData.Data.SetValue(i, j, func.GetValueAt(datatocal).Content.Last());
                }
            }
            return targetData;
        }

        public override void UpdateMethod()
        {
            ProcessHelper.UpdateVariables(SimpleMappingBox, ParentDataSource);
        }
    }
}
