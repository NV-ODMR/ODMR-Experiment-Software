using ODMR_Lab.IO操作;
using ODMR_Lab.位移台部分;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.自定义实验
{
    /// <summary>
    /// 磁场调节参数列表
    /// </summary>
    public abstract class CustomConfigParams : ConfigBase
    {
        /// <summary>
        /// 设备名称列表
        /// </summary>
        public abstract Dictionary<DeviceTypes, Param<string>> DeviceNameParams { get; set; }
    }
}
