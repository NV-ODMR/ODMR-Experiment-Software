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
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using System.Linq;
using ODMR_Lab.ODMR实验;
using System.IO;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.二维扫描;
using Controls.Windows;
using ODMR_Lab.实验部分.ODMR实验.实验方法;

namespace ODMR_Lab.实验部分.ODMR实验
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


        private void LoadExps()
        {
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
                if (item.IsAFMSubExperiment == false) continue;
                AFMScan2DExp afm2d = new AFMScan2DExp() { ODMRExperimentName = item.ODMRExperimentName, ParentPage = this };
                afm2d.AddSubExp(Activator.CreateInstance(item.GetType()) as ODMRExpObject);
                ExpObjects.Add(afm2d);
                AFMScan1DExp afm1d = new AFMScan1DExp() { ODMRExperimentName = item.ODMRExperimentName, ParentPage = this };
                afm1d.AddSubExp(Activator.CreateInstance(item.GetType()) as ODMRExpObject);
                ExpObjects.Add(afm1d);
            }

            ExpObjects.Sort((e1, e2) => e1.ODMRExperimentName.CompareTo(e2.ODMRExperimentName));
        }

        public DisplayPage(bool isLoadExps)
        {
            InitializeComponent();
            if (isLoadExps)
                LoadExps();
        }

        public List<KeyValuePair<FrameworkElement, RunningBehaviours>> GetControlsStates()
        {
            List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlsStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(InputPanel, RunningBehaviours.DisableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(DevicePanel, RunningBehaviours.DisableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(OutputPanel, RunningBehaviours.EnableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(ButtonsPanel, RunningBehaviours.DisableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(AutoSavePanel, RunningBehaviours.DisableWhenRunning));
            ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(ScanRangePanel, RunningBehaviours.DisableWhenRunning));
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
                if (CurrentExpObject != null && !CurrentExpObject.IsSubExperiment)
                {
                    foreach (var item in CurrentExpObject.InputParams)
                    {
                        try
                        {
                            item.ReadFromPage(new FrameworkElement[] { this }, false);
                            UnregisterName(ExpParamWindow.GetValidName(item.PropertyName));
                        }
                        catch (Exception ex) { }
                    }
                    foreach (var item in CurrentExpObject.OutputParams)
                    {
                        try
                        {
                            item.ReadFromPage(new FrameworkElement[] { this }, false);
                            UnregisterName(ExpParamWindow.GetValidName(item.PropertyName));
                        }
                        catch (Exception) { }
                    }
                    foreach (var item in CurrentExpObject.DeviceList)
                    {
                        try
                        {
                            item.Value.ReadFromPage(new FrameworkElement[] { this }, false);
                            UnregisterName(ExpParamWindow.GetValidName(item.Value.PropertyName));
                        }
                        catch (Exception ex) { }
                    }
                    CurrentExpObject.IsAutoSave = IsAutoSave.IsSelected;
                }

                CurrentExpObject?.DisConnectOuterControl();

                CurrentExpObject = ExpObjects[index];

                if (ODMRExpObject.Is2DScanExperiment(CurrentExpObject) || ODMRExpObject.Is1DScanExperiment(CurrentExpObject))
                {
                    ScanRangePanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ScanRangePanel.Visibility = Visibility.Hidden;
                }

                ExpName.Text = CurrentExpObject.ODMRExperimentName;
                ExpGroupName.Text = CurrentExpObject.ODMRExperimentGroupName;

                //刷新交互按钮
                ButtonsPanel.Children.Clear();
                foreach (var item in CurrentExpObject.InterativeButtons)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item.Key };
                    InteractBtnTemplate.CloneStyleTo(btn);
                    btn.Height = 40;
                    btn.Margin = new Thickness(5);
                    btn.Click += CurrentExpObject.ButtonClickEvent;
                    ButtonsPanel.Children.Add(btn);
                }

                //更新文件名
                CurrentExpObject.SavedFileName = CurrentExpObject.SavedFileName;

                Chart1D.DataSelectionChanged = CurrentExpObject.SetCurrentData1DInfo;
                Chart2D.DataSelected = CurrentExpObject.SetCurrentData2DInfo;
                CurrentExpObject.SelectDataDisplay();

                var ControlStates = GetControlsStates();

                CurrentExpObject.ConnectOuterControl(StartBtn, StopBtn, ResumeBtn, StartTime, EndTime, ProgressTitle, Progress, ControlStates);

                //刷新图表

                CurrentExpObject.UpdatePlotChart();

                if (CurrentExpObject.NewDisplayWindow == null)
                {
                    ShowInwindowPanel.Visibility = Visibility.Hidden;
                    ExpPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowInwindowPanel.Visibility = Visibility.Visible;
                    ExpPanel.Visibility = Visibility.Hidden;
                }
                if (CurrentExpObject != null)
                {
                    ControlButtonPanel.Visibility = Visibility.Visible;
                }

                //加载这个实验的参数
                InputPanel.Children.Clear();
                OutputPanel.Children.Clear();
                DevicePanel.Children.Clear();

                ExpParamWindow win = new ExpParamWindow(CurrentExpObject, this, false, false, false);

                //更新输入参数
                if (!CurrentExpObject.IsSubExperiment)
                {
                    HashSet<string> gnames = CurrentExpObject.InputParams.Select(x => x.GroupName).ToHashSet();
                    foreach (var item in gnames)
                    {
                        TextBlock l = new TextBlock() { Text = item };
                        l.Height = 30;
                        UIUpdater.CloneStyle(TextBlockTemplate, l);
                        InputPanel.Children.Add(l);
                        var ps = CurrentExpObject.InputParams.Where(x => x.GroupName == item);
                        foreach (var p in ps)
                        {
                            Grid g = win.GenerateControlBar(p, this, true);
                            InputPanel.Children.Add(g);
                            p.LoadToPage(new FrameworkElement[] { this }, false);
                        }
                    }
                    gnames = CurrentExpObject.OutputParams.Select(x => x.GroupName).ToHashSet();
                    foreach (var item in gnames)
                    {
                        TextBlock l = new TextBlock() { Text = item };
                        l.Height = 30;
                        UIUpdater.CloneStyle(TextBlockTemplate, l);
                        OutputPanel.Children.Add(l);
                        var ps = CurrentExpObject.OutputParams.Where(x => x.GroupName == item);
                        foreach (var p in ps)
                        {
                            Grid g = win.GenerateControlBar(p, this, true);
                            OutputPanel.Children.Add(g);
                            p.LoadToPage(new FrameworkElement[] { this }, false);
                        }
                    }

                    gnames = CurrentExpObject.DeviceList.Select(x => x.Value.GroupName).ToHashSet();
                    foreach (var item in gnames)
                    {
                        TextBlock l = new TextBlock() { Text = item };
                        l.Height = 30;
                        UIUpdater.CloneStyle(TextBlockTemplate, l);
                        DevicePanel.Children.Add(l);
                        var ps = CurrentExpObject.DeviceList.Where(x => x.Value.GroupName == item);
                        foreach (var p in ps)
                        {
                            Grid g = win.GenerateDeviceBar(p.Key, p.Value, this);
                            DevicePanel.Children.Add(g);
                            p.Value.LoadToPage(new FrameworkElement[] { this }, false);
                        }
                    }
                    IsAutoSave.IsSelected = CurrentExpObject.IsAutoSave;
                }
                return;
            }
            catch (Exception ex)
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
            {
                if (expobj.NewDisplayWindow != null)
                {
                    expobj.NewDisplayWindow.Topmost = true;
                    expobj.NewDisplayWindow.Topmost = false;
                    ExpPanel.Visibility = Visibility.Collapsed;
                    ShowInwindowPanel.Visibility = Visibility.Visible;
                    CurrentExpObject = expobj;
                }
                else
                {
                    ExpPanel.Visibility = Visibility.Visible;
                    ShowInwindowPanel.Visibility = Visibility.Collapsed;
                    SelectExp(ExpObjects.IndexOf(expobj));
                }
            }
        }
        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowInput(object sender, RoutedEventArgs e)
        {
            ExpParamWindow win = new ExpParamWindow(CurrentExpObject, this, true, false, false);
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.ShowDialog();
        }

        /// <summary>
        /// 输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowOutput(object sender, RoutedEventArgs e)
        {
            ExpParamWindow win = new ExpParamWindow(CurrentExpObject, this, false, true, false);
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.ShowDialog();
        }

        /// <summary>
        /// 设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowDevice(object sender, RoutedEventArgs e)
        {
            ExpParamWindow win = new ExpParamWindow(CurrentExpObject, this, false, false, true);
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.ShowDialog();
        }

        /// <summary>
        /// 自行保存文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CustomSaveFile(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string root = Path.GetDirectoryName(saveFileDialog.FileName);
                    string filename = Path.GetFileName(saveFileDialog.FileName);
                    CurrentExpObject.CustomSaveFile(root, filename);
                    TimeWindow win = new TimeWindow();
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception)
                {
                }
            }
        }

        #region 图表翻转
        private void RevXChanged(object sender, RoutedEventArgs e)
        {
            if (CurrentExpObject == null) return;
            CurrentExpObject.D2ChartXReverse = ChartReverseX.IsSelected;
        }
        private void RevYChanged(object sender, RoutedEventArgs e)
        {
            if (CurrentExpObject == null) return;
            CurrentExpObject.D2ChartYReverse = ChartReverseY.IsSelected;
        }
        #endregion

        private void OpenRangeWindow(object sender, RoutedEventArgs e)
        {
            if (CurrentExpObject.RangeWindow != null)
            {
                CurrentExpObject.RangeWindow.Topmost = true;
                CurrentExpObject.RangeWindow.Topmost = false;
            }
            else
            {
                if (ODMRExpObject.Is2DScanExperiment(CurrentExpObject))
                {
                    CurrentExpObject.RangeWindow = new ScanRangeSelectWindow(CurrentExpObject);
                    var result = CurrentExpObject.D2ScanRange;
                    CurrentExpObject.RangeWindow.ShowD2(result);
                }
                if (ODMRExpObject.Is1DScanExperiment(CurrentExpObject))
                {
                    CurrentExpObject.RangeWindow = new ScanRangeSelectWindow(CurrentExpObject);
                    var result = CurrentExpObject.D1ScanRange;
                    CurrentExpObject.RangeWindow.ShowD1(result);
                }
            }
        }

        private void ShowRangeInformation(object sender, RoutedEventArgs e)
        {
            string name = "未设置范围";
            if (CurrentExpObject != null && CurrentExpObject.D2ScanRange != null)
            {
                name = "扫描类型:" + CurrentExpObject.D2ScanRange.ScanName + "\n" + CurrentExpObject.D2ScanRange.GetDescription() + "\n";
            }
            if (CurrentExpObject != null && CurrentExpObject.D1ScanRange != null)
            {
                name = "扫描类型:" + CurrentExpObject.D1ScanRange.ScanName + "\n" + CurrentExpObject.D1ScanRange.GetDescription() + "\n";
            }
            MessageWindow.ShowTipWindow(name, Window.GetWindow(this));
        }

        private void ShowInWindow(object sender, RoutedEventArgs e)
        {
            if (CurrentExpObject == null) return;
            CurrentExpObject.NewDisplayWindow = new ExpNewWindow(CurrentExpObject.ODMRExperimentGroupName + ":" + CurrentExpObject.ODMRExperimentName, CurrentExpObject, CurrentExpObject.ParentPage);
            CurrentExpObject.NewDisplayWindow.Show();
            ExpPanel.Visibility = Visibility.Hidden;
            ShowInwindowPanel.Visibility = Visibility.Visible;
        }
    }
}
