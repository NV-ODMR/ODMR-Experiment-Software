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
    public partial class SubExpWindow : Window
    {
        WindowResizeHelper h = null;
        public SubExpWindow(string title, bool ShowClose = false)
        {
            InitializeComponent();
            h = new WindowResizeHelper();
            h.RegisterHideWindow(this, MinimizeBtn, MaximizeBtn, CloseBtn, 6, 30);
            var page = new DisplayPage(false);
            page.RawControlsStates = new List<KeyValuePair<FrameworkElement, 实验类.RunningBehaviours>>()
            {
                new KeyValuePair<FrameworkElement, 实验类.RunningBehaviours>(CloseBtn,实验类.RunningBehaviours.DisableWhenRunning),
            };
            SubExpContent.Children.Add(page);
            Title = title;
            TitleWindow.Content = title;
            if (ShowClose)
            {
                CloseColumn.Width = new GridLength(50);
            }
            else
            {
                CloseColumn.Width = new GridLength(0);
            }
        }

        public void Show(ODMRExpObject subexp)
        {
            var page = SubExpContent.Children[0] as DisplayPage;
            page.ExpObjects.Clear();
            page.ExpObjects.Add(subexp);
            page.SelectExp(0);
            subexp.ParentPage = SubExpContent.Children[0] as DisplayPage;
            Show();
        }

        public void EndUse()
        {
            Hide();
        }
    }
}
