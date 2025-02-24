using Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using ODMR_Lab.IO操作;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = Controls.ComboBox;
using ODMR_Lab.设备部分;
using ODMR_Lab.实验类;
using System.Windows.Forms;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM.二维扫描;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.线扫描;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM.线扫描;

namespace ODMR_Lab.ODMR实验
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {

        public override string PageName { get; set; } = "ODMR实验";

        /// <summary>
        /// 输入参数
        /// </summary>
        public List<ParamB> InputParams { get; set; } = new List<ParamB>();

        /// <summary>
        /// 输出参数
        /// </summary>
        public List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        /// <summary>
        /// 设备
        /// </summary>
        public List<ParamB> Devices { get; set; } = new List<ParamB>();

        public List<ODMRExpObject> ExpObjects { get; set; } = new List<ODMRExpObject>();

        public ODMRExpObject CurrentExpObject = null;

        public DisplayPage()
        {
            InitializeComponent();
            //查找所有实验
            var SequenceTypes = CodeHelper.ClassHelper.GetSubClassTypes(typeof(ODMRExpObject));
            List<ODMRExpObject> noafms = new List<ODMRExpObject>();
            //查找无AFM实验类型
            foreach (var item in SequenceTypes)
            {
                if (item.BaseType.FullName == typeof(ODMRExperimentWithoutAFM).FullName ||
                    item.BaseType.BaseType.FullName == typeof(ODMRExperimentWithoutAFM).FullName)
                {
                    ODMRExpObject exp = Activator.CreateInstance(item) as ODMRExpObject;
                    exp.ParentPage = this;
                    noafms.Add(exp);
                }
            }
            ExpObjects.AddRange(noafms);
            //设置所有AFM点实验类型
            foreach (var item in noafms)
            {
                AFMScan2DExp afm2d = new AFMScan2DExp() { ODMRExperimentName = item.ODMRExperimentName };
                afm2d.SubExperiments.Add(Activator.CreateInstance(item.GetType()) as ODMRExpObject);
                ExpObjects.Add(afm2d);
                AFMScan0DExp afm0d = new AFMScan0DExp() { ODMRExperimentName = item.ODMRExperimentName };
                afm0d.SubExperiments.Add(Activator.CreateInstance(item.GetType()) as ODMRExpObject);
                ExpObjects.Add(afm0d);
                AFMScan1DExp afm1d = new AFMScan1DExp() { ODMRExperimentName = item.ODMRExperimentName };
                afm1d.SubExperiments.Add(Activator.CreateInstance(item.GetType()) as ODMRExpObject);
                ExpObjects.Add(afm1d);
            }
        }

        public List<KeyValuePair<FrameworkElement, RunningBehaviours>> GetControlsStates()
        {
            List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlsStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(InputPanel, RunningBehaviours.DisableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(DevicePanel, RunningBehaviours.DisableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(OutputPanel, RunningBehaviours.EnableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(AutoSavePanel, RunningBehaviours.DisableWhenRunning));
            return ControlsStates;
        }

        /// <summary>
        /// 刷新当前实验
        /// </summary>
        public void SelectExp(int index)
        {
            try
            {
                if (index > ExpObjects.Count - 1 || index < 0) return;

                //存储上一个实验的参数
                if (CurrentExpObject != null)
                {
                    foreach (var item in CurrentExpObject.InputParams)
                    {
                        try
                        {
                            item.ReadFromPage(new FrameworkElement[] { this }, false);
                            InputPanel.UnregisterName(item.PropertyName);
                        }
                        catch (Exception ex) { }
                    }
                    foreach (var item in CurrentExpObject.OutputParams)
                    {
                        try
                        {
                            item.ReadFromPage(new FrameworkElement[] { this }, false);
                            OutputPanel.UnregisterName(item.PropertyName);
                        }
                        catch (Exception) { }
                    }
                    foreach (var item in CurrentExpObject.DeviceList)
                    {
                        try
                        {
                            item.Value.ReadFromPage(new FrameworkElement[] { this }, false);
                            DevicePanel.UnregisterName(item.Value.PropertyName);
                        }
                        catch (Exception ex) { }
                    }
                    CurrentExpObject.IsAutoSave = IsAutoSave.IsSelected;
                }

                CurrentExpObject?.DisConnectOuterControl();


                CurrentExpObject = ExpObjects[index];

                //更新文件名
                CurrentExpObject.SavedFileName = CurrentExpObject.SavedFileName;

                Chart1D.DataSelectionChanged = CurrentExpObject.SetCurrentData1DInfo;
                Chart2D.DataSelected = CurrentExpObject.SetCurrentData2DInfo;
                CurrentExpObject.SelectDataDisplay();

                var ControlStates = GetControlsStates();

                CurrentExpObject.ConnectOuterControl(StartBtn, StopBtn, ResumeBtn, StartTime, EndTime, ProgressTitle, Progress, ControlStates);

                //刷新图表

                CurrentExpObject.UpdatePlotChart();

                //加载这个实验的参数
                InputPanel.Children.Clear();
                OutputPanel.Children.Clear();
                DevicePanel.Children.Clear();

                ExpParamWindow win = new ExpParamWindow(CurrentExpObject, this, false, false, false);
                foreach (var item in CurrentExpObject.InputParams)
                {
                    Grid g = win.GenerateControlBar(item, this, true);
                    InputPanel.Children.Add(g);
                    item.LoadToPage(new FrameworkElement[] { this }, false);
                }
                foreach (var item in CurrentExpObject.OutputParams)
                {
                    Grid g = win.GenerateControlBar(item, this, false);
                    OutputPanel.Children.Add(g);
                    item.LoadToPage(new FrameworkElement[] { this }, false);
                }
                foreach (var item in CurrentExpObject.DeviceList)
                {
                    Grid g = win.GenerateDeviceBar(item.Key, item.Value, this);
                    DevicePanel.Children.Add(g);
                    item.Value.LoadToPage(new FrameworkElement[] { this }, false);
                }
                IsAutoSave.IsSelected = CurrentExpObject.IsAutoSave;
                return;
            }
            catch (Exception)
            {
                return;
            }
        }

        public override void InnerInit()
        {
        }


        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }

        /// <summary>
        /// 切换显示界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangePannel(object sender, RoutedEventArgs e)
        {
            Chart1D.Visibility = Visibility.Collapsed;
            Chart2D.Visibility = Visibility.Collapsed;
            D1Btn.KeepPressed = false;
            D2Btn.KeepPressed = false;
            if (sender == D1Btn)
            {
                Chart1D.Visibility = Visibility.Visible;
                D1Btn.KeepPressed = true;
                CurrentExpObject?.SetPlotType(true);
            }
            if (sender == D2Btn)
            {
                Chart2D.Visibility = Visibility.Visible;
                D2Btn.KeepPressed = true;
                CurrentExpObject?.SetPlotType(false);
            }
        }

        public void ChangeVisiblePanel(bool isD1)
        {
            if (isD1)
            {
                ChangePannel(D1Btn, new RoutedEventArgs());
            }
            else
            {
                ChangePannel(D2Btn, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// 选择文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SavePath.Content = dialog.SelectedPath;
                SavePath.ToolTip = dialog.SelectedPath;
            }
        }

        private void ChangeAutoSave(object sender, RoutedEventArgs e)
        {
            if (CurrentExpObject != null)
                CurrentExpObject.IsAutoSave = IsAutoSave.IsSelected;
        }

        /// <summary>
        /// 选择实验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectExp(object sender, RoutedEventArgs e)
        {
            ExpSelectWindow win = new ExpSelectWindow(this);
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var expobj = win.ShowDialog();
            if (expobj != null)
                SelectExp(ExpObjects.IndexOf(expobj));
        }

        /// <summary>
        /// 输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowOutput(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowDevice(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowInput(object sender, RoutedEventArgs e)
        {

        }
    }
}
