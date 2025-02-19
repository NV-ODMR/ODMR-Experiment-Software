using ODMR_Lab.IO操作;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ODMR_Lab.ODMR实验
{
    /// <summary>
    /// 自定义实验类型
    /// </summary>
    public abstract class SequenceExpObject : ExperimentObject<ExpParamBase, ConfigBase>
    {
        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.ODMR实验;

        /// <summary>
        /// 序列实验名称
        /// </summary>
        public abstract string ODMRExperimentName { get; set; }

        /// <summary>
        /// 输入参数
        /// </summary>
        public abstract List<ParamB> InputParams { get; set; }

        /// <summary>
        /// 输出参数
        /// </summary>
        public abstract List<ParamB> OutputParams { get; set; }

        public DisplayPage ParentPage = null;

        /// <summary>
        /// 设备列表(设备类型，界面参数)
        /// </summary>
        public abstract List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; }

        public List<KeyValuePair<string, InfoBase>> ExperimentDevices = new List<KeyValuePair<string, InfoBase>>();

        public override ConfigBase ReadConfig()
        {
            foreach (var item in InputParams)
            {
                item.ReadFromPage(new FrameworkElement[] { ParentPage.InputPanel }, true);
            }
            return null;
        }

        protected override void InnerToDataVisualSource(DataVisualSource s)
        {

            foreach (var item in InputParams)
            {
                s.Params.Add(item.Description, ParamB.GetUnknownParamValue(item));
            }
            foreach (var item in OutputParams)
            {
                s.Params.Add(item.Description, ParamB.GetUnknownParamValue(item));
            }
            foreach (var item in DeviceList)
            {
                s.Params.Add(item.Value.Description, ParamB.GetUnknownParamValue(item.Value));
            }
            SetDataToDataVisualSource(s);
        }

        /// <summary>
        /// 向DataVisualSource中设置数据，用于文件导入后显示在数据可视化窗口
        /// </summary>
        /// <param name="source"></param>
        public abstract void SetDataToDataVisualSource(DataVisualSource source);

        public override List<InfoBase> GetDevices()
        {
            List<InfoBase> infos = new List<InfoBase>();
            foreach (var item in DeviceList)
            {
                item.Value.ReadFromPage(new FrameworkElement[] { ParentPage.DevicePanel }, true);
                var res = DeviceDispatcher.GetDevice(item.Key, item.Value.Value);
                if (res == null) throw new Exception("设备未找到:" + item.Value.Description);
                infos.Add(res);
            }
            return infos;
        }
    }
}
