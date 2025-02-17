using CodeHelper;
using HardWares.纳米位移台;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HardWares.源表;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.设备部分.源表
{

    /// <summary>
    /// 源表
    /// </summary>
    public class PowerMeterInfo : DeviceInfoBase<PowerSourceBase>
    {
        /// <summary>
        /// 电流测量数据
        /// </summary>
        public List<double> CurrentBuffer { get; set; } = new List<double>();

        /// <summary>
        /// 最大保存点数
        /// </summary>
        public int MaxSavePoint { get; set; } = 1000000;

        /// <summary>
        /// 是否允许自动采样
        /// </summary>
        public bool AllowAutoMeasure { get; set; } = true;

        /// <summary>
        /// 是否正在采样
        /// </summary>
        public bool IsMeasuring { get; set; } = false;

        /// <summary>
        /// 电压测量数据
        /// </summary>
        public List<double> VoltageBuffer { get; set; } = new List<double>();
        /// <summary>
        /// 电压采样时间
        /// </summary>
        public List<DateTime> Times { get; set; } = new List<DateTime>();

        public PowerMeterInfo()
        {
        }

        public override void CreateDeviceInfoBehaviour()
        {
        }

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }

        public void AddMeasurePoint(DateTime time, double voltage, double current)
        {
            Times.Add(time);
            VoltageBuffer.Add(voltage);
            CurrentBuffer.Add(current);
            if (Times.Count > MaxSavePoint)
            {
                VoltageBuffer.RemoveAt(0);
                Times.RemoveAt(0);
                CurrentBuffer.RemoveAt(0);
            }
        }

        public void ClearPoint()
        {
            VoltageBuffer.Clear();
            Times.Clear();
            CurrentBuffer.Clear();
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }
    }
}
