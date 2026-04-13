using ODMR_Lab.IO操作;
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
    public class ParamListenerConfigParams : ConfigBase
    {
        #region 主界面参数

        public Param<double> SampleTimeText { get; set; } = new Param<double>("采样时间间隔(s)", 1, "SampleTimeText");

        public Param<int> StoredPoints { get; set; } = new Param<int>("存储点数", 100000, "StoredPoints");

        public Param<int> DisplayedPoints { get; set; } = new Param<int>("存储点数", 1000, "DisplayedPoints");
        
        #endregion
    }
}
