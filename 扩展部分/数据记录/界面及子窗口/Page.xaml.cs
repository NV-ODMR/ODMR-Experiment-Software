using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
using ODMR_Lab.扩展部分.数据记录.界面及子窗口;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;
using Path = System.IO.Path;

namespace ODMR_Lab.数据记录
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class ExtPage : PageBase
    {

        /// <summary>
        /// 用于拍照的相机
        /// </summary>
        public CameraInfo PhotoCamera = null;
        public override string PageName { get; set; } = "数据记录";

        public ExtPage()
        {
            InitializeComponent();
            (AssembleTab.ContentPanel as NoteControl).ParentPage = this;
        }

        public NoteAssemble CurrentNoteAssemble = null;

        private bool DeleteConfirm(string packagename)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要删除包" + packagename + "吗?", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
            { return true; }
            return false;
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
            FileObject obj = new FileObject();
            obj.Descriptions.Add("NoteAssemblePath", AssembleFolderPath.Text);
            obj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "PathData"));
        }

        public override void UpdateParam()
        {
            try
            {
                FileObject obj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "PathData.userdat"));
                AssembleFolderPath.Text = obj.Descriptions["NoteAssemblePath"];
            }
            catch (Exception)
            {
            }
        }

        private void GetAssembleFolderPath(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                AssembleFolderPath.Text = dlg.SelectedPath;
            }
        }

        private void ReLoad(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(AssembleFolderPath.Text))
            {
                Thread t = new Thread(() =>
                {
                    MessageWindow window = null;
                    Dispatcher.Invoke(() =>
                    {
                        window = new MessageWindow("正在加载", "正在从指定路径加载记录本...", MessageBoxButton.OK, false, false);
                        window.Owner = Window.GetWindow(this);
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        Window.GetWindow(this).IsEnabled = false;
                        window.Show();
                    });
                    try
                    {
                        string[] dirs = new string[0];
                        Dispatcher.Invoke(() =>
                        {
                            AssembleTab.ClearTabElement();
                            //加载数据集
                            dirs = Directory.GetDirectories(AssembleFolderPath.Text);
                        });
                        foreach (var dir in dirs)
                        {
                            if (!FileHelper.GetLastFolder(dir).Contains("assemble"))
                            {
                                continue;
                            }
                            NoteAssemble assem = null;
                            Dispatcher.Invoke(() =>
                            {
                                assem = new NoteAssemble("") { RootFolderPath = AssembleFolderPath.Text, FileIndex = int.Parse(FileHelper.GetLastFolder(dir).Replace("assemble", "")) };
                            });
                            assem.Load();
                            Dispatcher.Invoke(() =>
                            {
                                AssembleTab.AddTabElement(assem.Name, assem.Name, assem);
                            });
                        }
                        Dispatcher.Invoke(() =>
                        {
                            window.Close();
                            Window.GetWindow(this).IsEnabled = true;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            window.Close();
                            Window.GetWindow(this).IsEnabled = true;
                            MessageWindow.ShowTipWindow("加载未全部完成:" + ex.Message, Window.GetWindow(this));
                        });
                    }

                });
                t.Start();
            }
        }

        AddNoteWindow assembleWindow = new AddNoteWindow("新建记录本");

        private void AddAssemble(object sender, RoutedEventArgs e)
        {
            assembleWindow.NoteAssembleApplied -= NoteAssembleApplied;
            assembleWindow.NoteAssembleApplied += NoteAssembleApplied;
            assembleWindow.HideAction -= EnabePanel;
            assembleWindow.HideAction += EnabePanel;
            NoteAssemble assem = new NoteAssemble("") { RootFolderPath = AssembleFolderPath.Text };
            assem.FileIndex = assem.GenerateNewFileIndex();
            assembleWindow.LoadNoteAssemble(assem);
            IsEnabled = false;
            assembleWindow.Show();
        }

        AddNoteWindow assembleWindow1 = new AddNoteWindow("");

        private void ChangeAssemble(object sender, RoutedEventArgs e)
        {
            if (CurrentNoteAssemble == null) return;
            assembleWindow1.NoteAssembleApplied -= NoteAssembleApplied;
            assembleWindow1.NoteAssembleApplied += NoteAssembleApplied;
            assembleWindow1.HideAction -= EnabePanel;
            assembleWindow1.HideAction += EnabePanel;
            assembleWindow1.LoadNoteAssemble(CurrentNoteAssemble);
            IsEnabled = false;
            assembleWindow1.title.Content = "修改笔记本:" + CurrentNoteAssemble.Name;
            assembleWindow1.Show();
        }

        private void EnabePanel(object sender, RoutedEventArgs e)
        {
            IsEnabled = true;
        }

        private void NoteAssembleApplied(NoteAssemble assemble)
        {
            try
            {
                AssembleTab.ChangeTabElement(assembleWindow1.title.Content.ToString().Replace("修改笔记本:", ""), new Tuple<string, string, object>(assemble.Name, assemble.Name, assemble));
                //设置文件夹
                assemble.ChangeAssembleFile();
                MessageWindow.ShowTipWindow("笔记本已修改", Window.GetWindow(this));
                //更新所有笔记的显示
                (AssembleTab.ContentPanel as NoteControl).UpdateNotePanel();
            }
            catch (Exception)
            {
            }
        }

        private void AssembleTab_TabClicked(Tuple<string, string, object> obj)
        {
            CurrentNoteAssemble = obj.Item3 as NoteAssemble;
            (AssembleTab.ContentPanel as NoteControl).DisplayNoteAssemble(CurrentNoteAssemble);
        }

        SettingWindow setwindow = new SettingWindow("设置");
        /// <summary>
        /// 打开设置窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingWindow(object sender, RoutedEventArgs e)
        {
            setwindow.Load(PhotoCamera);
            setwindow.ShowDialog();
            PhotoCamera = setwindow.SelectedCamera;
        }
    }
}
