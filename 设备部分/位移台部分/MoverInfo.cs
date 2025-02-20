using CodeHelper;
using HardWares.纳米位移台;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分.位移台部分
{

    /// <summary>
    /// 位移台信息，包含位移台方向和是否反向的信息
    /// </summary>
    public class NanoMoverInfo : DeviceInfoBase<NanoControllerBase>
    {
        public List<NanoStageInfo> Stages { get; private set; } = new List<NanoStageInfo>();

        public NanoMoverInfo()
        {
        }

        public override void CreateDeviceInfoBehaviour()
        {
            foreach (var item in Device.Stages)
            {
                Stages.Add(new NanoStageInfo(this, item));
            }
        }



        protected override void AutoConnectedAction(FileObject file)
        {
            foreach (var item in Stages)
            {
                if (file.Descriptions.Keys.Contains("AxisPart " + item.Device.AxisName))
                {
                    item.PartType = (PartTypes)Enum.Parse(typeof(PartTypes), file.Descriptions["AxisPart " + item.Device.AxisName]);
                }
                if (file.Descriptions.Keys.Contains("AxisType " + item.Device.AxisName))
                {
                    item.MoverType = (MoverTypes)Enum.Parse(typeof(MoverTypes), file.Descriptions["AxisType " + item.Device.AxisName]);
                }
            }
        }

        protected override void CloseFileAction(FileObject obj)
        {
            foreach (var item in Stages)
            {
                obj.Descriptions.Add("AxisPart " + item.Device.AxisName, Enum.GetName(item.PartType.GetType(), item.PartType));
                obj.Descriptions.Add("AxisType " + item.Device.AxisName, Enum.GetName(item.MoverType.GetType(), item.MoverType));
            }
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }
    }

    public class NanoStageInfo : DeviceElementInfoBase<StageBase>
    {
        public MoverTypes MoverType { get; set; } = MoverTypes.None;

        /// <summary>
        /// 所属部分
        /// </summary>
        public PartTypes PartType { get; set; } = PartTypes.None;

        public NanoMoverInfo Parent { get; set; } = null;

        public NanoStageInfo(NanoMoverInfo info, StageBase stage)
        {
            Parent = info;
            Device = stage;
        }

        public override void CreateDeviceInfoBehaviour()
        {
        }

        public override string GetDeviceDescription()
        {
            return Enum.GetName(PartType.GetType(), PartType) + ":" + Enum.GetName(MoverType.GetType(), MoverType);
        }
    }


    /// <summary>
    /// 移动类型
    /// </summary>
    public enum MoverTypes
    {
        /// <summary>
        /// 未分配位移台
        /// </summary>
        None = -1,
        /// <summary>
        /// X位移台
        /// </summary>
        X = 0,
        /// <summary>
        /// Y位移台
        /// </summary>
        Y = 1,
        /// <summary>
        /// Z位移台
        /// </summary>
        Z = 2,
        /// <summary>
        /// X角度偏摆台
        /// </summary>
        AngleX = 3,
        /// <summary>
        /// Y角度偏摆台
        /// </summary>
        AngleY = 4,
        /// <summary>
        /// Z角度偏摆台
        /// </summary>
        AngleZ = 5
    }

    /// <summary>
    /// 所属部分类型
    /// </summary>
    public enum PartTypes
    {
        /// <summary>
        /// 未分配
        /// </summary>
        None = -1,
        /// <summary>
        /// 探针部分
        /// </summary>
        Probe = 0,
        /// <summary>
        /// 样品部分
        /// </summary>
        Sample = 1,
        /// <summary>
        /// 微波部分
        /// </summary>
        Microwave = 2,
        /// <summary>
        /// 磁铁部分
        /// </summary>
        Magnnet = 3,
        /// <summary>
        /// 镜头部分
        /// </summary>
        Len = 4,
    }
}
