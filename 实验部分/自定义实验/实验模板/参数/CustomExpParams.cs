using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.IO操作;

namespace ODMR_Lab.自定义实验
{
    public abstract class CustomExpParams : ExpParamBase
    {
        /// <summary>
        /// 实验名称，显示在按钮中
        /// </summary>
        public abstract string ExperimentName { get; set; }
    }
}
