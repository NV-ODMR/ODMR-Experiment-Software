using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.IO操作;

namespace ODMR_Lab.实验部分.场效应器件测量
{
    internal class VoltageSetConfigParams : ConfigBase
    {
        public Param<double> SetVoltage { get; set; } = new Param<double>("电压", 0, "SetVoltage");

        public Param<double> SetCurrentLimit { get; set; } = new Param<double>("电流限制值", 0.001, "SetCurrentLimit");

        public Param<double> SetRampStep { get; set; } = new Param<double>("变压步长", 0.001, "SetRampStep");

        public Param<double> SetRampGap { get; set; } = new Param<double>("变压间隔", 100, "SetRampGap");
    }
}
