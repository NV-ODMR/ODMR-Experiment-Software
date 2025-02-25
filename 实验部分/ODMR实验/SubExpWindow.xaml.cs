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

        ODMRExpObject SubExp = null;

        public SubExpWindow(string title)
        {
            InitializeComponent();
            Title = title;
            TitleWindow.Content = title;
        }

        public void Show(ODMRExpObject subexp)
        {
            SubExp = subexp;
            var page = SubExpContent as DisplayPage;
            page.ParamsColumn.Width = new GridLength(0);
            page.ExpObjects.Add(subexp);
            page.SelectExp(0);
            Show();
        }
    }
}
