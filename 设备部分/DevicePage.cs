using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分
{
    public abstract class DevicePageBase : PageBase
    {
        /// <summary>
        /// 刷新设备面板
        /// </summary>
        public abstract void RefreshPanels();
    }
}
