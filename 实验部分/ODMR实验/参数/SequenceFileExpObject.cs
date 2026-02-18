using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeHelper;
using Controls.Charts;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.实验部分.ODMR实验.参数
{
    /// <summary>
    /// 专门用于文件保存的ODMR实验类
    /// </summary>
    public class SequenceFileExpObject : ODMRExpObjectBase
    {
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>();
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>();
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();


        /// <summary>
        /// 实验名称
        /// </summary>
        public override string ODMRExperimentName { get; set; }
        /// <summary>
        /// 实验分类名
        /// </summary>
        public override string ODMRExperimentGroupName { get; set; }

        public override void ExperimentEvent()
        {
            return;
        }

        public override bool PreConfirmProcedure()
        {
            return false;
        }

        protected override void InnerToDataVisualSource(DataVisualSource source)
        {
            source.Params.Add("实验名:", ODMRExperimentGroupName + ":" + ODMRExperimentName);
            #region 读取输入输出参数
            foreach (var item in InputParams)
            {
                var names = item.PropertyName.Split('_');
                if (names.First() == "Input")
                    source.Params.Add("配置参数:" + item.Description, ParamB.GetUnknownParamValue(item));
                else
                    source.Params.Add("配置参数:" + names[0] + ":" + names[1] + ":" + item.Description, ParamB.GetUnknownParamValue(item));
            }
            foreach (var item in DeviceList)
            {
                var names = item.Value.PropertyName.Split('_');
                if (names.First() == "Dev")
                    source.Params.Add("设备:" + item.Value.Description, ParamB.GetUnknownParamValue(item.Value));
                else
                    source.Params.Add("设备:" + names[0] + ":" + names[1] + ":" + item.Value.Description, ParamB.GetUnknownParamValue(item.Value));
            }
            foreach (var item in OutputParams)
            {
                try
                {
                    var names = item.PropertyName.Split('_');
                    if (names.First() == "Output")
                        source.Params.Add("输出参数:" + item.Description, ParamB.GetUnknownParamValue(item));
                    else
                    {
                        source.Params.Add("输出参数:" + names[0] + ":" + names[1] + ":" + item.Description, ParamB.GetUnknownParamValue(item));
                    }
                }
                catch (Exception)
                {
                }
            }
            #endregion
            #region 读取图表
            source.ChartDataSource1D = D1ChartDatas;
            source.ChartDataSource2D = D2ChartDatas;
            #endregion
        }


        public override List<InfoBase> GetDevices()
        {
            return new List<InfoBase>();
        }

        public override ConfigBase ReadConfig()
        {
            return null;
        }


        protected override void InnerWrite(FileObject obj)
        {
            #region 导出实验名
            obj.Descriptions.Add("实验分组", ODMRExperimentGroupName);
            obj.Descriptions.Add("实验名", ODMRExperimentName);
            #endregion
            #region 导出输入输出和设备参数
            foreach (var item in InputParams)
            {
                obj.Descriptions.Add("Input" + "→" + item.Description + "→" + item.PropertyName, ParamB.GetUnknownParamValueToString(item));
            }
            foreach (var item in OutputParams)
            {
                obj.Descriptions.Add("Output" + "→" + item.Description + "→" + item.PropertyName, ParamB.GetUnknownParamValueToString(item));
            }
            foreach (var item in DeviceList)
            {
                obj.Descriptions.Add("Dev" + "→" + item.Value.Description + "→" + item.Value.PropertyName + "→" + Enum.GetName(typeof(DeviceTypes), item.Key), ParamB.GetUnknownParamValueToString(item.Value));
            }
            #endregion
            #region 导出图表数据
            foreach (var item in D1ChartDatas)
            {
                obj.WriteDoubleData("C1DData" + "→" + item.Name + "→" + item.GroupName + "→" + Enum.GetName(item.DataAxisType.GetType(), item.DataAxisType), (item as NumricChartData1D).Data);
            }
            foreach (var item in D2ChartDatas)
            {
                List<DataPoint> p = new List<DataPoint>();
                for (int i = 0; i < item.Data.YCounts; i++)
                {
                    DataPoint po = new DataPoint();
                    for (int j = 0; j < item.Data.XCounts; j++)
                    {
                        po.Data.Add(item.Data.GetValue(j, i));
                    }
                    p.Add(po);
                }
                obj.WritePointData("C2DData" + "→" + item.GroupName + "→" + item.Data.XName + "→" + item.Data.YName + "→" + item.Data.ZName + "→" +
                    item.Data.XLo.ToString() + "→" + item.Data.XHi.ToString() + "→" + item.Data.XCounts.ToString()
                    + "→" + item.Data.YLo.ToString() + "→" + item.Data.YHi.ToString() + "→" + item.Data.YCounts.ToString(), p);
            }
            #endregion
        }

        protected override void InnerRead(FileObject fobj)
        {
            #region 导入实验名
            if (fobj.Descriptions.Keys.Contains("实验分组"))
            {
                ODMRExperimentGroupName = fobj.Descriptions["实验分组"];
            }
            if (fobj.Descriptions.Keys.Contains("实验名"))
            {
                ODMRExperimentName = fobj.Descriptions["实验名"];
            }
            #endregion
            #region 导入输入输出和设备参数
            foreach (var v in fobj.Descriptions)
            {
                if (v.Key.Contains("Input" + "→"))
                {
                    try
                    {
                        string[] ss = v.Key.Split('→');
                        InputParams.Add(new Param<string>(ss[1], v.Value, ss[2]));
                    }
                    catch (Exception)
                    {
                    }
                }
                if (v.Key.Contains("Output" + "→"))
                {
                    try
                    {
                        string[] ss = v.Key.Split('→');
                        OutputParams.Add(new Param<string>(ss[1], v.Value, ss[2]));
                    }
                    catch (Exception)
                    {
                    }
                }
                if (v.Key.Contains("Dev" + "→"))
                {
                    try
                    {
                        string[] ss = v.Key.Split('→');
                        DeviceList.Add(new KeyValuePair<DeviceTypes, Param<string>>((DeviceTypes)Enum.Parse(typeof(DeviceTypes), ss[3]), new Param<string>(ss[1], v.Value, ss[2])));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            #endregion
            #region 导入图表数据
            var names = fobj.GetDataNames();
            foreach (var item in names)
            {
                if (item.Contains("C1DData" + "→"))
                {
                    string[] ss = item.Split('→');
                    NumricChartData1D data = new NumricChartData1D(ss[1], ss[2], (ChartDataType)Enum.Parse(typeof(ChartDataType), ss[3]));
                    data.Data = fobj.ExtractDouble(item);
                    D1ChartDatas.Add(data);
                }
                if (item.Contains("C2DData" + "→"))
                {
                    string[] ss = item.Split('→');
                    ChartData2D data = new ChartData2D(new FormattedDataSeries2D(int.Parse(ss[7]), double.Parse(ss[5]), double.Parse(ss[6]), int.Parse(ss[10]), double.Parse(ss[8]), double.Parse(ss[9])));
                    var res = fobj.ExtractPoint(item);
                    List<List<double>> d = new List<List<double>>();
                    foreach (var p in res)
                    {
                        d.Add(p.Data);
                    }
                    data.Data.SetZValues(d);
                    data.Data.XName = ss[2];
                    data.Data.YName = ss[3];
                    data.Data.ZName = ss[4];
                    data.GroupName = ss[1];
                    D2ChartDatas.Add(data);
                }
            }
            #endregion
        }

        /// <summary>
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic GetInputParamValueByName(string name)
        {
            foreach (var item in InputParams)
            {
                if (item.PropertyName == "Input_" + name)
                {
                    return ParamB.GetUnknownParamValue(item);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据描述获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic GetOutPutParamValueByDescription(string description)
        {
            foreach (var item in OutputParams)
            {
                if (item.Description == description)
                {
                    return ParamB.GetUnknownParamValue(item);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic GetOutputParamValueByName(string name)
        {
            foreach (var item in OutputParams)
            {
                if (item.PropertyName == name || item.PropertyName == "Output_" + name)
                {
                    return ParamB.GetUnknownParamValue(item);
                }
            }
            return null;
        }

        /// <summary>
        /// 从文件读取ODMR实验信息
        /// </summary>
        public static void ReadODMRExpImformationFromFile(string Path, out string ODMRGroupName, out string ODMRExpName)
        {
            var dic = FileObject.ReadDescription(Path);
            if (dic.ContainsKey("实验分组") && dic.ContainsKey("实验名"))
            {
                ODMRGroupName = dic["实验分组"];
                ODMRExpName = dic["实验名"];
                return;
            }
            ODMRExpName = "";
            ODMRGroupName = "";
            return;
        }
    }
}
