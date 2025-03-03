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

namespace ODMR_Lab.基本窗口
{
    /// <summary>
    /// ParamInputWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ParamInputWindow : Window
    {
        public ParamInputWindow(string wintitle)
        {
            InitializeComponent();
            Title = wintitle;
            title.Content = "     " + wintitle;
        }

        private Dictionary<string, string> result = new Dictionary<string, string>();

        public Dictionary<string, string> ShowDialog(Dictionary<string, string> values)
        {
            PasContent.Children.Clear();
            foreach (var item in values)
            {
                PasContent.Children.Add(CreateBar(item.Key, item.Value));
            }
            ShowDialog();
            return result;
        }

        private Grid CreateBar(string content, string value)
        {
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.Height = 30;
            TextBlock b = new TextBlock() { Text = content };
            UIUpdater.CloneStyle(tbTemplate, b);
            g.Children.Add(b);
            Grid.SetColumn(b, 0);
            TextBox tb = new TextBox() { Text = content };
            UIUpdater.CloneStyle(TextTemplate, tb);
            g.Children.Add(tb);
            Grid.SetColumn(tb, 1);
            return g;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            result = new Dictionary<string, string>();
            Close();
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            result = new Dictionary<string, string>();
            foreach (var item in PasContent.Children)
            {
                string key = ((item as Grid).Children[0] as TextBlock).Text;
                string value = ((item as Grid).Children[1] as TextBox).Text;
                result.Add(key, value);
            }
            Close();
        }
    }
}
