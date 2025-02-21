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
            //查找所有序列实验
            var SequenceTypes = CodeHelper.ClassHelper.GetSubClassTypes(typeof(ODMRExpObject));
            foreach (var item in SequenceTypes)
            {
                ODMRExpObject exp = null;
                if (item.FullName == typeof(SequenceFileExpObject).FullName) continue;
                if (item.GenericTypeArguments.Length != 0)
                {
                    exp = Activator.CreateInstance(item.MakeGenericType(item.GenericTypeArguments)) as ODMRExpObject;
                }
                else
                {
                    exp = Activator.CreateInstance(item) as ODMRExpObject;
                }
                exp.ParentPage = this;
                ExpObjects.Add(exp);
            }
            ExpType.TemplateButton = ExpType;
            foreach (var item in ExpObjects)
            {
                ExpType.Items.Add(new DecoratedButton() { Text = item.ODMRExperimentName, Tag = item });
            }
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
                }

                CurrentExpObject?.DisConnectOuterControl();

                CurrentExpObject = ExpObjects[index];

                List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlsStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();
                ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(InputPanel, RunningBehaviours.DisableWhenRunning));
                ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(DevicePanel, RunningBehaviours.DisableWhenRunning));
                ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(OutputPanel, RunningBehaviours.EnableWhenRunning));

                CurrentExpObject.ConnectOuterControl(StartBtn, StopBtn, ResumeBtn, StartTime, EndTime, ProgressTitle, Progress, ControlsStates);

                //刷新图表
                CurrentExpObject.UpdatePlotChart();

                //加载这个实验的参数
                InputPanel.Children.Clear();
                OutputPanel.Children.Clear();
                DevicePanel.Children.Clear();

                foreach (var item in CurrentExpObject.InputParams)
                {
                    Grid g = GenerateControlBar(item, true);
                    InputPanel.Children.Add(g);
                    item.LoadToPage(new FrameworkElement[] { this }, false);
                }
                foreach (var item in CurrentExpObject.OutputParams)
                {
                    Grid g = GenerateControlBar(item, false);
                    OutputPanel.Children.Add(g);
                    item.LoadToPage(new FrameworkElement[] { this }, false);
                }
                foreach (var item in CurrentExpObject.DeviceList)
                {
                    Grid g = GenerateDeviceBar(item.Key, item.Value);
                    DevicePanel.Children.Add(g);
                    item.Value.LoadToPage(new FrameworkElement[] { this }, false);
                }
                return;
            }
            catch (Exception)
            {
                return;
            }
        }

        private Grid GenerateControlBar(ParamB param, bool IsInput)
        {
            Grid g = new Grid();
            g.Margin = new Thickness(5);

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            g.Height = 35;

            TextBlock tb = new TextBlock() { Text = param.Description, ToolTip = param.Description };
            tb.Margin = new Thickness(5);
            UIUpdater.CloneStyle(TextBlockTemplate, tb);
            g.Children.Add(tb);
            Grid.SetColumn(tb, 0);

            FrameworkElement ui = null;

            if (param.ValueType.Name == typeof(bool).Name)
            {
                ui = new Chooser();
                ui.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                ui.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                ui.Height = 15;
                ui.Width = 30;
                if (!IsInput)
                    ui.IsEnabled = false;
            }
            if (param.ValueType.Name == typeof(int).Name || param.ValueType.Name == typeof(double).Name
                || param.ValueType.Name == typeof(float).Name || param.ValueType.Name == typeof(string).Name)
            {
                TextBox t = new TextBox();
                UIUpdater.CloneStyle(TextBoxTemplate, t);
                ui = t;
                if (!IsInput)
                    t.IsReadOnly = true;
            }
            if (typeof(Enum).IsAssignableFrom(param.ValueType))
            {
                ComboBox c = new ComboBox() { DefaultSelectIndex = 0 };
                c.TemplateButton = ComboBoxTemplate;
                ComboBoxTemplate.CloneStyleTo(c);
                c.TextAreaRatio = ComboBoxTemplate.TextAreaRatio;
                c.IconSource = ComboBoxTemplate.IconSource;
                c.IconStretch = ComboBoxTemplate.IconStretch;
                c.ImagePlace = ComboBoxTemplate.ImagePlace;

                c.Margin = new Thickness(5);
                foreach (var item in Enum.GetNames(param.ValueType))
                {
                    DecoratedButton b = new DecoratedButton() { Text = item };
                    c.Items.Add(b);
                }
                if (!IsInput)
                    c.IsEnabled = false;
                ui = c;
            }
            if (ui != null)
            {
                g.Children.Add(ui);
                Grid.SetColumn(ui, 1);
                if (IsInput)
                    InputPanel.RegisterName(param.PropertyName, ui);
                else
                    OutputPanel.RegisterName(param.PropertyName, ui);
            }
            return g;
        }

        private Grid GenerateDeviceBar(DeviceTypes type, Param<string> param)
        {
            Grid g = new Grid();
            g.Margin = new Thickness(5);

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            g.Height = 35;

            TextBlock tb = new TextBlock() { Text = param.Description, ToolTip = param.Description };
            tb.Margin = new Thickness(5);
            UIUpdater.CloneStyle(TextBlockTemplate, tb);
            g.Children.Add(tb);
            Grid.SetColumn(tb, 0);

            ComboBox box = new ComboBox() { DefaultSelectIndex = 0 };
            box.TemplateButton = ComboBoxTemplate;
            ComboBoxTemplate.CloneStyleTo(box);
            box.TextAreaRatio = ComboBoxTemplate.TextAreaRatio;
            box.IconSource = ComboBoxTemplate.IconSource;
            box.IconStretch = ComboBoxTemplate.IconStretch;
            box.ImagePlace = ComboBoxTemplate.ImagePlace;

            box.Items.Clear();
            var devs = DeviceDispatcher.GetDevice(type);
            foreach (var item in devs)
            {
                DecoratedButton btn = new DecoratedButton() { Text = item.GetDeviceDescription(), Tag = item };
                box.Items.Add(btn);
            }
            box.Select(param.Value);

            box.Click += ((sender, e) =>
            {
                box.Items.Clear();
                devs = DeviceDispatcher.GetDevice(type);
                foreach (var item in devs)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item.GetDeviceDescription(), Tag = item };
                    box.Items.Add(btn);
                }
                box.Select(param.Value);
            });
            DevicePanel.RegisterName(param.PropertyName, box);
            g.Children.Add(box);
            Grid.SetColumn(box, 1);
            return g;
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
            }
            if (sender == D2Btn)
            {
                Chart2D.Visibility = Visibility.Visible;
                D2Btn.KeepPressed = true;
            }
        }

        /// <summary>
        /// 改变实验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeExp(object sender, RoutedEventArgs e)
        {
            SelectExp(ExpObjects.IndexOf(ExpType.SelectedItem.Tag as ODMRExpObject));
        }

        private void ProgressContent_Loaded(object sender, RoutedEventArgs e)
        {
            ExpType.SelectionChanged -= ChangeExp;
            ExpType.SelectionChanged += ChangeExp;
            if (CurrentExpObject == null)
                ExpType.Select(0);
        }
    }
}
