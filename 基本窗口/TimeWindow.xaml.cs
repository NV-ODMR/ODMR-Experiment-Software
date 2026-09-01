using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;

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
