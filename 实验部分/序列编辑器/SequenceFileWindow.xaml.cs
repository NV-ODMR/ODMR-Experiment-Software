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

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// SequenceFileWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SequenceFileWindow : Window
    {
        public SequenceFileWindow(List<KeyValuePair<string, string>> FileInfos)
        {
            InitializeComponent();
            FilesPanel.ClearItems();
            foreach (var item in FileInfos)
            {
                FilesPanel.AddItem(item.Value, item.Key);
            }
            WindowResizeHelper helper = new WindowResizeHelper();
            helper.RegisterCloseWindow(this, null, null, CloseBtn, 0, 30);
            helper.BeforeClose += BeforeClose;
        }

        string selectedfile = "";

        private void BeforeClose(object sender, RoutedEventArgs e)
        {
            selectedfile = "";
        }

        public new string ShowDialog()
        {
            base.ShowDialog();
            return selectedfile;
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            if (FilesPanel.GetSelectedTag() == null)
            {
                selectedfile = "";
                Close();
                return;
            }
            selectedfile = FilesPanel.GetSelectedTag() as string;
            Close();
        }
    }
}
