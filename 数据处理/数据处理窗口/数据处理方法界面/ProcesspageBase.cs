using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ODMR_Lab.数据处理.数据处理窗口.数据处理方法界面
{
    public abstract class ProcesspageBase : Grid
    {
        public DataVisualSource ParentDataSource { get; set; } = null;

        public abstract string ProcessName { get; set; }

        public abstract ChartDataBase CalculateMethod();

        /// <summary>
        /// 更新方法
        /// </summary>
        public virtual void UpdateMethod()
        {
        }
    }
}
