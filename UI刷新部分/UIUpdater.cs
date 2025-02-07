using Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab
{
    public class UIUpdater
    {
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
            target.Foreground=source.Foreground;
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
