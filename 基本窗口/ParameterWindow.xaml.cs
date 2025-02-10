using Controls;
using Controls.Windows;
using HardWares.端口基类部分;
using ODMR_Lab.Windows;
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
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.基本窗口
{
    /// <summary>
    /// ParameterWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterWindow : Window
    {
        List<Parameter> ps = new List<Parameter>();

        public ParameterWindow(List<Parameter> param)
        {
            ps = new List<Parameter>();
            InitializeComponent();

            //添加参数
            foreach (var item in param)
            {
                if (!item.IsStatic) continue;
                var result = item.ReadValue();
                Grid g = CreateParamLine(item, result);
                paramsPanel.Children.Add(g);
            }
        }

        public Grid CreateParamLine(Parameter paramobj, object paramvalue)
        {
            Grid grid = new Grid();
            grid.Tag = paramobj;
            grid.Height = 40;
            grid.Margin = new Thickness(30, 10, 30, 10);
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Label l = new Label();
            l.Foreground = Brushes.White;
            l.Content = paramobj.Description;
            l.ToolTip = paramobj.Description;
            l.HorizontalContentAlignment = HorizontalAlignment.Left;
            l.VerticalContentAlignment = VerticalAlignment.Center;
            l.HorizontalAlignment = HorizontalAlignment.Stretch;
            l.VerticalAlignment = VerticalAlignment.Stretch;
            grid.Children.Add(l);
            Grid.SetColumn(l, 0);
            if (paramvalue is Enum)
            {
                ComboBox comboBox = new ComboBox();
                ButtonTemplate.CloneStyleTo(comboBox);
                comboBox.TextAreaRatio = ButtonTemplate.TextAreaRatio;
                comboBox.IconSource = ButtonTemplate.IconSource;
                comboBox.ImagePlace = ButtonTemplate.ImagePlace;
                comboBox.IconMargin = ButtonTemplate.IconMargin;
                comboBox.PanelWidth = 200;
                comboBox.FontSize = 15;
                foreach (string item in Enum.GetNames(paramvalue.GetType()))
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item };
                    ButtonTemplate.CloneStyleTo(btn);
                    comboBox.Items.Add(btn);
                }
                comboBox.Select(Enum.GetName(paramvalue.GetType(), paramvalue));
                grid.Children.Add(comboBox);
                Grid.SetColumn(comboBox, 1);
                if (paramobj.IsReadOnly)
                {
                    comboBox.IsHitTestVisible = false;
                }
                return grid;
            }
            if (paramvalue is bool)
            {
                Chooser chooser = new Chooser();
                chooser.Width = 60;
                chooser.Height = 30;
                chooser.HorizontalAlignment = HorizontalAlignment.Center;
                chooser.VerticalAlignment = VerticalAlignment.Center;
                chooser.HasAnimation = false;
                chooser.IsSelected = (bool)paramvalue;
                grid.Children.Add(chooser);
                Grid.SetColumn(chooser, 1);
                if (paramobj.IsReadOnly)
                {
                    chooser.IsHitTestVisible = false;
                }
                return grid;
            }
            else
            {
                TextBox font = new TextBox();
                UIUpdater.CloneStyle(TextTemplate, font);
                font.Text = paramvalue.ToString();
                grid.Children.Add(font);
                Grid.SetColumn(font, 1);
                if (paramobj.IsReadOnly)
                {
                    font.IsReadOnly = true;
                }
                return grid;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).Y < 30)
            {
                DragMove();
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 应用参数更改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Apply(object sender, RoutedEventArgs e)
        {
            string result = "";
            foreach (var item in paramsPanel.Children)
            {
                Grid g = item as Grid;
                Parameter p = g.Tag as Parameter;
                if (p.IsReadOnly) continue;
                try
                {
                    if (g.Children[1] is ComboBox)
                    {
                        p.WriteValue(Enum.Parse(p.ParamType, (g.Children[1] as ComboBox).SelectedItem.Text));
                        //更新参数
                        (g.Children[1] as ComboBox).Select(Enum.GetName(p.ParamType, (int)p.ReadValue()));
                        continue;
                    }
                    if (g.Children[1] is Chooser)
                    {
                        p.WriteValue((g.Children[1] as Chooser).IsSelected);
                        (g.Children[1] as Chooser).IsSelected = p.ReadValue();
                        continue;
                    }
                    if (g.Children[1] is FontChangeText)
                    {
                        p.WriteValue(Convert.ChangeType((g.Children[1] as FontChangeText).InnerTextBox.Text, p.ParamType));
                        (g.Children[1] as FontChangeText).InnerTextBox.Text = p.ReadValue().ToString();
                    }
                    if (g.Children[1] is TextBox)
                    {
                        p.WriteValue(Convert.ChangeType((g.Children[1] as TextBox).Text, p.ParamType));
                        (g.Children[1] as TextBox).Text = p.ReadValue().ToString();
                    }
                }
                catch (Exception exc)
                {
                    result += "参数" + p.Description + "未成功设置,错误原因" + exc.Message + "\n";
                }
            }
            if (result == "") result = "参数设置成功";
            MessageWindow.ShowTipWindow(result, this);
        }
    }
}
