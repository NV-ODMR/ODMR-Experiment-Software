using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 二维扫描范围基类
    /// </summary>
    public abstract class ScanRangeBase
    {
        public abstract string ScanName { get; protected set; }
        /// <summary>
        /// 生成范围描述
        /// </summary>
        /// <returns></returns>
        public abstract string GetDescription();

    }
}
