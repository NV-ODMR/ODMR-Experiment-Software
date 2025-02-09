using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controls.Charts;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ODMR_Lab.基本控件
{
    public class ChartData2D
    {

        public ChartData2D(FormattedDataSeries2D data)
        {
            Data = data;
        }

        public FormattedDataSeries2D Data { get; set; } = null;

        public string GroupName { get; set; } = "";

        public bool IsSelected { get; set; } = false;

        public string GetDescription()
        {
            return "X: " + Data.XName + " Y: " + Data.YName + " Z: " + Data.ZName;
        }

        public int GetXCount()
        {
            return Data.XCounts;
        }

        public int GetYCount()
        {
            return Data.YCounts;
        }
    }
}
