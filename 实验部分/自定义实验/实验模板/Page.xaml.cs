using CodeHelper;
using Controls;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Xml.Linq;
using Path = System.IO.Path;
using MathNet.Numerics.Distributions;
using System.Diagnostics.Contracts;
using MathNet.Numerics;
using ODMR_Lab.Python.LbviewHandler;
using System.Windows.Forms;
using ContextMenu = Controls.ContextMenu;
using ODMR_Lab.实验部分.样品定位;
using Label = System.Windows.Controls.Label;
using ODMR_Lab.基本控件;
using Clipboard = System.Windows.Clipboard;
using Controls.Windows;
using ODMR_Lab.实验部分.自定义实验;
using System.Reflection;
using ODMR_Lab.IO操作;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = Controls.ComboBox;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.自定义实验
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        /// <summary>
        /// 
        /// </summary>
        public override string PageName { get; set; } = "自定义实验";

        public override void UpdateParam()
        {
        }

        /// <summary>
        /// 实验对象
        /// </summary>
        public CustomExpObject ExpObject { get; set; } = null;

        public DisplayPage(CustomExpObject expObject)
        {
            InitializeComponent();
            //加载参数
            ExpObject = expObject;
            AddControlToUI();

            ExpObject.StartButton = StartBtn;
            ExpObject.ResumeButton = ResumeBtn;
            ExpObject.StopButton = StopBtn;

            ExpObject.ControlStates.Add(new KeyValuePair<FrameworkElement, 实验类.RunningBehaviours>(InputPanel, 实验类.RunningBehaviours.DisableWhenRunning));
            ExpObject.ControlStates.Add(new KeyValuePair<FrameworkElement, 实验类.RunningBehaviours>(DevicePanel, 实验类.RunningBehaviours.DisableWhenRunning));
        }

        private void AddControlToUI()
        {
            //加载配置参数
            if (ExpObject == null) return;
            PropertyInfo[] infos = ExpObject.GetType().GetProperties();

            //获取总行数和总列数
            int maxrow = infos.Select(x => ((ParamB)x.GetValue(ExpObject)).RowIndex).Max();
            InputPanel.RowDefinitions.Clear();
            for (int i = 0; i < maxrow; i++)
            {
                InputPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                List<PropertyInfo> columninfos = infos.Where(x => ((ParamB)x.GetValue(ExpObject)).RowIndex == i).ToList();
                Grid g = new Grid();
                for (int j = 0; j < columninfos.Count; j++)
                {
                    ParamB pb = (ParamB)columninfos[j].GetValue(ExpObject);
                    g.ColumnDefinitions.Add(new ColumnDefinition() { Width = pb.RowLength });
                    Grid gg = GenerateControlBar(pb);
                    g.Children.Add(gg);
                    SetColumn(gg, j);
                }
            }

            //设备列表
            for (int i = 0; i < ExpObject.Config.DeviceNameParams.Count; i++)
            {
                DevicePanel.RowDefinitions.Clear();
                DevicePanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                Grid g = GenerateDeviceBar(ExpObject.Config.DeviceNameParams.ElementAt(i).Key);
                DevicePanel.Children.Add(g);
                SetRow(g, i);
            }
        }

        private Grid GenerateControlBar(ParamB param)
        {
            Grid g = new Grid();

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });

            TextBlock tb = new TextBlock() { Text = param.Description };
            tb.Margin = new Thickness(5);
            UIUpdater.CloneStyle(TextBlockTemplate, tb);
            g.Children.Add(tb);
            Grid.SetColumn(tb, 0);

            if (param.ValueType.Name == typeof(bool).Name)
            {
                Chooser c = new Chooser();
                c.Margin = new Thickness(10);
                g.Children.Add(c);
                Grid.SetColumn(c, 1);
                return g;
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
                g.Children.Add(c);
                Grid.SetColumn(c, 1);
                return g;
            }

            TextBox t = new TextBox();
            UIUpdater.CloneStyle(TextBoxTemplate, t);
            g.Children.Add(t);
            Grid.SetColumn(t, 1);

            return g;

        }

        private Grid GenerateDeviceBar(DeviceTypes type)
        {
            Grid g = new Grid();

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Pixel) });

            TextBlock tb = new TextBlock() { Text = Enum.GetName(type.GetType(), type) };
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
    }
}
