using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.Windows;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Linq;
using ComboBox = Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ODMR_Lab.实验部分.ODMR实验
{

    /// <summary>
    /// ExpParamWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpFileWindow : Window
    {
        public ExpFileWindow()
        {
            InitializeComponent();

            WindowResizeHelper helper = new WindowResizeHelper();
            helper.RegisterHideWindow(this, null, null, CloseBtn, 5, 30);
        }

        public string RootFolder = "";

        private string ODMRGroupName = "";

        private string ODMRExpName = "";

        private void ExpSearchBar_ResultSelected(KeyValuePair<string, object> obj)
        {
            ODMRGroupName = ((List<string>)obj.Value)[0];
            ODMRExpName = ((List<string>)obj.Value)[1];
            ExpType.Text = ODMRGroupName + " " + ODMRExpName;
            UpdateFiles();
        }

        private void CleanStartDate(object sender, RoutedEventArgs e)
        {
            StartTime.ChoosedTime = null;
            UpdateFiles();
        }

        private void CleanEndDate(object sender, RoutedEventArgs e)
        {
            EndTime.ChoosedTime = null;
            UpdateFiles();
        }

        private Grid CreateFileViewingBar(string filepath)
        {
            Grid g = new Grid();
            MouseColorHelper hel = new MouseColorHelper(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838"))
                , new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D"))
                , new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0088D9")), true, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0088D9")));
            ControlEventHelper chel = new ControlEventHelper(g);
            //单击高亮文件
            chel.Click += (s, e) =>
            {
                try
                {
                    foreach (var item in FilesPanel.Children)
                    {
                        ((item as Grid).Tag as MouseColorHelper).KeepPressed = false;
                    }
                    hel.KeepPressed = true;
                    g.Focus();
                }
                catch (Exception)
                {
                }
            };
            //双击打开实验文件
            chel.MouseDoubleClick += (s, e) =>
            {
                MainWindow.DataWindow.WindowState = WindowState.Normal;
                MainWindow.DataWindow.Activate();
                MainWindow.DataWindow.ShowWithExpFile(filepath);
            };
            hel.RegistateTarget(g);

            g.PreviewKeyDown += (s, e) =>
            {
                //复制
                if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    try
                    {
                        Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection() { filepath });
                        TimeWindow win = new TimeWindow();
                        win.Owner = this;
                        win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        win.ShowWindow("文件已复制");
                    }
                    catch (Exception)
                    {
                    }
                }
            };

            g.Height = 50;
            g.Focusable = true;
            TextBlock b = new TextBlock();
            UIUpdater.CloneStyle(TextblockTemplate, b);
            b.Text = System.IO.Path.GetFileNameWithoutExtension(filepath);
            g.Children.Add(b);
            g.HorizontalAlignment = HorizontalAlignment.Stretch;
            g.Tag = hel;
            return g;
        }

        private void UpdateFiles()
        {
            try
            {
                string root = RootFolder;
                string path = System.IO.Path.Combine(root, ODMRGroupName, ODMRExpName);
                FilesPanel.Children.Clear();
                if (!Directory.Exists(path))
                {
                    return;
                }
                else
                {
                    var files = Directory.GetFiles(path);
                    DateTime? start = StartTime.ChoosedTime;
                    DateTime? end = EndTime.ChoosedTime;
                    List<KeyValuePair<string, DateTime>> filepairs = new List<KeyValuePair<string, DateTime>>();
                    if (TitleInput.Text != "")
                        files = SearchHelper.GetFuzzySearchResult(TitleInput.Text, files.Select(x => new KeyValuePair<string, object>(x, null)).ToList(), 0.8).Select((x) => x.Key).ToArray();
                    foreach (var file in files)
                    {
                        try
                        {
                            ODMRExpObject.GetExpTime(file, out DateTime starttime, out DateTime endtime);
                            if (start != null)
                            {
                                if (endtime < start) continue;
                            }
                            if (end != null)
                            {
                                if (endtime > end) continue;
                            }
                            filepairs.Add(new KeyValuePair<string, DateTime>(file, endtime));
                        }
                        catch (Exception)
                        {
                        }
                    }
                    filepairs.Sort((a, b) => a.Value.CompareTo(b.Value));
                    foreach (var item in filepairs)
                    {
                        FilesPanel.Children.Add(CreateFileViewingBar(item.Key));
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        private void TimeChoosedEvent(object sender, RoutedEventArgs e)
        {
            UpdateFiles();
        }

        private void TitleInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateFiles();
            }
        }

        private void SearchTitle(object sender, RoutedEventArgs e)
        {
            UpdateFiles();
        }
    }
}
