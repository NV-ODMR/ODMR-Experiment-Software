using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ODMR_Lab.实验部分.磁场调节;
using System.Threading;
using System.Windows;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;

namespace ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描
{
    internal enum MultiScanType
    {
        正向扫描 = 0,
        正反向扫描 = 1,
        随机扫描 = 2
    }
}
