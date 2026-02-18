using CodeHelper;
using System.Windows;
using Controls;
using HardWares.Windows;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;
using ComboBox = Controls.ComboBox;
using ContextMenu = Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using Label = System.Windows.Controls.Label;
using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;
using System.Collections.Generic;
using ODMR_Lab.设备部分.相机_翻转镜_开关;
using System.Linq;

namespace ODMR_Lab.设备部分.相机_翻转镜
{
    /// <summary>
    /// CameraWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MarkerSetWindow : Window
    {
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


        MarkerBase marker = null;

        public MarkerSetWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, null, null, CloseBtn, 0, 40);
        }

        List<KeyValuePair<ParamB, bool>> param = null;

        public void LoadParams(List<KeyValuePair<ParamB, bool>> ps)
        {
            param = ps;

            ParamPanel.Children.Clear();

            foreach (KeyValuePair<ParamB, bool> paramB in ps)
            {
                Grid g = new Grid();
                g.Margin = new Thickness(5);
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                g.HorizontalAlignment = HorizontalAlignment.Stretch;
                Label l = new Label();
                UIUpdater.CloneStyle(labletemplate, l);
                l.Content = paramB.Key.Description;
                g.Children.Add(l);
                Grid.SetColumn(l, 0);
                var value = ParamB.GetUnknownParamValue(paramB.Key);

                UIElement ui = null;

                if (value is string || value is double || value is float || value is int)
                {
                    TextBox b = new TextBox() { Text = value.ToString() };
                    UIUpdater.CloneStyle(BoxTemplate, b);
                    b.IsReadOnly = paramB.Value;
                    ui = b;
                }

                if (value is bool)
                {
                    Chooser c = new Chooser() { IsSelected = value };
                    c.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    c.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    c.Height = 15;
                    c.Width = 30;
                    c.IsEnabled = !paramB.Value;
                    ui = c;
                }

                if (value is Enum)
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
                    foreach (var item in Enum.GetNames(paramB.Key.ValueType))
                    {
                        DecoratedButton b = new DecoratedButton() { Text = item };
                        c.Items.Add(b);
                    }
                    c.IsEnabled = !paramB.Value;
                    ui = c;
                }

                if (value is Color)
                {
                    ColorSelector c = new ColorSelector();
                    c.IsEnabled = !paramB.Value;
                    c.CurrentColor = value;
                    c.Width = 90;
                    c.Height = 90;
                    c.HorizontalAlignment = HorizontalAlignment.Center;
                    c.VerticalAlignment = VerticalAlignment.Center;
                    ui = c;
                }

                g.Children.Add(ui);
                Grid.SetColumn(ui, 1);

                ParamPanel.Children.Add(g);
            }
        }

        public void Show(MarkerBase mark, BitmapSource sourceimage)
        {
            marker = mark;
            List<KeyValuePair<ParamB, bool>> ps = new List<KeyValuePair<ParamB, bool>>();
            ps.Add(new KeyValuePair<ParamB, bool>(new Param<double>("位置X(像素)", marker.GetPixelLocation(sourceimage).X, ""), true));
            ps.Add(new KeyValuePair<ParamB, bool>(new Param<double>("位置Y(像素)", marker.GetPixelLocation(sourceimage).Y, ""), true));
            ps.Add(new KeyValuePair<ParamB, bool>(new Param<double>("宽(像素)", marker.GetPixelWidth(sourceimage), ""), true));
            ps.Add(new KeyValuePair<ParamB, bool>(new Param<double>("高(像素)", marker.GetPixelHeight(sourceimage), ""), true));
            ps.Add(new KeyValuePair<ParamB, bool>(new Param<Color>("颜色", marker.MarkerColor.Color, ""), false));
            ps.Add(new KeyValuePair<ParamB, bool>(new Param<double>("标记粗细", marker.MarkerStrokeThickness, ""), false));
            LoadParams(ps);
            Show();
        }

        public void ReadParams()
        {
            for (int i = 0; i < param.Count; i++)
            {
                try
                {
                    if ((param[i].Key.ValueType.Name == typeof(string).Name) || (param[i].Key.ValueType.Name == typeof(double).Name) || (param[i].Key.ValueType.Name == typeof(int).Name) || (param[i].Key.ValueType.Name == typeof(float).Name))
                    {
                        string text = ((ParamPanel.Children[i] as Grid).Children[1] as TextBox).Text;
                        ParamB.SetUnknownParamValue(param[i].Key, text);
                    }
                    if ((param[i].Key.ValueType.Name == typeof(Color).Name))
                    {
                        Color color = ((ParamPanel.Children[i] as Grid).Children[1] as ColorSelector).CurrentColor;
                        ParamB.SetUnknownParamValue(param[i].Key, color);
                    }
                    if ((param[i].Key.ValueType.BaseType.Name == typeof(Enum).Name))
                    {
                        string content = ((ParamPanel.Children[i] as Grid).Children[1] as ComboBox).SelectedItem.Text;
                        ParamB.SetUnknownParamValue(param[i].Key, content);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            ReadParams();
            Color color = (Color)param.Where((x) => x.Key.Description == "颜色").ElementAt(0).Key.RawValue;
            double thick = (double)param.Where((x) => x.Key.Description == "标记粗细").ElementAt(0).Key.RawValue;
            marker.MarkerColor = new SolidColorBrush(color);
            marker.MarkerStrokeThickness = thick;
            Hide();
        }
    }
}
