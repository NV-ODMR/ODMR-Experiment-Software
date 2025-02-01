using ODMR_Lab.IO操作;
using ODMR_Lab.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.温度监测
{
    /// <summary>
    /// 温度监控参数列表
    /// </summary>
    public class TemperatureConfigParams : ConfigBase
    {
        public Param<string> DeviceName { get; set; } = new Param<string>("设备名称", "", "DeviceName");

        #region 自动保存参数
        public Param<bool> AutoChooser { get; set; } = new Param<bool>("自动保存", true, "AutoChooser");

        public Param<double> AutoSaveGap { get; set; } = new Param<double>("自动保存间隔", 1, "AutoSaveGap");

        public Param<string> AutoPath { get; set; } = new Param<string>("自动保存路径", "", "AutoPath");

        public Param<string> LastSaveTime { get; set; } = new Param<string>("上次自动保存时间", "", "LastSaveTime");

        public Param<string> Path { get; set; } = new Param<string>("手动保存路径", "", "Path");
        #endregion

        #region 主界面参数

        public Param<double> SampleTimeText { get; set; } = new Param<double>("采样时间间隔", 1, "SampleTimeText");

        #endregion
    }
}
