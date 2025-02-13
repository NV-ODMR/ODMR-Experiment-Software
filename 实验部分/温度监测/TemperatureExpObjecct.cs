using CodeHelper;
using Controls;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.温度监测;
using ODMR_Lab.数据处理;
using ODMR_Lab.温度监测部分;
using ODMR_Lab.磁场调节;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ComboBox = Controls.ComboBox;
using DisplayPage = ODMR_Lab.磁场调节.DisplayPage;

namespace ODMR_Lab.实验部分.磁场调节
{
    /// <summary>
    /// 温度监控的文件类
    /// </summary>
    public class TemperatureExpObject : ExperimentObject<CustomExpParams, TemperatureConfigParams>
    {
        #region 实验数据及IO部分
        /// <summary>
        /// 选中的温度通道
        /// </summary>
        public List<ChartData1D> SelectedChannelsData { get; set; } = new List<ChartData1D>();

        /// <summary>
        /// 参数列表
        /// </summary>
        public override CustomExpParams Param { get; set; } = new CustomExpParams();

        /// <summary>
        /// 参数列表
        /// </summary>
        public override TemperatureConfigParams Config { get; set; } = new TemperatureConfigParams();


        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.温度监测数据;

        /// <summary>
        /// 读取温度数据文件
        /// </summary>
        /// <param name="fobj"></param>
        protected override void InnerRead(FileObject fobj)
        {
            List<string> names = fobj.GetDataNames();
            foreach (var item in names)
            {
                if (fobj.JudgeDataType(item).IsEquivalentTo(typeof(DateTime)))
                {
                    SelectedChannelsData.Add(new TimeChartData1D(item, "温度监测数据") { Data = fobj.ExtractDate(item) });
                }
                if (fobj.JudgeDataType(item).IsEquivalentTo(typeof(double)))
                {
                    SelectedChannelsData.Add(new NumricChartData1D(item, "温度监测数据") { Data = fobj.ExtractDouble(item) });
                }
            }
        }

        protected override FileObject InnerWrite()
        {
            FileObject obj = new FileObject();

            foreach (var item in SelectedChannelsData)
            {
                if (item is TimeChartData1D)
                {
                    obj.WriteDateData(item.Name, (item as TimeChartData1D).Data);
                }
                if (item is NumricChartData1D)
                {
                    obj.WriteDoubleData(item.Name, (item as NumricChartData1D).Data);
                }
            }

            return obj;
        }

        public override DataVisualSource ToDataVisualSource()
        {
            DataVisualSource source = new DataVisualSource();
            source.ChartDataSource1D = SelectedChannelsData;
            source.Params.Add("实验类型", Enum.GetName(ExpType.GetType(), ExpType));
            Dictionary<string, string> temp = Param.GetPureDescription();
            foreach (var item in temp)
            {
                source.Params.Add(item.Key, item.Value);
            }
            return source;
        }
        #endregion

        #region 实验线程部分
        public override void ExperimentEvent()
        {
        }

        public override TemperatureConfigParams ReadConfig()
        {
            return null;
        }

        public override List<InfoBase> GetDevices()
        {
            return null;
        }

        public override bool PreConfirmProcedure()
        {
            return true;
        }
        #endregion
    }
}
