using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.板卡;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore
{
    /// <summary>
    /// CW谱核心方法
    /// </summary>
    public class CWCore : ScanCoreBase
    {
        /// <summary>
        /// CW单点数据：输入参数：微波频率(MHz,浮点数),微波功率(dBm,浮点数),循环次数(整数),超时时间(ms)(整数)
        /// 设备:板卡,微波源,APD
        /// 返回参数:对比度(失败返回0),加微波时的总光子计数(整数),不加微波时的总光子计数(整数)
        /// </summary>
        /// <param name="InputParams"></param>
        /// <param name="devices"></param>
        /// <returns></returns>
        public override List<object> CoreMethod(List<object> InputParams, params InfoBase[] devices)
        {
            //读取CW序列
            var sequence = SequenceDataAssemble.ReadFromSequenceName("CW");
            PulseBlasterInfo pb = devices[0] as PulseBlasterInfo;
            APDInfo apd = devices[2] as APDInfo;
            RFSourceInfo mwsource = devices[1] as RFSourceInfo;
            double frequency = (double)InputParams[0];
            double power = (double)InputParams[1];
            int loopcount = (int)InputParams[2];
            int timeout = (int)InputParams[3];

            sequence.LoopCount = loopcount;

            //打开微波源
            //设置频率和功率
            mwsource.Device.IsRFOutOpen = true;
            mwsource.Device.RFFrequency = frequency;
            mwsource.Device.RFAmplitude = power;

            //设置板卡指令
            List<CommandBase> Lines = new List<CommandBase>();
            Lines = sequence.AddToCommandLine(Lines, out string commandinform);
            pb.Device.SetCommands(Lines);
            //打开APD
            apd.StartTriggerSample(loopcount * 4 );
            //打开板卡输出
            pb.Device.Start();
            var countresult = apd.GetTriggerSamples(timeout);
            //关闭APD
            apd.EndTriggerSample();
            //关闭板卡输出
            pb.Device.End();
            //处理数据
            var countwithMW = countresult.Where((v, ind) => ind % 2 == 0);      //偶数项
            var countafterwithMW = countwithMW.Where((v, ind) => ind % 2 == 1);
            var countbeforewithMW = countwithMW.Where((v, ind) => ind % 2 == 0);
            var countwithoutMW = countresult.Where((v, ind) => ind % 2 == 1);
            var countafterwithoutMW = countwithoutMW.Where((v, ind) => ind % 2 == 1);
            var countbeforewithoutMW = countwithoutMW.Where((v, ind) => ind % 2 == 0);

            try
            {
                int sumwithMW = 0;
                for (int i = 0; i < countafterwithMW.Count(); i++)
                {
                    sumwithMW += Math.Abs(countafterwithMW.ElementAt(i) - countbeforewithMW.ElementAt(i));
                }

                int sumwithoutMW = 0;
                for (int i = 0; i < countafterwithoutMW.Count(); i++)
                {
                    sumwithoutMW += Math.Abs(countafterwithoutMW.ElementAt(i) - countbeforewithoutMW.ElementAt(i));
                }
                double contrast = (sumwithMW - sumwithoutMW) / (double)sumwithoutMW;
                return new List<object>() { contrast, sumwithMW, sumwithoutMW };
            }
            catch (Exception)
            {
                return new List<object>() { 0.0, 0, 0 };
            }
        }
    }
}
