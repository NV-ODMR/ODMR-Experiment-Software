using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// 用户编辑文件
    /// </summary>
    public class UserCustomExpObject : ExperimentObject<ExpParamBase, ConfigBase>
    {
        #region 数据及IO部分
        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.自定义数据;

        public override ExpParamBase Param { get; set; } = null;

        public override ConfigBase Config { get; set; } = null;

        public DataVisualSource DataSource { get; set; } = new DataVisualSource();

        public override DataVisualSource ToDataVisualSource()
        {
            return DataSource;
        }

        protected override void InnerRead(FileObject fobj)
        {
            DataSource.Params = fobj.Descriptions;
            List<string> names = fobj.GetDataNames();
            foreach (var item in names)
            {
                Type t = fobj.JudgeDataType(item);
                if (t != null)
                {
                    //处理item，分解为名称，轴类型和组名称
                    string[] ss = item.Split('$');
                    string name = "";
                    string groupname = "";
                    ChartDataType type = ChartDataType.XY;
                    if (ss.Count() == 1)
                    {
                        name = ss[0];
                    }
                    else
                    {
                        name = ss[0];
                        groupname = ss[1];
                        type = (ChartDataType)Enum.Parse(typeof(ChartDataType), ss[2]);
                    }
                    if (t.Name == typeof(double).Name)
                    {
                        DataSource.ChartDataSource1D.Add(new NumricChartData1D(name, groupname, type) { Data = fobj.ExtractDouble(item) });
                    }
                    if (t.Name == typeof(DateTime).Name)
                    {
                        DataSource.ChartDataSource1D.Add(new TimeChartData1D(name, groupname, type) { Data = fobj.ExtractDate(item) });
                    }
                }
            }
        }

        protected override FileObject InnerWrite()
        {
            FileObject obj = new FileObject();
            if (!DataSource.Params.Values.Contains("自定义数据") && DataSource.Params.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", "自定义数据");
            }
            foreach (var item in DataSource.Params)
            {
                if (item.Key == "实验类型" && item.Value != "自定义数据")
                {
                    obj.Descriptions.Add("原实验类型", item.Value);
                }
                else
                {
                    obj.Descriptions.Add(item.Key, item.Value);
                }
            }
            foreach (var item in DataSource.ChartDataSource1D)
            {
                string name = item.Name + "$" + item.GroupName + "$" + Enum.GetName(typeof(ChartDataType), item.DataAxisType);
                if (item is NumricChartData1D)
                {
                    obj.WriteDoubleData(name, (item as NumricChartData1D).Data);
                }
                if (item is TimeChartData1D)
                {
                    obj.WriteDateData(name, (item as TimeChartData1D).Data);
                }
            }
            return obj;
        }
        #endregion

        #region 实验线程部分
        public override void ExperimentEvent()
        {
        }

        public override List<InfoBase> GetDevices()
        {
            return new List<InfoBase>();
        }

        public override ConfigBase ReadConfig()
        {
            return null;
        }
        #endregion
    }
}
