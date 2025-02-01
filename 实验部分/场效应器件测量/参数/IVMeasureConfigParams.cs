using ODMR_Lab.IO操作;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.场效应器件测量
{
    public class IVMeasureConfigParams : ConfigBase
    {
        /// <summary>
        /// IV测量设备
        /// </summary>
        public Param<string> IVDevice { get; set; } = new Param<string>("IV测量设备", "", "IVDevice");

        /// <summary>
        /// 扫描步长
        /// </summary>
        public Param<double> IVScanStep { get; set; } = new Param<double>("扫描步长(V)", double.NaN, "IVScanStep");

        /// <summary>
        /// 变压步长
        /// </summary>
        public Param<double> IVRampStep { get; set; } = new Param<double>("变压步长(V)", double.NaN, "IVRampStep");

        /// <summary>
        /// 变压间隔
        /// </summary>
        public Param<int> IVRampGap { get; set; } = new Param<int>("变压间隔(ms)", 100, "IVRampGap");

        /// <summary>
        /// 限流值
        /// </summary>
        public Param<double> IVCurrentLimit { get; set; } = new Param<double>("限流值(A)", double.NaN, "IVCurrentLimit");

        /// <summary>
        /// 扫描路径点
        /// </summary>
        public List<double> ScanPoints { get; set; } = new List<double>();
    }
}
