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

namespace ODMR_Lab.序列实验
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

        public List<SequenceExpObject> ExpObjects { get; set; } = new List<SequenceExpObject>();

        SequenceExpObject CurrentExpObject = null;

        public DisplayPage()
        {
            InitializeComponent();
            //查找所有序列实验
            var SequenceTypes = CodeHelper.ClassHelper.GetSubClassTypes(typeof(SequenceExpObject));
            foreach (var item in SequenceTypes)
            {
                var exp = Activator.CreateInstance(item) as SequenceExpObject;
                exp.ParentPage = this;
                ExpObjects.Add(exp);
            }
            ExpType.TemplateButton = ExpType;
            foreach (var item in ExpObjects)
            {
                ExpType.Items.Add(new DecoratedButton() { Text = item.ODMRExperimentName, Tag = item });
            }
        }

        protected void UpdateInputPanel()
        {
            foreach (var item in InputPanel.Children)
            {
                UnregisterName((item as FrameworkElement).Name);
            }
            if (CurrentExpObject == null) return;
            //设备列表
            foreach (var item in CurrentExpObject.InputParams)
            {
                Grid g = GenerateControlBar(item, true);
                DevicePanel.Children.Add(g);
            }
        }

        protected void UpdateOutputPanel()
        {
            foreach (var item in OutputPanel.Children)
            {
                UnregisterName((item as FrameworkElement).Name);
            }
            if (CurrentExpObject == null) return;
            //设备列表
            foreach (var item in CurrentExpObject.OutputParams)
            {
                Grid g = GenerateControlBar(item, false);
                DevicePanel.Children.Add(g);
            }
        }

        protected void UpdateDevicePanel()
        {
            foreach (var item in DevicePanel.Children)
            {
                UnregisterName((item as FrameworkElement).Name);
            }
            if (CurrentExpObject == null) return;
            //设备列表
            foreach (var item in CurrentExpObject.DeviceList)
            {
                Grid g = GenerateDeviceBar(item.Key, item.Value);
                DevicePanel.Children.Add(g);
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
                        item.ReadFromPage(new FrameworkElement[] { InputPanel }, false);
                    }
                    foreach (var item in CurrentExpObject.OutputParams)
                    {
                        item.ReadFromPage(new FrameworkElement[] { OutputPanel }, false);
                    }
                    foreach (var item in CurrentExpObject.DeviceList)
                    {
                        item.Value.ReadFromPage(new FrameworkElement[] { DevicePanel }, false);
                    }
                }

                CurrentExpObject?.DisConnectOuterControl();

                CurrentExpObject = ExpObjects[index];

                List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlsStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();
                ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(InputPanel, RunningBehaviours.DisableWhenRunning));
                ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(DevicePanel, RunningBehaviours.DisableWhenRunning));
                ControlsStates.Add(new KeyValuePair<FrameworkElement, RunningBehaviours>(OutputPanel, RunningBehaviours.EnableWhenRunning));

                CurrentExpObject.ConnectOuterControl(StartBtn, StopBtn, ResumeBtn, StartTime, EndTime, ProgressTitle, Progress, ControlsStates);


                //加载这个实验的参数
                UpdateDevicePanel();
                UpdateOutputPanel();
                UpdateInputPanel();
                foreach (var item in CurrentExpObject.InputParams)
                {
                    item.LoadToPage(new FrameworkElement[] { InputPanel }, false);
                }
                foreach (var item in CurrentExpObject.OutputParams)
                {
                    item.LoadToPage(new FrameworkElement[] { OutputPanel }, false);
                }
                foreach (var item in CurrentExpObject.DeviceList)
                {
                    item.Value.LoadToPage(new FrameworkElement[] { DevicePanel }, false);
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

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });

            TextBlock tb = new TextBlock() { Text = param.Description, ToolTip = param.Description };
            tb.Margin = new Thickness(5);
            UIUpdater.CloneStyle(TextBlockTemplate, tb);
            g.Children.Add(tb);
            Grid.SetColumn(tb, 0);

            FrameworkElement ui = null;

            if (param.ValueType.Name == typeof(bool).Name)
            {
                ui = new Chooser();
                ui.Margin = new Thickness(10);
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
                RegisterName(param.PropertyName, ui);
            }
            return g;
        }

        private Grid GenerateDeviceBar(DeviceTypes type, Param<string> param)
        {
            Grid g = new Grid();

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });

            TextBlock tb = new TextBlock() { Text = param.Description, ToolTip = param.Description };
            tb.Margin = new Thickness(5);
            UIUpdater.CloneStyle(TextBlockTemplate, tb);
            g.Children.Add(tb);
            Grid.SetColumn(tb, 0);

            ComboBox box = new ComboBox() { DefaultSelectIndex = 0 };
            box.TemplateButton = ComboBoxTemplate;
            ComboBoxTemplate.CloneStyleTo(box);
            box.Click += ((sender, e) =>
            {
                box.Items.Clear();
                var devs = DeviceDispatcher.GetDevice(type);
                foreach (var item in devs)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item.GetDeviceDescription(), Tag = item };
                    box.Items.Add(btn);
                }
            });
            RegisterName(param.PropertyName, box);
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
            SelectExp(ExpObjects.IndexOf(ExpType.SelectedItem.Tag as SequenceExpObject));
        }
    }
}
