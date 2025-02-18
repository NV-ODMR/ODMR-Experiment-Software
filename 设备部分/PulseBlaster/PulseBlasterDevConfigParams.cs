using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.IO操作;

namespace ODMR_Lab.设备部分.光子探测器
{
    public class PulseBlasterDevConfigParams : ConfigBase
    {
        /// <summary>
        /// 采样频率
        /// </summary>
        public Param<double> SampleFreq { get; set; } = new Param<double>("采样频率", 100, "SampleFreq");

        /// <summary>
        /// 采样频率
        /// </summary>
        public Param<int> MaxDisplayPoint { get; set; } = new Param<int>("最大显示点数", 1000, "MaxDisplayPoint");

        /// <summary>
        /// 采样频率
        /// </summary>
        public Param<int> MaxSavePoint { get; set; } = new Param<int>("最大保存点数", 100000, "MaxSavePoint");
    }
}
