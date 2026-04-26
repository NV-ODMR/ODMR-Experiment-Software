using Controls.Algebra;
using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ODMR_Lab.数据处理.数据处理窗口.数据处理方法界面
{
    public class ProcessHelper
    {

        /// <summary>
        /// 是否是二维计算表达式并且是否合法
        /// </summary>
        /// <returns></returns>
        public static bool IsD2CalCulate(DataVisualSource parentSource, string expression, out int xcount, out int ycount, out double xlo, out double xhi, out double ylo, out double yhi, out List<ChartData2D> datas, out List<string> datavs)
        {
            xcount = -1;
            ycount = -1;
            xlo = 0;
            ylo = 0;
            xhi = 0;
            yhi = 0;
            datas = new List<ChartData2D>();
            datavs = new List<string>();
            Regex reg1 = new Regex("[O][0-9]+");
            Regex reg2 = new Regex("[T][0-9]+");
            MatchCollection coll1 = reg1.Matches(expression);
            MatchCollection coll2 = reg2.Matches(expression);
            if (coll1.Count != 0) return false;
            //查找变量
            foreach (Match m in coll2)
            {
                ChartData2D dd = parentSource.ChartDataSource2D[int.Parse(m.Value.Replace("T", ""))];
                if (!datavs.Contains(m.Value))
                {
                    datas.Add(dd);
                    datavs.Add(m.Value);
                }
            }
            HashSet<int> xs = new HashSet<int>(datas.Select(x => x.Data.XCounts));
            HashSet<int> ys = new HashSet<int>(datas.Select(x => x.Data.YCounts));
            HashSet<double> xlos = new HashSet<double>(datas.Select(x => x.Data.XLo));
            HashSet<double> ylos = new HashSet<double>(datas.Select(x => x.Data.YLo));
            HashSet<double> xhis = new HashSet<double>(datas.Select(x => x.Data.XHi));
            HashSet<double> yhis = new HashSet<double>(datas.Select(x => x.Data.YHi));
            if (xs.Count != 1 || ys.Count != 1 || xlos.Count != 1 || ylos.Count != 1 || xhis.Count != 1 || yhis.Count != 1) throw new Exception("数据总点数不同，无法进行映射计算");
            xcount = xs.ElementAt(0);
            ycount = ys.ElementAt(0);
            xlo = xlos.ElementAt(0);
            ylo = ylos.ElementAt(0);
            xhi = xhis.ElementAt(0);
            yhi = yhis.ElementAt(0);
            return true;
        }

        /// <summary>
        /// 是否是一维计算表达式
        /// </summary>
        /// <returns></returns>
        public static bool IsD1CalCulate(DataVisualSource parentSource, string expression, out int count, out List<ChartData1D> datas, out List<string> datavs)
        {
            datas = new List<ChartData1D>();
            datavs = new List<string>();
            Regex reg1 = new Regex("[O][0-9]+");
            Regex reg2 = new Regex("[T][0-9]+");
            count = -1;
            MatchCollection coll1 = reg1.Matches(expression);
            MatchCollection coll2 = reg2.Matches(expression);
            if (coll2.Count != 0) return false;
            //查找变量
            foreach (Match m in coll1)
            {
                ChartData1D dd = parentSource.ChartDataSource1D[int.Parse(m.Value.Replace("O", ""))];
                if (!datavs.Contains(m.Value))
                {
                    datas.Add(dd);
                    datavs.Add(m.Value);
                }
            }
            HashSet<int> xs = new HashSet<int>(datas.Select(x => x.GetCount()));
            if (xs.Count != 1) throw new Exception("数据总点数不同，无法进行映射计算");
            count = xs.ElementAt(0);
            return true;
        }

        /// <summary>
        /// 更新公式输入栏的变量
        /// </summary>
        public static void UpdateVariables(AlgebraBox box, DataVisualSource parentSource)
        {
            //添加变量
            box.Variables.Clear();
            box.Variables.AddRange(parentSource.ChartDataSource1D.Select(x => "O" + parentSource.ChartDataSource1D.IndexOf(x)));
            box.Variables.AddRange(parentSource.ChartDataSource2D.Select(x => "T" + parentSource.ChartDataSource2D.IndexOf(x)));
            box.UpdateTextDisplay();
        }
    }
}
