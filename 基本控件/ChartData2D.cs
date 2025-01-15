using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ODMR_Lab.基本控件
{
    public class ChartData2D
    {
        public string Name { get; set; } = "";

        public List<List<double>> ZData { get; set; } = new List<List<double>>();

        public ChartData2D(List<List<double>> Z)
        {
            ZData = Z;
        }

        public int Row
        {
            get { return ZData.Count; }
        }

        public int Column
        {
            get
            {
                if (Row == 0) return 0;
                return ZData[0].Count;
            }
        }
    }
}
