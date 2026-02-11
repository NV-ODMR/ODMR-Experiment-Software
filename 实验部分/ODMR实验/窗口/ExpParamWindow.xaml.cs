using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.实验部分.ODMR实验
{

    /// <summary>
    /// ExpParamWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpParamWindow : Window
    {
        ODMRExpObject ParentExp = null;

        DisplayPage ParentPage = null;

        private static Label LabelTemplate = new Label() { Name = "LabelTemplate", HorizontalContentAlignment = HorizontalAlignment.Center, Foreground = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };

        private static TextBlock TextBlockTemplate = new TextBlock() { Foreground = Brushes.White, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = 12 };

        private static ComboBox ComboBoxTemplate = new ComboBox()
        {
            Name = "ComboBoxTemplate",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D")),
            FontSize = 12,
            TextAreaRatio = 0.9,
            ImagePlace = ImagePlace.Right,
            IconMargin = new Thickness(3),
            Foreground = Brushes.White,
            PressedForeground = Brushes.White,
            MoveInForeground = Brushes.White,
            MoveInColor = Brushes.Gray,
            PressedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1B6BDD")),
            IconSource = new BitmapImage(new Uri("/图片资源/downArrow.png", UriKind.Relative))
        };

        private static TextBox TextBoxTemplate = new TextBox()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderBrush = null,
            CaretBrush = Brushes.White
        };

        bool Isinput = false;

        bool Isoutput = false;

        bool Isdevice = false;

        public ExpParamWindow(ODMRExpObject exp, DisplayPage parentpage, bool input, bool output, bool device)
        {
            InitializeComponent();

            WindowResizeHelper helper = new WindowResizeHelper();
            helper.RegisterCloseWindow(this, null, null, CloseBtn, 5, 30);

            Isinput = input;
            Isoutput = output;
            Isdevice = device;

            ParentExp = exp;
            ParentPage = parentpage;

            //加载参数
            if (input)
            {
                HashSet<string> GNames = exp.InputParams.Select(x => x.GroupName).ToHashSet();

                foreach (var item in GNames)
                {
                    //添加标签
                    ParamPannel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                    Label l = new Label() { Content = item };
                    UIUpdater.CloneStyle(LabelTemplate, l);
                    ParamPannel.Children.Add(l);
                    Grid.SetRow(l, ParamPannel.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(l, 2);

                    bool isfirst = true;
                    var plist = exp.InputParams.Where(x => x.GroupName == item);
                    foreach (var p in plist)
                    {
                        if (isfirst)
                            ParamPannel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                        Grid g = GenerateControlBar(p, this, true);
                        ParamPannel.Children.Add(g);
                        p.LoadToPage(new FrameworkElement[] { this }, false);
                        Grid.SetRow(g, ParamPannel.RowDefinitions.Count - 1);
                        Grid.SetColumn(g, isfirst ? 0 : 1);
                        isfirst = !isfirst;
                    }
                }
            }
            if (output)
            {
                HashSet<string> GNames = exp.OutputParams.Select(x => x.GroupName).ToHashSet();

                foreach (var item in GNames)
                {
                    //添加标签
                    ParamPannel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                    Label l = new Label() { Content = item };
                    UIUpdater.CloneStyle(LabelTemplate, l);
                    ParamPannel.Children.Add(l);
                    Grid.SetRow(l, ParamPannel.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(l, 2);

                    bool isfirst = true;
                    var plist = exp.OutputParams.Where(x => x.GroupName == item);
                    foreach (var p in plist)
                    {
                        if (isfirst)
                            ParamPannel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                        Grid g = GenerateControlBar(p, this, false);
                        ParamPannel.Children.Add(g);
                        p.LoadToPage(new FrameworkElement[] { this }, false);
                        Grid.SetRow(g, ParamPannel.RowDefinitions.Count - 1);
                        Grid.SetColumn(g, isfirst ? 0 : 1);
                        isfirst = !isfirst;
                    }
                }
            }
            if (device)
            {
                HashSet<string> GNames = exp.DeviceList.Select(x => x.Value.GroupName).ToHashSet();

                foreach (var item in GNames)
                {
                    //添加标签
                    ParamPannel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                    Label l = new Label() { Content = item };
                    UIUpdater.CloneStyle(LabelTemplate, l);
                    ParamPannel.Children.Add(l);
                    Grid.SetRow(l, ParamPannel.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(l, 2);

                    var plist = exp.DeviceList.Where(x => x.Value.GroupName == item);
                    foreach (var p in plist)
                    {
                        ParamPannel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });
                        Grid g = GenerateDeviceBar(p.Key, p.Value, this);
                        ParamPannel.Children.Add(g);
                        p.Value.LoadToPage(new FrameworkElement[] { this }, false);
                        Grid.SetRow(g, ParamPannel.RowDefinitions.Count - 1);
                        Grid.SetColumn(g, 0);
                        Grid.SetColumnSpan(g, 2);
                    }
                }
            }
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            if (!ParentExp.IsExpEnd)
            {
                MessageWindow.ShowTipWindow("实验正在进行中,无法设置参数", this);
                return;
            }
            //设置参数
            foreach (var item in ParamPannel.Children)
            {
                if (item is Grid)
                {
                    ParamB p = (item as Grid).Tag as ParamB;
                    p.ReadFromPage(new FrameworkElement[] { this }, false);
                    p.LoadToPage(new FrameworkElement[] { ParentPage }, false);
                }
            }
            Close();
        }



        public static Grid GenerateControlBar(ParamB param, FrameworkElement parent, bool IsInput)
        {
            Grid g = new Grid();
            g.Margin = new Thickness(5);

            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            g.Height = 35;

            TextBlock tb = new TextBlock() { Text = param.Description, ToolTip = param.Description };
            tb.Margin = new Thickness(5);
            tb.MinWidth = 80;
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
                t.MinWidth = 80;
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
                c.MinWidth = 80;

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
                parent.RegisterName(GetValidName(param.PropertyName), ui);
            }
            g.Tag = param;
            if (param.Helper != "") g.ToolTip = param.Helper;
            return g;
        }


        public Grid GenerateDeviceBar(DeviceTypes type, Param<string> param, FrameworkElement parent)
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
            parent.RegisterName(GetValidName(param.PropertyName), box);
            g.Children.Add(box);
            g.Tag = param;
            Grid.SetColumn(box, 1);
            if (param.Helper != "") g.ToolTip = param.Helper;
            return g;
        }

        public static string GetValidName(string name)
        {
            name = name.Replace("(", "");
            name = name.Replace(")", "");
            name = name.Replace("[", "");
            name = name.Replace("]", "");
            name = name.Replace("{", "");
            name = name.Replace("}", "");
            name = name.Replace(" ", "");
            return name;
        }
    }
}
