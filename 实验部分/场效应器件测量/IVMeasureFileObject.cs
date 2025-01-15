using CodeHelper;
using ODMR_Lab.基本控件;
using ODMR_Lab.数据处理;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.场效应器件测量
{
    public class IVMeasureFileObject : ExperimentFileObject<IVMeasureParams>
    {
        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.源表IV测量数据;

        /// <summary>
        /// 电流测量值
        /// </summary>
        public List<double> IVIData { get; set; } = new List<double>();

        /// <summary>
        /// 电压测量值
        /// </summary>
        public List<double> IVVData { get; set; } = new List<double>();

        /// <summary>
        /// 电压设置值
        /// </summary>
        public List<double> IVTatgetData { get; set; } = new List<double>();

        /// <summary>
        /// 测量时间
        /// </summary>
        public List<DateTime> IVTimes { get; set; } = new List<DateTime>();

        public override IVMeasureParams Param { get; set; } = new IVMeasureParams();

        protected override void InnerRead(FileObject fobj)
        {
            IVIData = fobj.ExtractDouble("I_Measure");
            IVVData = fobj.ExtractDouble("V_Measure");
            IVTatgetData = fobj.ExtractDouble("V_Target");
            IVTimes = fobj.ExtractDate("Times");

            //添加扫描路径点
            if (fobj.Descriptions.Keys.Contains("ScanPoints"))
            {
                List<string> strs = fobj.Descriptions["ScanPoints"].Split(',').ToList();
                Param.ScanPoints.Clear();
                foreach (var item in strs)
                {
                    try
                    {
                        Param.ScanPoints.Add(double.Parse(item.Replace("V", "")));
                    }
                    catch (Exception) { }
                }
            }
        }

        protected override FileObject InnerWrite()
        {
            FileObject obj = new FileObject();
            obj.Descriptions = Param.GenerateDescription();

            //添加扫描路径点
            string points = "";
            foreach (var item in Param.ScanPoints)
            {
                points += item.ToString() + "V " + ",";
            }
            if (points != "") points = points.Remove(points.Length - 1, 1);

            obj.Descriptions.Add("ScanPoints", points);

            obj.WriteDoubleData("I_Measure", IVIData);
            obj.WriteDoubleData("V_Measure", IVVData);
            obj.WriteDoubleData("V_Target", IVTatgetData);
            obj.WriteDateData("Times", IVTimes);

            return obj;
        }

        public override DataVisualSource ToDataVisualSource()
        {
            DataVisualSource source = new DataVisualSource();

            source.Params.Add("实验类型", Enum.GetName(ExpType.GetType(), ExpType));
            Dictionary<string, string> temp = Param.GetPureDescription();
            foreach (var item in temp)
            {
                source.Params.Add(item.Key, item.Value);
            }

            string scanroute = "";
            foreach (var item in Param.ScanPoints)
            {
                scanroute += item.ToString() + "V ,";
            }
            if (scanroute != "")
            {
                scanroute = scanroute.Remove(scanroute.Length - 1, 1);
            }

            source.Params.Add("扫描路径", scanroute);

            source.ChartDataSource1D.Clear();
            source.ChartDataSource1D.Add(new TimeChartData1D() { Name = "测量时间", Data = IVTimes });
            source.ChartDataSource1D.Add(new NumricChartData1D() { Name = "电流测量值(A)", Data = IVIData });
            source.ChartDataSource1D.Add(new NumricChartData1D() { Name = "电压测量值(V)", Data = IVVData });
            source.ChartDataSource1D.Add(new NumricChartData1D() { Name = "电压设定值(V)", Data = IVTatgetData });
            return source;
        }
    }
}
