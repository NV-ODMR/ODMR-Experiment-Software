using ODMR_Lab.IO操作;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controls.Charts;

namespace ODMR_Lab.基本控件
{
    public class Chart2DStyleParams : ParamBase
    {
        public Param<ColorMaps> MapColorStyle { get; set; } = new Param<ColorMaps>("颜色类型", ColorMaps.Topo, "MapColorStyle");

        public Param<bool> AutoScale { get; set; } = new Param<bool>("自适应值范围", true, "AutoScale");

        public Param<bool> Smooth { get; set; } = new Param<bool>("平滑", false, "Smooth");

        public Param<double> ValueHi { get; set; } = new Param<double>("值上限", 0, "ValueHi");

        public Param<double> ValueLo { get; set; } = new Param<double>("值下限", 1, "ValueLo");

        public Param<bool> LockCursor { get; set; } = new Param<bool>("光标", false, "LockCursor");

        public Param<bool> CrossThickness { get; set; } = new Param<bool>("光标线宽", false, "CrossThickness");

        /// <summary>
        /// 全局图表参数
        /// </summary>
        public static Chart2DStyleParams GlobalChartPlotParam { get; set; } = new Chart2DStyleParams();
    }
}
