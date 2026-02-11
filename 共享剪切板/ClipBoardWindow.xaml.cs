using CodeHelper;
using Controls.Windows;
using ODMR_Lab.Windows;
using ODMR_Lab.数据记录;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
            Title = "共享剪切板";
            title.Content = "     " + "共享剪切板";
        }

        private string CurrentFile = "";

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            MessageWindow w = new MessageWindow("操作", "正在操作文件...", MessageBoxButton.OK, false, false);
            w.Owner = this;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        StringCollection file = null;
                        object data = null;
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = false;
                            w.Show();
                        });
                        file = Clipboard.GetFileDropList();
                        //远程数据
                        data = Clipboard.GetData("remoteCustomData");
                        if (file.Count == 0 && data == null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                IsEnabled = true;
                                w.Close();
                            });
                            return;
                        }
                        if (file.Count != 0)
                        {
                            //本地文件传入
                            string lastpath = file[file.Count - 1];
                            Dispatcher.Invoke(() =>
                            {
                                FileViewer.SetDisplayFile(lastpath);
                            });
                            CurrentFile = lastpath;
                        }
                        if (data != null)
                        {
                            //远程文件传入
                            var input = data as List<object>;
                            ConvertToFile(input);
                            string filepath = Path.Combine(Environment.CurrentDirectory, "TempPasteFile", (string)input[0]);
                            Dispatcher.Invoke(() =>
                            {
                                FileViewer.SetDisplayFile(filepath);
                            });
                            CurrentFile = filepath;
                        }

                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                        });
                    }
                    catch (Exception)
                    {
                        CurrentFile = "";
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                        });
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            //粘贴图片
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = false;
                            w.Show();
                            if (CurrentFile == "") return;
                        });
                        Clipboard.Clear();
                        Clipboard.SetFileDropList(new StringCollection() { CurrentFile });
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                        });
                    }
                    catch (Exception)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                        });
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
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
            MessageWindow w = new MessageWindow("操作", "正在操作文件...", MessageBoxButton.OK, false, false);
            w.Owner = this;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Thread t = new Thread(() =>
            {
                if (CurrentFile != "")
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = false;
                            w.Show();
                        });
                        Clipboard.Clear();
                        Clipboard.SetFileDropList(new StringCollection() { CurrentFile });
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                            TimeWindow win = new TimeWindow();
                            win.Owner = this;
                            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            win.ShowWindow("文件已复制");
                        });
                    }
                    catch (Exception)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                        });
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void CopyRemoteData(object sender, RoutedEventArgs e)
        {
            MessageWindow w = new MessageWindow("操作", "正在操作文件...", MessageBoxButton.OK, false, false);
            w.Owner = this;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Thread t = new Thread(() =>
            {
                if (CurrentFile != "")
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = false;
                            w.Show();
                        });
                        ConvertToByteAndSetToClipboard(CurrentFile);
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                            TimeWindow win = new TimeWindow();
                            win.Owner = this;
                            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            win.ShowWindow("文件已复制");
                        });
                    }
                    catch (Exception)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            IsEnabled = true;
                            w.Close();
                        });
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
