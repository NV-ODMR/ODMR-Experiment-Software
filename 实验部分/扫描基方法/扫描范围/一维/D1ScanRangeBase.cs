using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法.扫描范围
{
    /// <summary>
    /// 一维扫描范围基类
    /// </summary>
    public abstract class D1ScanRangeBase : ScanRangeBase
    {
        public int Counts { get; protected set; } = 1;

        public bool Reverse { get; protected set; } = false;
    }
}
