using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ODMR_Lab
{
    public abstract class PageBase : Grid
    {
        /// <summary>
        /// 初始化步骤
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// 当程序停止时需要进行的操作
        /// </summary>
        public abstract void CloseBehaviour();
    }
}
