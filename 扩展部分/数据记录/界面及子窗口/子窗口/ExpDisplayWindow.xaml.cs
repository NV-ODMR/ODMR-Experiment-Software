using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.Windows;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.相机_翻转镜;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpDisplayWindow : Window
    {
        public ExpDisplayWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, MinBtn, MaxBtn, null, 5, 40);
            Title = "实验数据预览";
            title.Content = "     " + "实验数据预览";
        }

        public new void ShowWithExpFile(string filepath)
        {
            if (!File.Exists(filepath)) return;
            ExpDataPage.LoadFiles(new List<string>() { filepath });
            Show();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
