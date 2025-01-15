using ODMR_Lab.IO操作;
using ODMR_Lab.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// 磁场调节参数列表
    /// </summary>
    public class TemperatureParams : ParamBase
    {
        public Param<string> DeviceName { get; set; } = new Param<string>("设备名称", "");
    }
}
