using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    /// </summary>
    public class LaserOn : ScanCoreBase
    {
        /// <summary>
        /// 打开激光(脉冲总长度:10微秒)：输入参数：激光通道占空比(浮点数,小于1)
        /// 设备:板卡
        /// 返回参数:无
        /// </summary>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            double duty = (double)InputParams[0];
            PulseBlasterInfo pb = devices[0] as PulseBlasterInfo;
            SequenceChannel ch = pb.FindChannelEnumOfDescription("激光触发通道");
            int totaltime = 10 * 1000;
            List<CommandBase> cmds = new List<CommandBase>();
            if (duty == 1)
            {
                cmds.Add(new CommandLine(new List<int>() { (int)ch }, totaltime));
                cmds.Add(new BranchCommandLine(0));
                pb.Device.SetCommands(cmds);
                pb.Device.Start();
                return new List<object>();
            }
            if (duty == 0)
            {
                cmds.Add(new CommandLine(new List<int>() { }, totaltime));
                cmds.Add(new BranchCommandLine(0));
                pb.Device.SetCommands(cmds);
                pb.Device.Start();
                return new List<object>();
            }
            cmds.Add(new CommandLine(new List<int>() { (int)ch }, (int)(totaltime * duty)));
            cmds.Add(new CommandLine(new List<int>() { }, (int)(totaltime * (1 - duty))));
            cmds.Add(new BranchCommandLine(0));
            pb.Device.SetCommands(cmds);
            pb.Device.Start();
            return new List<object>();
        }
    }
}
