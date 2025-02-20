using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
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
    public abstract class ODMRExpObject : ExperimentObject<ExpParamBase, ConfigBase>
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

        /// <summary>
        /// 实验设备
        /// </summary>
        public List<KeyValuePair<string, InfoBase>> ExperimentDevices = new List<KeyValuePair<string, InfoBase>>();

        public override ConfigBase Config { get; set; } = null;

        public override ExpParamBase Param { get; set; } = null;

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
            ExperimentDevices.Clear();
            List<InfoBase> infos = new List<InfoBase>();
            foreach (var item in DeviceList)
            {
                item.Value.ReadFromPage(new FrameworkElement[] { ParentPage.DevicePanel }, true);
                var res = DeviceDispatcher.GetDevice(item.Key, item.Value.Value);
                if (res == null) throw new Exception("设备未找到:" + item.Value.Description);
                infos.Add(res);
                ExperimentDevices.Add(new KeyValuePair<string, InfoBase>(item.Value.Description, res));
            }
            return infos;
        }

        /// <summary>
        /// 根据描述获取实验设备
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public InfoBase GetDeviceByDescription(string description)
        {
            foreach (var item in ExperimentDevices)
            {
                if (item.Key == description)
                {
                    return item.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据描述获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public ParamB GetInputParamValueByDescription(string description)
        {
            foreach (var item in InputParams)
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
        public dynamic GetInputParamValueByName(string name)
        {
            foreach (var item in InputParams)
            {
                if (item.PropertyName == name)
                {
                    return ParamB.GetUnknownParamValue(item);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据描述获取输出参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public ParamB SetOutputParamByDescription(string description, object value)
        {
            foreach (var item in OutputParams)
            {
                if (item.Description == description)
                {
                    ParamB.SetUnknownParamValue(item, value);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据参数名获取输出参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public ParamB SetOutputParamByName(string name, object value)
        {
            foreach (var item in OutputParams)
            {
                if (item.PropertyName == name)
                {
                    ParamB.SetUnknownParamValue(item, value);
                }
            }
            return null;
        }

        public void ReadFromPageAndWriteConfigToFile(FileObject obj)
        {
            foreach (var item in InputParams)
            {
                item.ReadFromPage(new FrameworkElement[] { ParentPage }, false);
                obj.Descriptions.Add("Input" + "→" + item.Description + "→" + item.PropertyName + "→" + GetType().FullName, ParamB.GetUnknownParamValueToString(item));
            }
            foreach (var item in DeviceList)
            {
                item.Value.ReadFromPage(new FrameworkElement[] { ParentPage }, false);
                obj.Descriptions.Add("Dev" + "→" + item.Value.Description + "→" + item.Value.PropertyName + "→" + GetType().FullName, ParamB.GetUnknownParamValueToString(item.Value));
            }
        }

        public void ReadFromFileAndLoadToPage(FileObject obj)
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat")))
            {
                return;
            }
            var filted = obj.Descriptions.Where(x => x.Key.Split('→').Last() == GetType().FullName);
            foreach (var item in InputParams)
            {
                var res = filted.Where(x =>
                {
                    string[] ss = x.Key.Split('→');
                    if (ss[0] == "Input" && ss[1] == item.Description && ss[2] == item.PropertyName) return true;
                    return false;
                });
                if (res.Count() == 0) continue;
                ParamB.SetUnknownParamValue(item, res.ElementAt(0).Value);
                item.LoadToPage(new FrameworkElement[] { ParentPage }, false);
            }
            foreach (var item in DeviceList)
            {
                var res = filted.Where(x =>
                {
                    string[] ss = x.Key.Split('→');
                    if (ss[0] == "Dev" && ss[1] == item.Value.Description && ss[2] == item.Value.PropertyName) return true;
                    return false;
                });
                if (res.Count() == 0) continue;
                ParamB.SetUnknownParamValue(item.Value, res.ElementAt(0).Value);
                item.Value.LoadToPage(new FrameworkElement[] { ParentPage }, false);
            }
        }

        /// <summary>
        /// 一维图表数据
        /// </summary>
        protected abstract List<ChartData1D> D1ChartDatas { get; set; }
        /// <summary>
        /// 二维图表数据
        /// </summary>
        protected abstract List<ChartData2D> D2ChartDatas { get; set; }

        /// <summary>
        /// 获取一维数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public ChartData1D Get1DChartData(string name, string groupname)
        {
            var data = D1ChartDatas.Where(x => x.GroupName == groupname && x.Name == name);
            if (data.Count() != 0) return data.ElementAt(0);
            return null;
        }

        /// <summary>
        /// 获取一维数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public ChartData2D Get2DChartData(string zname, string groupname)
        {
            var data = D2ChartDatas.Where(x => x.GroupName == groupname && x.Data.ZName == zname);
            if (data.Count() != 0) return data.ElementAt(0);
            return null;
        }


        /// <summary>
        /// 刷新绘图
        /// </summary>
        public void UpdatePlotChartFlow(bool isAutoScale)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentPage.Chart1D.UpdateChartAndDataFlow(isAutoScale);
                ParentPage.Chart2D.UpdateChartAndDataFlow();
            });
        }


        /// <summary>
        /// 刷新绘图
        /// </summary>
        public void UpdatePlotChart()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentPage.Chart1D.DataSource.Clear(false);
                ParentPage.Chart2D.DataSource.Clear(false);
                ParentPage.Chart1D.DataSource.AddRange(D1ChartDatas);
                ParentPage.Chart2D.DataSource.AddRange(D2ChartDatas);
            });
        }



    }
}
