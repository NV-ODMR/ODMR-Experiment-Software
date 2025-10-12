using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.纳米位移台.PI;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验类;
using ODMR_Lab.实验部分.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验.实验方法;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.二维扫描;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ODMR_Lab.ODMR实验
{
    /// <summary>
    /// 自定义实验类型
    /// </summary>
    public abstract class ODMRExpObject : ODMRExpObjectBase
    {
        public DisplayPage ParentPage = null;

        public bool IsAutoSave { get; set; } = true;

        public virtual bool IsDisplayAsExp { get; set; } = true;

        /// <summary>
        /// 实验描述
        /// </summary>
        public abstract string Description { get; set; }

        #region 二维和一维扫描范围
        public abstract bool Is1DScanExp { get; set; }
        public D1PointsScanRangeBase D1ScanRange { get; set; } = null;
        public abstract bool Is2DScanExp { get; set; }
        public D2ScanRangeBase D2ScanRange { get; set; } = null;

        /// <summary>
        /// 扫描范围窗口
        /// </summary>
        public ScanRangeSelectWindow RangeWindow { get; set; } = null;
        #endregion

        /// <summary>
        /// 是否独立显示在窗口
        /// </summary>
        public ExpNewWindow NewDisplayWindow { get; set; } = null;

        /// <summary>
        /// 子程序执行要显示的窗口
        /// </summary>
        public SubExpWindow SubExpDisplayWindow { get; set; } = null;

        /// <summary>
        /// 是否是AFM子实验
        /// </summary>
        public abstract bool IsAFMSubExperiment { get; protected set; }

        /// <summary>
        /// 如果实验有循环轮数,那么每轮循环完成后可能要进行一些操作,此时可以调用此委托以在主实验和子实验之间通信
        /// </summary>
        public Action LoopEndMethod { get; set; } = null;

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

        public static bool IsAFMScanExperiment(ODMRExpObject exp)
        {
            Type ptype = exp.GetType();
            while (ptype != null)
            {
                string name = ptype.FullName;
                if (ptype.IsGenericType) name = ptype.GetGenericTypeDefinition().FullName;
                if (name == typeof(AFMScan1DExp).FullName || name == typeof(AFMScan2DExp).FullName)
                {
                    return true;
                }
                ptype = ptype.BaseType;
            }
            return false;
        }

        public static bool IsNoAFMScanExperiment(ODMRExpObject exp)
        {
            Type ptype = exp.GetType();
            while (ptype != null)
            {
                string name = ptype.FullName;
                if (ptype.IsGenericType) name = ptype.GetGenericTypeDefinition().FullName;
                if (name == typeof(ODMRExperimentWithoutAFM).FullName)
                {
                    return true;
                }
                ptype = ptype.BaseType;
            }
            return false;
        }

        public static bool IsNoAFMScanExperiment(Type exp)
        {
            Type ptype = exp;
            while (ptype != null)
            {
                string name = ptype.FullName;
                if (ptype.IsGenericType) name = ptype.GetGenericTypeDefinition().FullName;
                if (name == typeof(ODMRExperimentWithoutAFM).FullName)
                {
                    return true;
                }
                ptype = ptype.BaseType;
            }
            return false;
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

        private bool d2ChartXReverse = false;
        public bool D2ChartXReverse
        {
            get
            {
                return d2ChartXReverse;
            }
            set
            {
                d2ChartXReverse = value;
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (ParentPage.CurrentExpObject == this)
                    {
                        ParentPage.Chart2D.ReverseX = value;
                    }
                });
            }
        }

        private bool d2ChartYReverse = false;
        public bool D2ChartYReverse
        {
            get
            {
                return d2ChartYReverse;
            }
            set
            {
                d2ChartYReverse = value;
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (ParentPage.CurrentExpObject == this)
                    {
                        ParentPage.Chart2D.ReverseY = value;
                    }
                });
            }
        }

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
                ParentPage.Chart2D.ReverseX = D2ChartXReverse;
                ParentPage.Chart2D.ReverseY = D2ChartYReverse;
                ParentPage.ChartReverseX.IsSelected = D2ChartXReverse;
                ParentPage.ChartReverseY.IsSelected = D2ChartYReverse;
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
        /// 最顶层的实验
        /// </summary>
        public ODMRExpObject TopExp = null;

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
            foreach (var item in DeviceList)
            {
                item.Value.PropertyName = "Device_" + item.Value.PropertyName;
                item.Value.GroupName = ODMRExperimentGroupName;
            }

            ValidateParameters();
        }

        public void ValidateParameters()
        {
            //清除所有子实验参数
            var subinput = InputParams.Where((x) => x.PropertyName.IndexOf("Input_") != 0).ToList();
            var subdev = DeviceList.Where((x) => x.Value.PropertyName.IndexOf("Device_") != 0).ToList();
            foreach (var item in subinput)
            {
                InputParams.Remove(item);
            }
            foreach (var item in subdev)
            {
                DeviceList.Remove(item);
            }

            var ll = GetSubExperiments();
            foreach (var item in ll)
            {
                item.IsSubExperiment = true;
                item.ParentExp = this;
                item.TopExp = this;
            }

            if (IsSubExperiment == false)
            {
                //获取所有子实验以及子实验的子实验
                var subexps = AppendSubExp(this, new List<ODMRExpObject>());

                //添加子实验参数
                foreach (var item in subexps)
                {
                    var exp = item.ParentExp;
                    AddSubExp(item, false);
                    item.TopExp = this;
                    item.ParentExp = exp;
                }
            }
            SubExperiments = ll;

            InterativeButtons = AddInteractiveButtons();
        }

        public ODMRExpObject(bool isSubExperiment)
        {
            IsSubExperiment = isSubExperiment;

            foreach (var item in InputParams)
            {
                item.PropertyName = "Input_" + item.PropertyName;
                item.GroupName = ODMRExperimentGroupName + ":" + ODMRExperimentName;
            }
            foreach (var item in DeviceList)
            {
                item.Value.PropertyName = "Device_" + item.Value.PropertyName;
                item.Value.GroupName = ODMRExperimentGroupName;
            }

            var ll = GetSubExperiments();
            foreach (var item in ll)
            {
                item.IsSubExperiment = true;
                item.ParentExp = this;
                item.TopExp = this;
            }

            if (IsSubExperiment == false)
            {
                //获取所有子实验以及子实验的子实验
                var subexps = AppendSubExp(this, new List<ODMRExpObject>());

                //添加子实验参数
                foreach (var item in subexps)
                {
                    var exp = item.ParentExp;
                    AddSubExp(item, false);
                    item.TopExp = this;
                    item.ParentExp = exp;
                }
            }
            SubExperiments = ll;

            InterativeButtons = AddInteractiveButtons();

        }

        /// <summary>
        /// 列出所有用到的子实验列表(序号从0开始,作为调用RunSubexpBlock的序号)
        /// </summary>
        /// <returns></returns>
        protected abstract List<ODMRExpObject> GetSubExperiments();

        //添加子实验
        public static List<ODMRExpObject> AppendSubExp(ODMRExpObject exp, List<ODMRExpObject> originlist)
        {
            foreach (var item in exp.GetSubExperiments())
            {
                originlist.Add(item);
                originlist = AppendSubExp(item, originlist);
            }
            return originlist;
        }

        protected void AddDevicesToList(List<KeyValuePair<DeviceTypes, Param<string>>> vs)
        {
            foreach (var item in vs)
            {
                item.Value.PropertyName = "Device_" + item.Value.PropertyName;
                item.Value.GroupName = ODMRExperimentGroupName;
                DeviceList.Add(item);
            }
        }

        /// <summary>
        /// 添加交互按钮
        /// </summary>
        /// <returns></returns>
        protected abstract List<KeyValuePair<string, Action>> AddInteractiveButtons();

        [Obsolete]
        public void AddSubExp(ODMRExpObject exp, bool addtoList = true)
        {
            if (addtoList)
                SubExperiments.Add(exp);
            exp.IsSubExperiment = true;
            exp.ParentExp = this;
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
            foreach (var p in exp.DeviceList)
            {
                var pnew = p.Value.Clone();
                pnew.PropertyName = exp.ODMRExperimentGroupName + "_" + exp.ODMRExperimentName + "_" + pnew.PropertyName;
                pnew.GroupName = exp.ODMRExperimentName;
                Devices.Add(new KeyValuePair<DeviceTypes, Param<string>>(p.Key, (Param<string>)pnew));
            }
            InputParams.AddRange(Inparams);
            DeviceList.AddRange(Devices);
        }

        /// <summary>
        /// 添加子实验的输入参数到主实验中
        /// </summary>
        /// <param name="exp"></param>
        public void AddSubexpOutputParams(ODMRExpObject exp)
        {
            foreach (var p in exp.OutputParams)
            {
                var pnew = p.Clone();
                pnew.PropertyName = exp.ODMRExperimentGroupName + "_" + exp.ODMRExperimentName + "_" + pnew.PropertyName;
                pnew.GroupName = exp.ODMRExperimentName;
                OutputParams.Add(pnew);
            }
        }

        public override ConfigBase ReadConfig()
        {
            if (ParentPage != null)
                foreach (var item in InputParams)
                {
                    item.ReadFromPage(new FrameworkElement[] { ParentPage.InputPanel }, true);
                }
            return null;
        }

        private void ClearOutputParams()
        {
            //清除面板参数
            App.Current.Dispatcher.Invoke(() =>
            {
                if (ParentPage != null)
                {
                    foreach (var item in OutputParams)
                    {
                        try
                        {
                            ParentPage.UnregisterName(ExpParamWindow.GetValidName(item.PropertyName));
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (ParentPage.CurrentExpObject == this)
                    {
                        for (int i = 0; i < ParentPage.OutputPanel.Children.Count; i++)
                        {
                            if ((ParentPage.OutputPanel.Children[i] as FrameworkElement).Name != "")
                            {
                                ParentPage.UnregisterName((ParentPage.OutputPanel.Children[i] as FrameworkElement).Name);
                            }
                            ParentPage.OutputPanel.Children.RemoveAt(i);
                            --i;
                        }
                    }
                }
            });
            OutputParams.Clear();
        }

        private void UpdateOutputParams()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ////生成子实验输出参数
                //foreach (var item in SubExperiments)
                //{
                //    AddSubexpOutputParams(item);
                //}
                //更新到窗口
                if (ParentPage != null)
                    if (ParentPage.CurrentExpObject == this && !ParentPage.CurrentExpObject.IsSubExperiment)
                    {
                        foreach (var item in OutputParams)
                        {
                            item.PropertyName = "Output_" + item.PropertyName;
                            Grid g = ExpParamWindow.GenerateControlBar(item, ParentPage, false);
                            ParentPage.OutputPanel.Children.Add(g);
                            item.LoadToPage(new FrameworkElement[] { ParentPage }, false);
                        }
                    }
            });
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
                //清除输出参数
                ClearOutputParams();
                //清除图表数据
                D1ChartDatas.Clear();
                D2ChartDatas.Clear();
                D1FitDatas.Clear();
                UpdatePlotChart();
                PreExpEvent();
                ODMRExperiment();
                AfterExpEvent();

                //刷新输出参数到窗口
                UpdateOutputParams();
            }
            catch (Exception ex)
            {
                try
                {
                    AfterExpEvent();
                    //刷新输出参数到窗口
                    UpdateOutputParams();
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
                    string name = ODMRExperimentName + date + ".userdat";
                    CustomSaveFile(root, name);
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

        public void CustomSaveFile(string filepath, string filename)
        {
            SequenceFileExpObject fob = new SequenceFileExpObject();
            fob.ExpStartTime = ExpStartTime;
            fob.ExpEndTime = ExpEndTime;
            fob.InputParams = InputParams;
            fob.OutputParams = OutputParams;
            fob.DeviceList = DeviceList;
            fob.ODMRExperimentGroupName = ODMRExperimentGroupName;
            fob.ODMRExperimentName = ODMRExperimentName;
            fob.D1ChartDatas = D1ChartDatas;
            fob.D2ChartDatas = D2ChartDatas;
            fob.WriteToFile(filepath, filename);
            SavedFileName = Path.Combine(filepath, filename);
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
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic SetInputParamValueByName(string name, object value)
        {
            foreach (var item in InputParams)
            {
                if (item.PropertyName == "Input_" + name)
                {
                    ParamB.SetUnknownParamValue(item, value);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic SetInputParamValueByDescription(string description, object value)
        {
            foreach (var item in InputParams)
            {
                if (item.Description == description)
                {
                    ParamB.SetUnknownParamValue(item, value);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic SetDeviceByDescription(string description, object value)
        {
            foreach (var item in DeviceList)
            {
                if (item.Value.Description == description)
                {
                    ParamB.SetUnknownParamValue(item.Value, value);
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
                if (ParentPage != null)
                {
                    ParentPage.ChangeVisiblePanel(true);
                    ParentPage.Chart1D.SelectData(groupname, xname, ynames.ToList());
                }
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
                if (ParentPage != null)
                {
                    ParentPage.ChangeVisiblePanel(true);
                    ParentPage.Chart1D.SelectFitData(FitDataName.ToList());
                }
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
                if (ParentPage != null)
                {
                    ParentPage.ChangeVisiblePanel(false);
                    ParentPage.Chart2D.SelectData(groupname, xname, yname, zname);
                }
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
            obj.Descriptions.Add("Reverse2DX" + "→" + ODMRExperimentName + "→" + ODMRExperimentGroupName + "→" + GetType().FullName, D2ChartXReverse.ToString());
            obj.Descriptions.Add("Reverse2DY" + "→" + ODMRExperimentName + "→" + ODMRExperimentGroupName + "→" + GetType().FullName, D2ChartYReverse.ToString());
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
            autosave = filted.Where(x =>
            {
                string[] ss = x.Key.Split('→');
                if (ss[0] == "Reverse2DX") return true;
                return false;
            });
            if (autosave.Count() != 0) D2ChartXReverse = bool.Parse(autosave.ElementAt(0).Value);
            autosave = filted.Where(x =>
            {
                string[] ss = x.Key.Split('→');
                if (ss[0] == "Reverse2DY") return true;
                return false;
            });
            if (autosave.Count() != 0) D2ChartYReverse = bool.Parse(autosave.ElementAt(0).Value);
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
        public List<double> Get1DChartDataSource(string name, string groupname)
        {
            var data = D1ChartDatas.Where(x => x.GroupName == groupname && x.Name == name);
            if (data.Count() != 0) return (data.ElementAt(0) as NumricChartData1D).Data;
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
                if (ParentPage != null)
                {
                    ParentPage.Chart1D.UpdateChartAndDataFlow(isAutoScale);
                    ParentPage.Chart2D.UpdateChartAndDataFlow();
                }
            });
        }


        /// <summary>
        /// 刷新绘图
        /// </summary>
        public void UpdatePlotChart()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (ParentPage != null)
                {
                    ParentPage.Chart1D.DataSource.Clear(false);
                    ParentPage.Chart1D.FitData.Clear(false);
                    ParentPage.Chart2D.DataSource.Clear(false);
                    ParentPage.Chart1D.DataSource.AddRange(D1ChartDatas);
                    ParentPage.Chart1D.FitData.AddRange(D1FitDatas);
                    ParentPage.Chart2D.DataSource.AddRange(D2ChartDatas);
                }
            });
        }


        /// <summary>
        /// 运行子实验（阻塞）,子实验运行失败则报错,返回执行的子实验
        /// </summary>
        /// <param name="index"></param>
        /// <param name="ShowWindow"></param>
        /// <param name="InputParams">需要预设的参数,参数名要和实验参数名保持一致，Description不必一致</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ODMRExpObject RunSubExperimentBlock(int index, bool ShowWindow = false, List<ParamB> inputParams = null)
        {
            if (index < 0 || index > SubExperiments.Count - 1) throw new Exception("没有找到子实验");
            ODMRExpObject subExp = SubExperiments[index];

            //设置子实验参数值
            IEnumerable<ParamB> inputs;
            if (IsSubExperiment == false)
                inputs = InputParams.Where((x) => x.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            else
            {
                inputs = TopExp.InputParams.Where((x) => x.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            }
            foreach (var item in inputs)
            {
                var par = subExp.InputParams.Where(x => x.PropertyName == item.PropertyName.Replace(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_", ""));
                if (par.Count() != 0)
                {
                    if (inputParams != null)
                    {
                        var inp = inputParams.Where(x => "Input_" + x.PropertyName == par.ElementAt(0).PropertyName);
                        if (inp.Count() != 0)
                        {
                            //如果在预设参数中设置则优先选择预设参数
                            ParamB.SetUnknownParamValue(par.ElementAt(0), inp.ElementAt(0).RawValue);
                            continue;
                        }
                    }
                    ParamB.SetUnknownParamValue(par.ElementAt(0), item.RawValue);
                }
            }

            //设置子实验设备
            IEnumerable<KeyValuePair<DeviceTypes, Param<string>>> devs;
            if (IsSubExperiment == false)
                devs = DeviceList.Where((x) => x.Value.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            else
            {
                devs = TopExp.DeviceList.Where((x) => x.Value.PropertyName.Contains(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_"));
            }
            foreach (var item in devs)
            {
                var par = subExp.DeviceList.Where(x => x.Value.PropertyName == item.Value.PropertyName.Replace(subExp.ODMRExperimentGroupName + "_" + subExp.ODMRExperimentName + "_", ""));
                if (par.Count() != 0)
                {
                    ParamB.SetUnknownParamValue(par.ElementAt(0).Value, item.Value.RawValue);
                }
            }

            DisConnectOuterControl();

            subExp.JudgeThreadEndOrResumeAction = JudgeThreadEndOrResumeAction;
            subExp.IsSubExperiment = true;
            subExp.ParentExp = this;
            subExp.DisConnectODMRParentExperiment();

            if (ShowWindow)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (subExp.SubExpDisplayWindow == null) subExp.SubExpDisplayWindow = new SubExpWindow("子实验: " + subExp.ODMRExperimentGroupName + ":" + subExp.ODMRExperimentName + " , " + "母实验: " + ODMRExperimentGroupName + ":" + ODMRExperimentName);
                    subExp.SubExpDisplayWindow.Show(subExp);
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

            subExp.Start();

            while (!subExp.IsExpEnd) { Thread.Sleep(50); }

            App.Current.Dispatcher.Invoke(() =>
            {
                subExp.DisConnectOuterControl();
                //设置状态
                ConnectOuterControl(ParentPage.StartBtn, ParentPage.StopBtn, ParentPage.ResumeBtn, ParentPage.StartTime, ParentPage.EndTime, ParentPage.ProgressTitle, ParentPage.Progress, ParentPage.GetControlsStates());
            });

            //关闭子窗口//
            if (ShowWindow)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        subExp.SubExpDisplayWindow.EndUse();
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
            if (subExp.ExpFailedException != null)
                throw subExp.ExpFailedException;
            return subExp;
        }
    }
}