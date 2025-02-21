using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
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
    public abstract class ODMRExpObject : ODMRExpObjectBase
    {
        /// <summary>
        /// 序列实验名称
        /// </summary>
        public abstract string ODMRExperimentName { get; set; }

        public DisplayPage ParentPage = null;

        public bool IsAutoSave { get; set; } = false;

        public string HistoryFileName { get; set; } = "";

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

        public override void ExperimentEvent()
        {
            EndStateEvent += SaveFile;
            EndStateEvent += SaveFile;
            ODMRExperiment();
        }

        public void SaveFile()
        {
            if (IsAutoSave)
            {
                try
                {
                    if (ParentPage.SavePath.Content.ToString() == "") return;
                    string root = Path.Combine(ParentPage.SavePath.Content.ToString(), ODMRExperimentName);
                    if (!Directory.Exists(root))
                    {
                        Directory.CreateDirectory(root);
                    }
                    //获取当前时间
                    DateTime dateTime = DateTime.Now;
                    string date = dateTime.ToString("yyyy_MM_dd_HH_mm_ss");
                    SequenceFileExpObject fob = new SequenceFileExpObject();
                    fob.InputParams = InputParams;
                    fob.OutputParams = OutputParams;
                    fob.DeviceList = DeviceList;
                    fob.D1ChartDatas = D1ChartDatas;
                    fob.D2ChartDatas = D2ChartDatas;
                    fob.WriteToFile(root, ODMRExperimentName + date + ".userdat");
                    SetExpState("实验已完成,已保存文件路径:" + Path.Combine(root, ODMRExperimentName + date + ".userdat"));
                }
                catch (Exception)
                {
                }
            }
            else
            {
                SetExpState("实验已完成,文件未保存");
            }
        }

        /// <summary>
        /// 实验内容
        /// </summary>
        public abstract void ODMRExperiment();

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
                ExperimentDevices.Add(new KeyValuePair<string, InfoBase>(item.Value.PropertyName, res));
            }
            return infos;
        }

        /// <summary>
        /// 根据描述获取实验设备
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public InfoBase GetDeviceByName(string name)
        {
            foreach (var item in ExperimentDevices)
            {
                if (item.Key == name)
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
            obj.Descriptions.Add("IsAutoSave" + "→" + GetType().FullName, IsAutoSave.ToString());
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
            var autosave = filted.Where(x =>
            {
                string[] ss = x.Key.Split('→');
                if (ss[0] == "IsAutoSave") return true;
                return false;
            });
            if (autosave.Count() != 0) IsAutoSave = bool.Parse(autosave.ElementAt(0).Value);
        }

        protected override void InnerToDataVisualSource(DataVisualSource source)
        {
            return;
        }

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
                ParentPage.Chart1D.DataSource.Clear(true);
                ParentPage.Chart2D.DataSource.Clear(true);
                ParentPage.Chart1D.DataSource.AddRange(D1ChartDatas);
                ParentPage.Chart2D.DataSource.AddRange(D2ChartDatas);
            });
        }
    }
}