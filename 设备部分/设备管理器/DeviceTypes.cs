using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分
{
    /// <summary>
    /// 设备类型
    /// </summary>
    public enum DeviceTypes
    {
        /// <summary>
        /// 相机
        /// </summary>
        相机 = 0,
        /// <summary>
        /// 翻转镜
        /// </summary>
        翻转镜 = 1,
        /// <summary>
        /// 源表
        /// </summary>
        源表 = 2,
        /// <summary>
        /// 位移台
        /// </summary>
        位移台 = 3,
        /// <summary>
        /// 温控
        /// </summary>
        温控 = 7,
        /// <summary>
        /// 射频源
        /// </summary>
        射频源 = 8,
        /// <summary>
        /// 锁相放大器
        /// </summary>
        锁相放大器 = 9
    }
}
