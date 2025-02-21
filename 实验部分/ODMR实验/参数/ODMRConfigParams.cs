using ODMR_Lab.IO操作;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.参数
{
    public class ODMRConfigParams : ConfigBase
    {
        public Param<string> SavePath { get; set; } = new Param<string>("自动保存根目录", "", "SavePath");
    }
}
