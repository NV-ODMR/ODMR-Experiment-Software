using Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab
{
    public class UIUpdater
    {
        #region 控件模板
        public static DecoratedButton ButtonTemplate = new DecoratedButton()
        {
            FontSize = 10,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")),
            Foreground = Brushes.White,
            MoveInColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF424242")),
            PressedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF39393A")),
            MoveInForeground = Brushes.White,
            PressedForeground = Brushes.White,
        };

        public static Label LabelTemplate = new Label()
        {
            Background = Brushes.Transparent,
            FontSize = 10,
            Foreground = Brushes.White,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        public static TextBox TextBoxTemplate = new TextBox()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 10,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderBrush = null,
            CaretBrush = Brushes.White
        };

        public static ComboBox ComboBoxTemplate = new ComboBox()
        {
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

        public static TextBlock TextBlockTemplate = new TextBlock() { Foreground = Brushes.White, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = 10 };

        #endregion

        public static void SetDefaultTemplate(DecoratedButton btn)
        {
            ButtonTemplate.CloneStyleTo(btn);
        }

        public static void SetDefaultTemplate(Label label)
        {
            CloneStyle(LabelTemplate, label);
        }

        public static void SetDefaultTemplate(TextBox box)
        {
            CloneStyle(TextBoxTemplate, box);
        }
        public static void SetDefaultTemplate(TextBlock box)
        {
            CloneStyle(TextBlockTemplate, box);
        }
        public static void SetDefaultTemplate(ComboBox box)
        {
            ComboBoxTemplate.CloneStyleTo(box);
        }

        public static void SetUIControl(TextBlock ele, string value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ele.Text = value;
            });
        }

        public static void SetUIControl(TextBox ele, string value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ele.Text = value;
            });
        }

        public static void SetUIControl(Label ele, string value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ele.Content = value;
            });
        }

        public static void SetUIControl(Chooser ele, bool value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ele.IsSelected = value;
            });
        }

        public static void SetUIControl(ComboBox ele, int ind)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ele.Select(ind);
            });
        }

        #region 复制样式
        public static void CloneStyle(TextBlock source, TextBlock target)
        {
            target.FontSize = source.FontSize;
            target.FontWeight = source.FontWeight;
            target.TextAlignment = source.TextAlignment;
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
            target.Foreground = source.Foreground;
            target.TextWrapping = source.TextWrapping;
        }

        public static void CloneStyle(TextBox source, TextBox target)
        {
            target.FontSize = source.FontSize;
            target.FontWeight = source.FontWeight;
            target.TextAlignment = source.TextAlignment;
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
            target.HorizontalContentAlignment = source.HorizontalContentAlignment;
            target.VerticalContentAlignment = source.VerticalContentAlignment;
            target.BorderThickness = source.BorderThickness;
            target.BorderBrush = source.BorderBrush;
            target.Background = source.Background;
            target.CaretBrush = source.CaretBrush;
            target.Foreground = source.Foreground;
            target.SelectionBrush = source.SelectionBrush;
        }

        public static void CloneStyle(Label source, Label target)
        {
            target.FontSize = source.FontSize;
            target.FontWeight = source.FontWeight;
            target.Foreground = source.Foreground;
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
            target.Background = source.Background;
            target.HorizontalContentAlignment = source.HorizontalContentAlignment;
            target.VerticalContentAlignment = source.VerticalContentAlignment;
            target.BorderBrush = source.BorderBrush;
            target.BorderThickness = source.BorderThickness;
        }
        #endregion
    }
}
