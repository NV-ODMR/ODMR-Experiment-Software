using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    /// <summary>
    /// CW谱核心方法
    /// </summary>
    public class CW : ScanCoreBase
    {
        /// <summary>
        /// CW单点数据：输入参数：微波频率(MHz,浮点数),微波功率(dBm,浮点数),
        /// 设备:板卡,微波源
        /// 返回参数:CW光子计数列表
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            //读取CW序列
            SequenceDataAssemble.ReadFromSequenceName("CW");
        }
    }
}
