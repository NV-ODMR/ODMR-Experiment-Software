using ODMR_Lab.IO操作;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.基本控件
{
    public class Chart1DStyleParams : ParamBase
    {
        public Param<double> LineWidth { get; set; } = new Param<double>("线宽", 1, "LineWidth");

        public Param<bool> IsSmooth { get; set; } = new Param<bool>("平滑曲线", false, "IsSmooth");

        public Param<double> PointSize { get; set; } = new Param<double>("点尺寸", 10, "PointSize");

        /// <summary>
        /// 此属性必须放在所有属性最后
        /// </summary>
        public Param<string> PlotStyle { get; set; } = new Param<string>("曲线类型", "线状图", "PlotStyle");

        /// <summary>
        /// 全局图表参数
        /// </summary>
        public static Chart1DStyleParams GlobalChartPlotParam { get; set; } = new Chart1DStyleParams();
    }
}
