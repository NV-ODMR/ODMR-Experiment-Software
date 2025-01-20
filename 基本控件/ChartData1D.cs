using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.基本控件
{
    public abstract class ChartData1D
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = "";

        public bool IsSelectedAsY { get; set; } = false;

        public bool IsSelectedAsX { get; set; } = false;

        /// <summary>
        /// 是否显示在数据栏中
        /// </summary>
        public bool IsInDataDisplay { get; set; } = false;

        public int GetCount()
        {
            if(this is NumricChartData1D)
            {
                return (this as NumricChartData1D).Data.Count;
            }
            else
            {
                return (this as TimeChartData1D).Data.Count;
            }
        }
    }

    public class NumricChartData1D : ChartData1D
    {
        /// <summary>
        /// 数据
        /// </summary>
        public List<double> Data { get; set; } = new List<double>();
    }

    public class TimeChartData1D : ChartData1D
    {
        /// <summary>
        /// 数据
        /// </summary>
        public List<DateTime> Data { get; set; } = new List<DateTime>();
    }
}
