using CodeHelper;
using ODMR_Lab.ODMR实验;
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

namespace ODMR_Lab.实验部分.ODMR实验.实验方法
{
    /// <summary>
    /// SubExpWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpNewWindow : Window
    {
        ODMRExpObject Exp = null;

        DisplayPage OriginParentPage = null;

        public ExpNewWindow(string title, ODMRExpObject exp, DisplayPage parentpage)
        {
            InitializeComponent();
            Exp = exp;
            OriginParentPage = parentpage;
            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterWindow(this, MinimizeBtn, MaximizeBtn, null, 6, 30);

            SubExpContent.Children.Add(new DisplayPage(false));
            Title = title;
            TitleWindow.Content = title;
        }

        public new void Show()
        {
            var page = SubExpContent.Children[0] as DisplayPage;
            page.ExpObjects.Add(Exp);
            Exp.DisConnectOuterControl();
            Exp.ParentPage = SubExpContent.Children[0] as DisplayPage;
            page.SelectExp(0);
            Exp.ParentPage.ExpPanel.Visibility = Visibility.Visible;
            Exp.ParentPage.ShowInwindowPanel.Visibility = Visibility.Hidden;
            base.Show();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Exp.NewDisplayWindow = null;
            Exp.ParentPage = OriginParentPage;
            if (Exp.ParentPage.CurrentExpObject == Exp)
            {
                Exp.ParentPage.SelectExp(Exp.ParentPage.ExpObjects.IndexOf(Exp));
                Exp.ParentPage.ShowInwindowPanel.Visibility = Visibility.Hidden;
                Exp.ParentPage.ExpPanel.Visibility = Visibility.Visible;
            }
            Close();
        }
    }
}
