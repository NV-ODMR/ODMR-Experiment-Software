using CodeHelper;
using HardWares;
using HardWares.射频源;
using HardWares.射频源.Rigol_DSG_3060;
using HardWares.相机_CCD_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分.射频源_锁相放大器
{
    public class SignalGeneratorChannelInfo
    {
        public SignalGeneratorInfo ParentInfo { get; set; } = null;

        public SignalChannelBase Channel { get; set; } = null;

        public SignalGeneratorChannelInfo(SignalGeneratorInfo parentInfo, SignalChannelBase channel)
        {
            ParentInfo = parentInfo;
            Channel = channel;
        }
    }
}
