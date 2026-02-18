using CodeHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataViewingWindow : Window
    {
        public DataViewingWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
            Title = "数据预览";
            title.Content = "     " + "数据预览";
        }

        public new void ShowWithExpFile(string filepath)
        {
            if (!File.Exists(filepath)) return;
            try
            {
                ExpPage.LoadFiles(new List<string>() { filepath });
                Show();
            }
            catch (Exception) { }
        }

        // 导入 Windows API 中的设置任务栏应用程序 ID 的方法
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public new void Show()
        {
            var dhelper = new WindowInteropHelper(this);
            SetCurrentProcessExplicitAppUserModelID("DataVisualize");
            base.Show();
        }
    }
}
