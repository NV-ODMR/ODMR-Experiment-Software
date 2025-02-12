using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    public abstract class ExpPageBase : PageBase
    {
        /// <summary>
        /// 更新UI参数到后台
        /// </summary>
        public abstract void UpdateParam();
    }
}
