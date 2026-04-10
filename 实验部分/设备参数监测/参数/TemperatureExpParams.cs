using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.IO操作;

namespace ODMR_Lab.实验部分.温度监测
{
    public class TemperatureExpParams : ExpParamBase
    {
        public Param<string> DeviceName { get; set; } = new Param<string>("设备名称", "", "DeviceName");
    }
}
