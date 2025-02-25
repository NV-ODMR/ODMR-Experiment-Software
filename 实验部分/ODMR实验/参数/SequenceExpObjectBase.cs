using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace ODMR_Lab.ODMR实验
{
    /// <summary>
    /// 自定义实验类型
    /// </summary>
    public abstract class ODMRExpObjectBase : ExperimentObject<ExpParamBase, ConfigBase>
    {
        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.ODMR实验;

        /// <summary>
        /// 输入参数
        /// </summary>
        public abstract List<ParamB> InputParams { get; set; }

        /// <summary>
        /// 输出参数
        /// </summary>
        public abstract List<ParamB> OutputParams { get; set; }

        /// <summary>
        /// 设备列表(设备类型，界面参数)
        /// </summary>
        public abstract List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; }

        /// <summary>
        /// 子实验列表
        /// </summary>
        public abstract List<ODMRExpObject> SubExperiments { get; set; }

        /// <summary>
        /// 交互按钮列表
        /// </summary>
        public List<KeyValuePair<string, Action>> InterativeButtons { get; set; } = new List<KeyValuePair<string, Action>>();

        public override ConfigBase Config { get; set; } = null;

        public override ExpParamBase Param { get; set; } = null;
        /// <summary>
        /// 一维图表数据
        /// </summary>
        public abstract List<ChartData1D> D1ChartDatas { get; set; }
        /// <summary>
        /// 一维图表拟合
        /// </summary>
        public abstract List<FittedData1D> D1FitDatas { get; set; }
        /// <summary>
        /// 二维图表数据
        /// </summary>
        public abstract List<ChartData2D> D2ChartDatas { get; set; }

        protected override void InnerRead(FileObject fobj)
        {
            return;
        }

        protected override void InnerWrite(FileObject obj)
        {
            return;
        }
    }
}