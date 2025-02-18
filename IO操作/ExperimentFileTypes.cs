using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    /// <summary>
    /// 实验文件类型
    /// </summary>
    public enum ExperimentFileTypes
    {
        None = -1,

        磁场调节 = 0,

        源表IV测量数据 = 1,

        自定义数据 = 2,

        温度监测数据 = 3,

        ODMR实验 = 4,
    }
}
