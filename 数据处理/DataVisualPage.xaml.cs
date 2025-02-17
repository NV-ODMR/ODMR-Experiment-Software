using CodeHelper;
using Controls;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.源表;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.位移台部分;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using ODMR_Lab.实验部分.场效应器件测量;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using ContextMenu = Controls.ContextMenu;
using Label = System.Windows.Controls.Label;
using Path = System.IO.Path;
using ODMR_Lab.IO操作;
using OpenCvSharp;
using Window = System.Windows.Window;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using Controls.Windows;

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DataVisualPage : PageBase
    {
        public override string PageName { get; set; } = "数据可视化";

        public List<DataVisualSource> Source { get; set; } = new List<DataVisualSource>();


        public DataVisualPage()
        {
            InitializeComponent();
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        private void SelectPanel(object sender, RoutedEventArgs e)
        {
            string name = (sender as DecoratedButton).Name;
            if (sender == BtnPlot1D)
            {
                Plot1D.Visibility = Visibility.Visible;
                Plot2D.Visibility = Visibility.Hidden;
                BtnPlot1D.KeepPressed = true;
                BtnPlot2D.KeepPressed = false;
                return;
            }
            if (sender == BtnPlot2D)
            {
                Plot1D.Visibility = Visibility.Hidden;
                Plot2D.Visibility = Visibility.Visible;
                BtnPlot1D.KeepPressed = false;
                BtnPlot2D.KeepPressed = true;
                return;
            }
        }

        #region 文件操作
        List<string> OpenedFiles { get; set; } = new List<string>();
        /// <summary>
        /// 打开指定文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "实验文件(*.userdat)|*.userdat";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadFiles(dlg.FileNames.ToList());
            }
        }

        /// <summary>
        /// 导入指定路径文件
        /// </summary>
        /// <param name="paths"></param>
        private void LoadFiles(List<string> paths)
        {
            DataVisualSource sou = null;
            int ind = -1;
            for (int i = 0; i < paths.Count(); i++)
            {
                try
                {
                    if (OpenedFiles.Contains(paths[i]))
                    {
                        sou = Source[OpenedFiles.IndexOf(paths[i])];
                        ind = OpenedFiles.IndexOf(paths[i]);
                    }
                    else
                    {
                        sou = DataVisualSource.LoadFromFile(paths[i]);
                        ind = OpenedFiles.Count;
                        Source.Add(sou);
                        OpenedFiles.Add(paths[i]);
                        UpdateFilesPanel();
                    }
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("打开文件失败,原因:\n" + ex.Message, Window.GetWindow(this));
                }
            }
            try
            {
                FilesPanel.UpdateLayout();
                SelectFileBtn(FilesPanel.Children[ind] as DecoratedButton);
                UpdateFromSource(sou);
            }
            catch (Exception ex) { return; }
        }

        /// <summary>
        /// 文件另存为
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFileAs(object sender, RoutedEventArgs e)
        {
            UserCustomExpObject obj = new UserCustomExpObject();
            if (GetSelectedIndex() == -1)
            {
                return;
            }
            obj.DataSource = Source[GetSelectedIndex()];

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "自定义文件(*.userdat)|*.userdat";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                //检查文件是否打开
                foreach (var item in OpenedFiles)
                {
                    if (saveFileDialog.FileName == item)
                    {
                        MessageWindow.ShowTipWindow("文件" + item + "已打开，无法进行覆盖", Window.GetWindow(this));
                        return;
                    }
                }

                try
                {
                    obj.WriteToFile(Path.GetDirectoryName(saveFileDialog.FileName), Path.GetFileName(saveFileDialog.FileName));

                    TimeWindow win = new TimeWindow();
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("文件保存失败,原因：" + ex.Message, Window.GetWindow(this));
                }
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFile(object sender, RoutedEventArgs e)
        {
            int ind = GetSelectedIndex();
            if (ind == -1) return;
            if (File.Exists(OpenedFiles[ind]) == false)
            {
                MessageWindow.ShowTipWindow("没有找到文件,文件可能已经被移动或删除", Window.GetWindow(this));
            }
            if (ExperimentObject<ExpParamBase, ConfigBase>.GetExpType(OpenedFiles[ind]) != ExperimentFileTypes.自定义数据)
            {
                MessageWindow.ShowTipWindow("原始实验数据仅支持读取，无法进行编辑操作，请先将文件另存为自定义数据后再进行操作", Window.GetWindow(this));
                return;
            }
            try
            {
                UserCustomExpObject obj = new UserCustomExpObject();
                obj.DataSource = Source[ind];
                obj.WriteToFile(Path.GetDirectoryName(OpenedFiles[ind]), Path.GetFileName(OpenedFiles[ind]));

                TimeWindow win = new TimeWindow();
                win.Owner = Window.GetWindow(this);
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.ShowWindow("文件已保存");
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("文件保存失败,原因：" + ex.Message, Window.GetWindow(this));
            }
        }

        private int GetSelectedIndex()
        {
            foreach (var item in FilesPanel.Children)
            {
                if ((item as DecoratedButton).KeepPressed)
                {
                    return FilesPanel.Children.IndexOf(item as DecoratedButton);
                }
            }
            return -1;
        }
        #endregion

        /// <summary>
        /// 更新信息面板
        /// </summary>
        private void UpdateInformationPanel(DataVisualSource source)
        {
            ExpInformation.ClearItems();
            foreach (var item in source.Params)
            {
                ExpInformation.AddItem(null, item.Key + " : " + item.Value);
            }
        }

        private void UpdateFilesPanel()
        {
            FilesPanel.Children.Clear();
            foreach (var item in OpenedFiles)
            {
                DecoratedButton btn = new DecoratedButton();
                RawDataTemplateBtn.CloneStyleTo(btn);
                btn.TextAreaRatio = RawDataTemplateBtn.TextAreaRatio;
                if (ExperimentObject<ExpParamBase, ConfigBase>.GetExpType(item) == ExperimentFileTypes.自定义数据)
                {
                    btn.IconSource = CustomDataTemplateBtn.IconSource;
                }
                else
                {
                    btn.IconSource = RawDataTemplateBtn.IconSource;
                }
                btn.Height = 50;
                btn.ToolTip = System.IO.Path.GetFileNameWithoutExtension(item);
                btn.Foreground = Brushes.White;
                btn.Text = System.IO.Path.GetFileNameWithoutExtension(item);
                btn.Tag = Source[OpenedFiles.IndexOf(item)];
                btn.Click += SelectFile;

                ContextMenu menu = new ContextMenu();
                DecoratedButton it = new DecoratedButton();
                BtnPlot1D.CloneStyleTo(it);
                it.Text = "关闭文件";
                it.Tag = Source[OpenedFiles.IndexOf(item)];
                it.Click += CloseFile;
                menu.Items.Add(it);
                menu.ApplyToControl(btn);
                FilesPanel.Children.Add(btn);
            }
        }


        /// <summary>
        /// 选中当前文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SelectFile(object sender, RoutedEventArgs e)
        {
            DataVisualSource source = (sender as DecoratedButton).Tag as DataVisualSource;
            UpdateFromSource(source);
            SelectFileBtn(sender as DecoratedButton);
        }

        private void SelectFileBtn(DecoratedButton btn)
        {
            foreach (var item in FilesPanel.Children)
            {
                (item as DecoratedButton).KeepPressed = false;
            }
            btn.KeepPressed = true;
        }

        #region 文件处理

        /// <summary>
        /// 关闭当前文件
        /// </summary>
        private void CloseFile(object sender, RoutedEventArgs e)
        {
            DataVisualSource source = (sender as DecoratedButton).Tag as DataVisualSource;
            if (OpenedFiles[Source.IndexOf(source)] == CurrentFile.Content.ToString().Replace("当前文件:", ""))
            {
                UpdateFromSource(new DataVisualSource());
            }
            OpenedFiles.RemoveAt(Source.IndexOf(source));
            Source.Remove(source);
            UpdateFilesPanel();
        }

        /// <summary>
        /// 外接拖动进入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void File_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        /// <summary>
        /// 外界拖动移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void File_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        /// <summary>
        /// 外界拖动松开(加载发票文件)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void File_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) == null) return;
            var obj = (e.Data.GetData(DataFormats.FileDrop) as string[]).ToList();
            LoadFiles(obj);
            e.Handled = true;
        }
        #endregion

        public void UpdateFromSource(DataVisualSource source)
        {
            if (Source == null) return;

            //设置当前文件
            int ind = Source.IndexOf(source);
            if (ind == -1)
            {
                CurrentFile.Content = "无打开文件";
            }
            else
            {
                CurrentFile.Content = "当前文件:" + OpenedFiles[ind];
            }

            UpdateInformationPanel(source);

            //更新图表部分
            Plot1D.DataSource.Clear(false);
            Plot1D.DataSource.AddRange(source.ChartDataSource1D);
            Plot1D.UpdateChartAndDataFlow(true);
            if (source.ChartDataSource1D.Count != 0)
            {
                Plot1D.SelectGroup(source.ChartDataSource1D[0].GroupName);
            }
        }

        /// <summary>
        /// 打开数据处理窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenDataProcessWindow(object sender, RoutedEventArgs e)
        {
            int ind = GetSelectedIndex();
            if (ind == -1) return;
            DataProcessWindow win = new DataProcessWindow(this);
            win.ParentDataSource = Source[ind];
            win.ShowDialog();
            UpdateFromSource(Source[ind]);
        }
    }
}
