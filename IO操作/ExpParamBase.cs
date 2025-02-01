using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.IO操作
{
    public abstract class ExpParamBase : ParamBase
    {
        /// <summary>
        /// 实验开始时间
        /// </summary>
        public Param<string> ExpStartTime { get; set; } = new Param<string>("开始时间", "", "ExpStartTime");

        /// <summary>
        /// 实验结束时间
        /// </summary>
        public Param<string> ExpEndTime { get; set; } = new Param<string>("结束时间", "", "ExpEndTime");

        public void SetStartTime(DateTime time)
        {
            ExpStartTime.Value = time.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void SetEndTime(DateTime time)
        {
            ExpEndTime.Value = time.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
