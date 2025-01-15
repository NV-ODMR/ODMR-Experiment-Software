using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// 用户编辑文件
    /// </summary>
    public class UserCustomFileObject : ExperimentFileObject<ParamBase>
    {
        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.自定义数据;

        public override ParamBase Param { get; set; } = null;

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
                    if (t.Name == typeof(double).Name)
                    {
                        DataSource.ChartDataSource1D.Add(new NumricChartData1D() { Data = fobj.ExtractDouble(item), Name = item });
                    }
                    if (t.Name == typeof(DateTime).Name)
                    {
                        DataSource.ChartDataSource1D.Add(new TimeChartData1D() { Data = fobj.ExtractDate(item), Name = item });
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
                if (item is NumricChartData1D)
                {
                    obj.WriteDoubleData(item.Name, (item as NumricChartData1D).Data);
                }
                if (item is TimeChartData1D)
                {
                    obj.WriteDateData(item.Name, (item as TimeChartData1D).Data);
                }
            }
            return obj;
        }
    }
}
