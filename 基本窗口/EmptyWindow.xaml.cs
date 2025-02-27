using CodeHelper;
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
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EmptyWindow : Window
    {
        PageBase ParentPage = null;

        public EmptyWindow(string wintitle, PageBase parent)
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, MinBtn, MaxBtn, null, 5, 40);
            Title = wintitle;
            title.Content = "     " + wintitle;
            ParentPage = parent;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            ParentPage.IsDisplayedInPage = true;
            Hide();
        }

    }
}
