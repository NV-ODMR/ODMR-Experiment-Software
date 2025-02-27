using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验类;
using ODMR_Lab.实验部分.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验.实验方法;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
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
        /// 实验名称
        /// </summary>
        public abstract string ODMRExperimentName { get; set; }
        /// <summary>
        /// 实验分类名
        /// </summary>
        public abstract string ODMRExperimentGroupName { get; set; }

        public DisplayPage ParentPage = null;

        public bool IsAutoSave { get; set; } = false;

        /// <summary>
        /// 是否是AFM子实验
        /// </summary>
        public abstract bool IsAFMSubExperiment { get; protected set; }

        private string savedFileName = null;
        public string SavedFileName
        {
            get { return savedFileName; }
            set
            {
                savedFileName = value;
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (ParentPage.CurrentExpObject == this)
                    {
                        ParentPage.SavedFileName.Content = value;
                        ParentPage.SavedFileName.ToolTip = value;
                    }
                });
            }
        }

        #region 当前选中的图表数据信息
        private bool IsPlot1D { get; set; } = false;

        private string D1GroupName { get; set; } = "";
        private string D2GroupName { get; set; } = "";

        private List<string> D1SelectedYName { get; set; } = new List<string>();

        private string D1SelectedXName { get; set; } = "";

        private string D2DataXName { get; set; } = "";
        private string D2DataYName { get; set; } = "";
        private string D2DataZName { get; set; } = "";

        /// <summary>
        /// 设置当前选中数据(实验方法中禁用)
        /// </summary>
        [Obsolete]
        public void SetCurrentData1DInfo(string xname, List<string> ynames, string groupname)
        {
            D1SelectedXName = xname;
            D1SelectedYName = ynames;
            D1GroupName = groupname;
        }

        /// <summary>
        /// 设置当前选中数据(实验方法中禁用)
        /// </summary>
        [Obsolete]
        public void SetCurrentData2DInfo(string xname, string yname, string zname, string groupname)
        {
            D2DataXName = xname;
            D2DataYName = yname;
            D2DataZName = zname;
            D2GroupName = groupname;
        }

        //设置图表显示(实验方法中禁用)
        [Obsolete]
        public void SelectDataDisplay()
        {
            if (IsPlot1D)
            {
                ParentPage.ChangeVisiblePanel(true);
                ParentPage.Chart1D.SelectData(D1GroupName, D1SelectedXName, D1SelectedYName);
            }
            else
            {
                ParentPage.ChangeVisiblePanel(false);
                ParentPage.Chart2D.SelectData(D2GroupName, D2DataXName, D2DataYName, D2DataZName);
            }
        }

        [Obsolete]
        public void SetPlotType(bool isPlot1D)
        {
            IsPlot1D = isPlot1D;
        }
        #endregion

        /// <summary>
        /// 实验设备
        /// </summary>
        public List<KeyValuePair<string, InfoBase>> ExperimentDevices = new List<KeyValuePair<string, InfoBase>>();

        public override ConfigBase Config { get; set; } = null;

        public override ExpParamBase Param { get; set; } = null;

        /// <summary>
        /// 所属实验
        /// </summary>
        public ODMRExpObject ParentExp = null;

        /// <summary>
        /// 初始化并且刷新参数
        /// </summary>
        public ODMRExpObject()
        {
            foreach (var item in InputParams)
            {
                item.PropertyName = "Input_" + item.PropertyName;
                item.GroupName = ODMRExperimentGroupName + ":" + ODMRExperimentName;
            }
            foreach (var item in OutputParams)
            {
                item.PropertyName = "Output_" + item.PropertyName;
                item.GroupName = ODMRExperimentGroupName + ":" + ODMRExperimentName;
            }
            foreach (var item in DeviceList)
            {
                item.Value.PropertyName = "Device_" + item.Value.PropertyName;
                item.Value.GroupName = ODMRExperimentGroupName;
            }

            var ll = SubExperiments;
            SubExperiments = new List<ODMRExpObject>();

            //添加子实验参数
            foreach (var item in ll)
            {
                AddSubExp(item);
            }

            InterativeButtons = AddInteractiveButtons();
        }

        /// <summary>
        /// 添加交互按钮
        /// </summary>
        /// <returns></returns>
        protected abstract List<KeyValuePair<string, Action>> AddInteractiveButtons();

        [Obsolete]
        public void AddSubExp(ODMRExpObject exp)
        {
            SubExperiments.Add(exp);
            List<ParamB> Inparams = new List<ParamB>();
            List<ParamB> Outparams = new List<ParamB>();
            List<KeyValuePair<DeviceTypes, Param<string>>> Devices = new List<KeyValuePair<DeviceTypes, Param<string>>>();
            foreach (var p in exp.InputParams)
            {
                var pnew = p.Clone();
                pnew.PropertyName = exp.ODMRExperimentGroupName + "_" + exp.ODMRExperimentName + "_" + pnew.PropertyName;
                pnew.GroupName = exp.ODMRExperimentName;
                Inparams.Add(pnew);
            }
            foreach (var p in exp.OutputParams)
            {
                var pnew = p.Clone();
                pnew.PropertyName = exp.ODMRExperimentGroupName + "_" + exp.ODMRExperimentName + "_" + pnew.PropertyName;
                pnew.GroupName = exp.ODMRExperimentName;
                Outparams.Add(pnew);
            }
            foreach (var p in exp.DeviceList)
            {
                var pnew = p.Value.Clone();
                pnew.PropertyName = exp.ODMRExperimentGroupName + "_" + exp.ODMRExperimentName + "_" + pnew.PropertyName;
                pnew.GroupName = exp.ODMRExperimentName;
                Devices.Add(new KeyValuePair<DeviceTypes, Param<string>>(p.Key, (Param<string>)pnew));
            }
            InputParams.AddRange(Inparams);
            OutputParams.AddRange(Outparams);
            DeviceList.AddRange(Devices);
        }

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
            EndStateEvent -= SaveFile;
            EndStateEvent += SaveFile;

            //清除文件名
            savedFileName = "";
            SetExpState("");
            try
            {
                PreExpEvent();
                ODMRExperiment();
                AfterExpEvent();
            }
            catch (Exception ex)
            {
                try
                {
                    AfterExpEvent();
                }
                catch (Exception e) { }
                throw ex;
            }
        }

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [Obsolete]
        public void ButtonClickEvent(object sender, RoutedEventArgs e)
        {
            //添加按钮点击事件
            foreach (var item in InterativeButtons)
            {
                if ((sender as DecoratedButton).Text == item.Key)
                {
                    Thread t = new Thread(() =>
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            (sender as DecoratedButton).IsEnabled = false;
                        });
                        try
                        {
                            item.Value?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                MessageWindow.ShowTipWindow("按钮指令未完成:" + ex.Message, Window.GetWindow(ParentPage));
                            });
                        }
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            (sender as DecoratedButton).IsEnabled = true;
                        });
                    });
                    t.Start();
                }
            }
        }

        /// <summary>
        /// 实验前操作
        /// </summary>
        public abstract void PreExpEvent();

        /// <summary>
        /// 实验后操作
        /// </summary>
        public abstract void AfterExpEvent();

        private Exception expRunningException = null;
        public Exception ExpRunningException { get { return expRunningException; } }

        public void SaveFile()
        {
            if (IsAutoSave)
            {
                try
                {
                    if (ParentPage.SavePath.Content.ToString() == "") return;
                    string root = Path.Combine(ParentPage.SavePath.Content.ToString(), ODMRExperimentGroupName, ODMRExperimentName);
                    if (!Directory.Exists(root))
                    {
                        Directory.CreateDirectory(root);
                    }
                    //获取当前时间
                    DateTime dateTime = DateTime.Now;
                    string date = dateTime.ToString("yyyy_MM_dd_HH_mm_ss");
                    SequenceFileExpObject fob = new SequenceFileExpObject();
                    fob.ExpStartTime = ExpStartTime;
                    fob.ExpEndTime = ExpEndTime;
                    fob.InputParams = InputParams;
                    fob.OutputParams = OutputParams;
                    fob.DeviceList = DeviceList;
                    fob.D1ChartDatas = D1ChartDatas;
                    fob.D2ChartDatas = D2ChartDatas;
                    fob.WriteToFile(root, ODMRExperimentName + date + ".userdat");
                    SetExpState("实验完成,文件已保存.  " + GetExpState());
                    SavedFileName = Path.Combine(ODMRExperimentGroupName, ODMRExperimentName + date);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                SetExpState("实验完成,文件未保存.  " + GetExpState());
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
            //只找主程序的设备
            var maindevList = DeviceList.Where(x => x.Value.PropertyName.Split('_')[0] == "Device");
            foreach (var item in maindevList)
            {
                if (!IsSubExperiment)
                    item.Value.ReadFromPage(new FrameworkElement[] { ParentPage.DevicePanel }, true);
                var res = DeviceDispatcher.GetDevice(item.Key, item.Value.Value);
                if (res == null) throw new Exception("设备未找到:" + item.Value.Description);
                ExperimentDevices.Add(new KeyValuePair<string, InfoBase>(item.Value.PropertyName, res));
                //如果是子程序那么遇到和主程序相同的设备时不用连接 
                if (ParentExp != null)
                {
                    //主程序中不存在此设备 
                    if (ParentExp.ExperimentDevices.Where(x => x.Value == res).Count() == 0)
                        infos.Add(res);
                    else
                    {
                        //如果主程序未执行则要重新连接
                        if (ParentExp.IsExpEnd)
                        {
                            infos.Add(res);
                        }
                    }
                }
                else
                {
                    infos.Add(res);
                }
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
                if (item.Key == "Device_" + name)
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
                if (item.PropertyName == "Input_" + name)
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
        /// 显示一维图表数据
        /// </summary>
        /// <param name="Datas"></param>
        public void Show1DChartData(string groupname, string xname, params string[] ynames)
        {
            Show1DChartData(groupname, xname, ynames.ToList());
        }

        /// <summary>
        /// 显示一维图表数据
        /// </summary>
        /// <param name="Datas"></param>
        public void Show1DChartData(string groupname, string xname, List<string> ynames)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentPage.ChangeVisiblePanel(true);
                ParentPage.Chart1D.SelectData(groupname, xname, ynames.ToList());
            });
        }

        /// <summary>
        /// 显示一维图表数据
        /// </summary>
        /// <param name="Datas"></param>
        public void Show1DFittedData(params string[] FitDataName)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentPage.ChangeVisiblePanel(true);
                ParentPage.Chart1D.SelectFitData(FitDataName.ToList());
            });
        }

        /// <summary>
        /// 显示二维图表数据
        /// </summary>
        /// <param name="Datas"></param>
        public void Show2DChartData(string groupname, string xname, string yname, string zname)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ParentPage.ChangeVisiblePanel(false);
                ParentPage.Chart2D.SelectData(groupname, xname, yname, zname);
            });
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
                if (item.PropertyName == "Output_" + name)
                {
                    ParamB.SetUnknownParamValue(item, value);
                    item.LoadToPage(new FrameworkElement[] { ParentPage }, false);
                }
            }
            return null;
        }

        public void ReadFromPageAndWriteConfigToFile(FileObject obj)
        {
            foreach (var item in InputParams)
            {
                item.ReadFromPage(new FrameworkElement[] { ParentPage }, false);
                obj.Descriptions.Add("Input" + "→" + item.Description + "→" + item.PropertyName + "→" + ODMRExperimentName + "→" + ODMRExperimentGroupName + "→" + GetType().FullName, ParamB.GetUnknownParamValueToString(item));
            }
            foreach (var item in DeviceList)
            {
                item.Value.ReadFromPage(new FrameworkElement[] { ParentPage }, false);
                obj.Descriptions.Add("Dev" + "→" + item.Value.Description + "→" + item.Value.PropertyName + "→" + ODMRExperimentName + "→" + ODMRExperimentGroupName + "→" + GetType().FullName, ParamB.GetUnknownParamValueToString(item.Value));
            }
            obj.Descriptions.Add("IsAutoSave" + "→" + ODMRExperimentName + "→" + ODMRExperimentGroupName + "→" + GetType().FullName, IsAutoSave.ToString());
        }

        public void ReadFromFileAndLoadToPage(FileObject obj)
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat")))
            {
                return;
            }
            var filted = obj.Descriptions.Where(x =>
            {
                var ss = x.Key.Split('→').Reverse();
                if (ss.ElementAt(0) == GetType().FullName && ss.ElementAt(1) == ODMRExperimentGroupName && ss.ElementAt(2) == ODMRExperimentName) return true;
                return false;
            });
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
                ParentPage.Chart1D.DataSource.Clear(false);
                ParentPage.Chart1D.FitData.Clear(false);
                ParentPage.Chart2D.DataSource.Clear(false);
                ParentPage.Chart1D.DataSource.AddRange(D1ChartDatas);
                ParentPage.Chart1D.FitData.AddRange(D1FitDatas);
                ParentPage.Chart2D.DataSource.AddRange(D2ChartDatas);
            });
        }

        /// <summary>
        /// 运行子实验（阻塞）,子实验运行失败则报错,返回执行的子实验
        /// </summary>
        public ODMRExpObject RunSubExperimentBlock(int index, bool ShowWindow = false)
        {
            if (index < 0 || index > SubExperiments.Count - 1) throw new Exception("没有找到子实验");
            ODMRExpObject subExp = SubExperiments[index];

            //设置子实验参数值
            var inputs = InputParams.Where((x) => x.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            foreach (var item in inputs)
            {
                var par = subExp.InputParams.Where(x => x.PropertyName == item.PropertyName.Replace(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_", ""));
                if (par.Count() != 0)
                {
                    ParamB.SetUnknownParamValue(par.ElementAt(0), item.RawValue);
                }
            }

            //设置子实验设备
            var devs = DeviceList.Where((x) => x.Value.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            foreach (var item in devs)
            {
                var par = subExp.DeviceList.Where(x => x.Value.PropertyName == item.Value.PropertyName.Replace(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_", ""));
                if (par.Count() != 0)
                {
                    ParamB.SetUnknownParamValue(par.ElementAt(0).Value, item.Value.RawValue);
                }
            }

            DisConnectOuterControl();



            SubExpWindow win = null;
            if (ShowWindow)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    win = new SubExpWindow("子实验: " + subExp.ODMRExperimentGroupName + ":" + subExp.ODMRExperimentName + " , " + "母实验: " + ODMRExperimentGroupName + ":" + ODMRExperimentName);
                    win.Show(subExp);
                    //设置状态
                    subExp.DisConnectOuterControl();
                    subExp.ConnectOuterControl(ParentPage.StartBtn, ParentPage.StopBtn, ParentPage.ResumeBtn, null, null, subExp.ParentPage.ProgressTitle, subExp.ParentPage.Progress, subExp.ParentPage.GetControlsStates());
                });
            }
            else
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    subExp.DisConnectOuterControl();
                    //设置状态
                    subExp.ConnectOuterControl(ParentPage.StartBtn, ParentPage.StopBtn, ParentPage.ResumeBtn, null, null, null, null, new List<KeyValuePair<FrameworkElement, RunningBehaviours>>());
                });
            }

            subExp.JudgeThreadEndOrResumeAction = JudgeThreadEndOrResume;
            subExp.IsSubExperiment = true;
            subExp.ParentExp = this;
            subExp.Start();

            while (!subExp.IsExpEnd) { Thread.Sleep(50); }

            App.Current.Dispatcher.Invoke(() =>
            {
                subExp.DisConnectOuterControl();
                //设置状态
                ConnectOuterControl(ParentPage.StartBtn, ParentPage.StopBtn, ParentPage.ResumeBtn, ParentPage.StartTime, ParentPage.EndTime, ParentPage.ProgressTitle, ParentPage.Progress, ParentPage.GetControlsStates());
            });

            //关闭子窗口//
            if (win != null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        win.Close();
                        win = null;
                    }
                    catch (Exception) { }
                });
            }

            //设置输出参数
            var outputs = OutputParams.Where((x) => x.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            foreach (var item in outputs)
            {
                var par = subExp.OutputParams.Where(x => x.PropertyName == item.PropertyName.Replace(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_", ""));
                if (par.Count() != 0)
                {
                    SetOutputParamByName(item.PropertyName, par.ElementAt(0).RawValue);
                }
            }

            var controlsStates = SubExperiments[index].ParentPage.GetControlsStates();
            App.Current.Dispatcher.Invoke(() =>
            {
                ConnectOuterControl(ParentPage.StartBtn, ParentPage.StopBtn, ParentPage.ResumeBtn, ParentPage.StartTime, ParentPage.EndTime, ParentPage.ProgressTitle, ParentPage.Progress, controlsStates);
            });
            if (subExp.ExpFailedException != null) throw subExp.ExpFailedException;
            return subExp;
        }
    }
}