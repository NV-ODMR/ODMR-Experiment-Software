using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.Windows
{
    /// <summary>
    /// TimeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TimeWindow : Window
    {
        public TimeWindow()
        {
            InitializeComponent();
        }

        public void ShowWindow(string content)
        {
            Content.Content = content;
            Topmost = true;
            Show();
            Thread t = new Thread(() =>
            {
                Thread.Sleep(2000);
                Dispatcher.Invoke(() =>
                {
                    DoubleAnimation ani = new DoubleAnimation(100, 0, TimeSpan.FromSeconds(1), FillBehavior.Stop);
                    border.BeginAnimation(OpacityProperty, ani);
                    Close();
                });
            });
            t.Start();

        }
    }
}
