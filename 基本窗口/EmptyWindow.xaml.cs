using CodeHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, PinBtn, 5, 40);
            hel.BeforeHide += BeforeHide;
            Title = wintitle;
            title.Content = "     " + wintitle;
            ParentPage = parent;
        }

        private void BeforeHide(object sender, RoutedEventArgs e)
        {
            ParentPage.IsDisplayedInPage = true;
        }

    }
}
