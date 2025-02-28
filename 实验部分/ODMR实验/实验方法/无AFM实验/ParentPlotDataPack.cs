using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验
{
    public class ParentPlotDataPack
    {
        public string Name = "";
        public string GroupName = "";
        public List<double> ChartData = null;
        /// <summary>
        /// 是否连续绘制（当有些情况多次进行子实验，X坐标的数据相同，此时不需要多次在母实验的图表中绘制X数据，此时将此属性设置为false）
        /// </summary>
        public bool IsContinusPlot = false;

        public ChartDataType AxisType = ChartDataType.X;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">在母实验中的数据名</param>
        /// <param name="groupName">在母实验中的数据分组名</param>
        /// <param name="chartData">在母实验中的数据</param>
        /// <param name="isContinusPlot">是否连续绘制（当有些情况多次进行子实验，X坐标的数据相同，此时不需要多次在母实验的图表中绘制X数据，此时将此属性设置为false）</param>
        public ParentPlotDataPack(string name, string groupName, ChartDataType type, List<double> chartData, bool isContinusPlot)
        {
            AxisType = type;
            Name = name;
            GroupName = groupName;
            ChartData = chartData;
            IsContinusPlot = isContinusPlot;
        }
    }
}
