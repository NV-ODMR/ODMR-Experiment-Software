using CodeHelper;
using System.Collections.Generic;
using System.Windows;

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
            helper.RegisterCloseWindow(this, null, null, CloseBtn, null, 4, 30);
            helper.BeforeClose += BeforeClose;
        }

        string selectedfile = "";

        private void BeforeClose(object sender, RoutedEventArgs e)
        {
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
