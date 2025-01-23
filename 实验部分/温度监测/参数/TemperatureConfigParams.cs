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
    public class TemperatureConfigParams : ConfigBase
    {
        public Param<string> DeviceName { get; set; } = new Param<string>("设备名称", "");

        #region 自动保存参数
        public Param<bool> AutoChooser { get; set; } = new Param<bool>("自动保存", true);

        public Param<double> AutoSaveGap { get; set; } = new Param<double>("自动保存间隔", 1);

        public Param<string> AutoPath { get; set; } = new Param<string>("自动保存路径", "");

        public Param<string> LastSaveTime { get; set; } = new Param<string>("上次自动保存时间", "");

        public Param<string> Path { get; set; } = new Param<string>("手动保存路径", "");
        #endregion

        #region 主界面参数

        public Param<double> SampleTimeText { get; set; } = new Param<double>("采样时间间隔", 1);

        #endregion
    }
}
