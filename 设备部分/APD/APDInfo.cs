using CodeHelper;
using HardWares;
using HardWares.APD;
using HardWares.APD.Exclitas_SPCM_AQRH;
using HardWares.板卡;
using HardWares.板卡.DAQmxCounterSignalChannel;
using HardWares.相机_CCD_;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分.光子探测器
{
    public class APDInfo : DeviceInfoBase<APDBase>
    {
        public override void CreateDeviceInfoBehaviour()
        {
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }

        /// <summary>
        /// 获取连续采样源,找不到则报错
        /// </summary>
        /// <returns></returns>
        private PulseBlasterInfo GetContinusSampleSourceInfo()
        {
            var result = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster).Select(x => x as PulseBlasterInfo);
            foreach (var item in result)
            {
                if (item.Device is HardWares.板卡.DAQmxCounterSignalChannel.PulseBlaster)
                {
                    return item;
                }
            }
            throw new Exception("未找到连续测量的信号源");
        }

        /// <summary>
        /// 获取脉冲采样源,找不到则报错
        /// </summary>
        /// <returns></returns>
        private PulseBlasterInfo GetPulseSourceInfo()
        {
            var result = DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster).Select(x => x as PulseBlasterInfo);
            foreach (var item in result)
            {
                if (!(item.Device is HardWares.板卡.DAQmxCounterSignalChannel.PulseBlaster))
                {
                    return item;
                }
            }
            throw new Exception("未找到连续测量的信号源");
        }

        /// <summary>
        /// 开始连续取样,否则报错
        /// </summary>
        public void StartContinusSample(double frequency)
        {
            var res = GetContinusSampleSourceInfo();
            (res.Device as PulseBlaster).PulseFrequency = frequency;
            res.Device.Start();
            Device.BeginSample(APDTriggerChannels.Channel1, 2);
        }

        /// <summary>
        /// 获取连续取样读数
        /// </summary>
        /// <returns></returns>
        public double GetContinusSampleRatio()
        {
            var res = GetContinusSampleSourceInfo();
            double freq = (res.Device as PulseBlaster).PulseFrequency;
            var cs = Device.GetCounts(1000);
            try
            {
                return (cs[cs.Count - 1] - cs[cs.Count - 2]) * freq;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        /// <summary>
        /// 结束连续取样
        /// </summary>
        public void EndContinusSample()
        {
            var res = GetContinusSampleSourceInfo();
            res.Device.End();
            Device.EndSample();
        }

        /// <summary>
        /// 开始触发取样,否则报错
        /// </summary>
        public void StartTriggerSample(int samplecount)
        {
            var res = GetPulseSourceInfo();
            Device.BeginSample(APDTriggerChannels.Channel2, samplecount);
        }

        /// <summary>
        /// 获取连续取样读数
        /// </summary>
        /// <returns></returns>
        public List<int> GetTriggerSamples(int timeout)
        {
            try
            {
                var cs = Device.GetCounts(timeout);
                return cs;
            }
            catch (Exception) { return new List<int>(); }
        }

        /// <summary>
        /// 结束连续取样
        /// </summary>
        public void EndTriggerSample()
        {
            Device.EndSample();
        }
    }
}
