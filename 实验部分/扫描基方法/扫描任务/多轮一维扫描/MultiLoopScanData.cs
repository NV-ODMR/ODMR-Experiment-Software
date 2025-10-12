using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描
{
    /// <summary>
    /// 多轮扫描数据类型
    /// </summary>
    internal class MultiLoopScanData
    {
        /// <summary>
        /// 数据名称
        /// </summary>
        public string DataName { get; set; } = "";

        /// <summary>
        /// 数据组名称
        /// </summary>
        public string DataGroupName { get; set; } = "";

        /// <summary>
        /// 数据处理方法
        /// </summary>
        public MultiLoopDataProcessBase DataProcessMethod = null;

        /// <summary>
        /// 数据集，外列表表示不同循环的数据，内层列表表示同一循环内的多个数据点
        /// </summary>
        public List<List<double>> DataAssemble { get; set; } = new List<List<double>>();

        /// <summary>
        /// 设置值,如果对应名称的值不存在则添加
        /// </summary>
        /// <param name="Datas"></param>
        /// <returns></returns>
        public static void SetValue(List<MultiLoopScanData> Datas, int loopindex, int datatotalCount, string DataName, string DataGroupName, int dataindex, double datavalue, MultiLoopDataProcessBase dataprocessmethod)
        {
            var result = Datas.Where((x) => { return x.DataName == DataName && x.DataGroupName == DataGroupName; });
            MultiLoopScanData selecteddataassemble = null;
            if (result.Count() == 0)
            {
                var data = new MultiLoopScanData() { DataName = DataName, DataGroupName = DataGroupName, DataProcessMethod = dataprocessmethod };
                Datas.Add(data);
                selecteddataassemble = data;
                for (int i = 0; i < datatotalCount; i++)
                {
                    data.DataAssemble.Add(new List<double>());
                }
            }
            else
            {
                selecteddataassemble = result.ElementAt(0);
            }
            selecteddataassemble.DataAssemble[dataindex].Add(datavalue);
        }

        public static List<double> GetSigmaData(List<MultiLoopScanData> Datas, string DataName, string DataGroupName)
        {
            try
            {
                var data = FindData(Datas, DataName, DataGroupName);
                return data.DataAssemble.Select((x) => data.DataProcessMethod.GetSigma(x, Datas)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<double> GetAverageData(List<MultiLoopScanData> Datas, string DataName, string DataGroupName)
        {
            try
            {
                var data = FindData(Datas, DataName, DataGroupName);
                return data.DataAssemble.Select((x) => data.DataProcessMethod.GetAverage(x, Datas)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MultiLoopScanData FindData(List<MultiLoopScanData> datas, string DataName, string DataGroupName)
        {
            var result = datas.Where((x) => { return x.DataName == DataName && x.DataGroupName == DataGroupName; });
            if (result.Count() != 0) return result.ElementAt(0);
            return null;
        }
    }
}
