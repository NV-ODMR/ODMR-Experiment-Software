using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.IO操作;

namespace ODMR_Lab.设备部分.源表
{
    public class PowerMeterDevConfigParams : ConfigBase
    {
        /// <summary>
        /// 最大采样点数
        /// </summary>
        public Param<int> SamplePoint { get; set; } = new Param<int>("最大采样点数", 100000);

        public Param<double> SampleTime { get; set; } = new Param<double>("采样间隔（S）", 0.1);
    }
}
