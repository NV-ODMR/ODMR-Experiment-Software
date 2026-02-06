using CodeHelper;
using ODMR_Lab.数据记录;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;

namespace ODMR_Lab.共享剪切板
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ClipBoardWindow : Window
    {
        public ClipBoardWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, MinBtn, MaxBtn, null, 5, 40);
            Title = "共享剪切板";
            title.Content = "     " + "共享剪切板";
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private string CurrentFile = "";
        private bool IsLocalFile = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {
                    var file = Clipboard.GetFileDropList();
                    //远程数据
                    var data = Clipboard.GetData("remoteCustomData");
                    if (file == null && data == null)
                    {
                        return;
                    }
                    if (file != null)
                    {
                        //本地文件传入
                        string lastpath = file[file.Count - 1];
                        FileViewer.SetDisplayFile(lastpath);
                        CurrentFile = lastpath;
                        IsLocalFile = true;
                    }
                    if (data != null)
                    {
                        //远程文件传入
                        var input = data as List<object>;
                        ConvertToFile(input);
                        string filepath = Path.Combine(Environment.CurrentDirectory, "TempPasteFile", (string)input[0]);
                        FileViewer.SetDisplayFile(filepath);
                        CurrentFile = filepath;
                        IsLocalFile = false;
                    }
                }
                catch (Exception)
                {
                    CurrentFile = "";
                }
            }
            //粘贴图片
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {
                    if (CurrentFile == "") return;
                    if (IsLocalFile)
                    {
                        Clipboard.Clear();
                        Clipboard.SetFileDropList(new StringCollection() { CurrentFile });
                    }
                    else
                    {
                        ConvertToByteAndSetToClipboard(CurrentFile);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void ConvertToByteAndSetToClipboard(string filepath)
        {
            List<object> result = new List<object>();
            result.Add(System.IO.Path.GetFileName(filepath));
            result.Add(File.ReadAllBytes(filepath));
            Clipboard.Clear();
            Clipboard.SetData("remoteCustomData", result);
            return;
        }

        private void ConvertToFile(List<object> input)
        {
            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "TempPasteFile"));
            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "TempPasteFile", (string)input[0]), (byte[])input[1]);
        }

        private void ClearTempFolder()
        {
            Directory.Delete(Path.Combine(Environment.CurrentDirectory, "TempPasteFile"), true);
        }

        private void CopyLocalFile(object sender, RoutedEventArgs e)
        {
            if (CurrentFile != "")
            {
                Clipboard.Clear();
                Clipboard.SetFileDropList(new StringCollection() { CurrentFile });
            }
        }

        private void CopyRemoteData(object sender, RoutedEventArgs e)
        {
            if (CurrentFile != "")
            {
                ConvertToByteAndSetToClipboard(CurrentFile);
            }
        }
    }
}
