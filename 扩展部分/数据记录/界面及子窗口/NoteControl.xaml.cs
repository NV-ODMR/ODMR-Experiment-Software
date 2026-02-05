using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分.电源;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class NoteControl : Grid
    {
        public ExtPage ParentPage { get; set; } = null;

        AddNoteWindow addNotewindow = new AddNoteWindow("新建笔记");
        AddNoteWindow changeNotewindow = new AddNoteWindow("修改笔记");

        NoteUnitDisplayWindow addNoteUnitWindow = new NoteUnitDisplayWindow("新建条目");
        NoteUnitDisplayWindow changeNoteUnitWindow = new NoteUnitDisplayWindow("编辑条目");

        ExpDisplayWindow expDisplayWindow = new ExpDisplayWindow();

        public NoteControl()
        {
            InitializeComponent();

            addWindow.Owner = Window.GetWindow(this);
            addWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            addNotewindow.NoteApplied += AddNoteCommand;

            changeNotewindow.Owner = Window.GetWindow(this);
            changeNotewindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            changeNotewindow.NoteApplied += ChangeNoteCommand;

            expDisplayWindow.Owner = Window.GetWindow(this);
            expDisplayWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            addNoteUnitWindow.Owner = Window.GetWindow(this);
            addNoteUnitWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            addNoteUnitWindow.NoteUnitApplied += NoteUnitAppliedCommand;
            addNoteUnitWindow.NoteUnitUnApplied += NoteUnitUnAppliedCommand;
            addNoteUnitWindow.HideAction += new RoutedEventHandler((obj, e1) => { IsEnabled = true; });
            addNoteUnitWindow.ImageViewer.ParentExpDisplayWindow = expDisplayWindow;

            changeNoteUnitWindow.Owner = Window.GetWindow(this);
            changeNoteUnitWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            changeNoteUnitWindow.NoteUnitApplied += NoteUnitChangedCommand;
            changeNoteUnitWindow.HideAction += new RoutedEventHandler((obj, e1) => { IsEnabled = true; });
            changeNoteUnitWindow.ImageViewer.ParentExpDisplayWindow = expDisplayWindow;

        }

        NoteAssemble CurrentNoteAssemble = null;

        Note CurrentNote = null;

        public void DisplayNoteAssemble(NoteAssemble assemble)
        {
            CurrentNoteAssemble = assemble;
            UpdateNotePanel();
        }

        private void ShowNoteSetWindow(object sender, MouseButtonEventArgs e)
        {
            changeNotewindow.LoadNote((sender as NoteUnitBar).Tag as Note);
            changeNotewindow.Show();
        }

        private void NoteSelected(int arg1, object arg2)
        {
            //显示记录条目
            NoteUnitPanel.Children.Clear();
            var contents = (arg2 as Note).NoteContents;
            foreach (var item in contents)
            {
                NoteUnitBar bar = new NoteUnitBar();
                ControlEventHelper helper = new ControlEventHelper(bar);
                bar.Tag = item;

                //双击事件,打开条目详细内容
                helper.MouseDoubleClick += new MouseButtonEventHandler((sender, e) => { });

                bar.LoadNoteUnit(item);
                //右键删除事件
                bar.DeleteCommand += new RoutedEventHandler((sender, e) =>
                {
                    if (MessageWindow.ShowMessageBox("删除提示", "确定要删除此条目吗？此操作不可恢复", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                    {
                        //删除条目
                        (arg2 as Note).NoteContents.Remove(bar.Tag as NoteUnit);
                        (bar.Tag as NoteUnit).DeleteNoteUnitFromFile();
                    }
                });
                NoteUnitPanel.Children.Add(bar);
            }
        }

        /// <summary>
        /// 新建记录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNote(object sender, RoutedEventArgs e)
        {
            if (CurrentNoteAssemble == null) return;
            if (addNotewindow != null && addNotewindow.IsVisible)
            {
                addNotewindow.Show();
                return;
            }
            Note newnote = new Note(CurrentNoteAssemble, "") { ParentTags = CurrentNoteAssemble.GlobalTags.Select(x => x.GetOutlineCopy()).ToList() };
            newnote.FileIndex = newnote.GenerateNewFileIndex();
            addNotewindow.LoadNote(newnote);
            addNotewindow.Show();
        }

        private void AddNoteCommand(Note note)
        {
            if (note.Parent != null)
            {
                try
                {
                    note.Parent.Notes.Add(note);
                    note.ChangeNoteInFile();
                    //刷新所有子条目
                    NotePanel.Children.Add(CreateNoteBar(note));
                    UpdateNoteUnitPanel();
                    MessageWindow.ShowTipWindow("成功编辑条目:" + note.Name, Window.GetWindow(this));
                }
                catch (Exception)
                {
                }
            }
        }

        private void ChangeNoteCommand(Note note)
        {
            if (note.Parent != null)
            {
                try
                {
                    note.ChangeNoteInFile();
                    int ind = note.Parent.Notes.IndexOf(note);
                    NotePanel.Children.RemoveAt(ind);
                    NotePanel.Children.Insert(ind, CreateNoteBar(note));
                    UpdateNoteUnitPanel();
                    MessageWindow.ShowTipWindow("成功编辑条目:" + note.Name, Window.GetWindow(this));
                }
                catch (Exception)
                {
                }
            }
        }


        private NoteUnitBar CreateNoteBar(Note note)
        {
            NoteUnitBar bar = new NoteUnitBar();
            bar.LoadNote(note);
            bar.Tag = note;
            bar.DeleteCommand += DeleteNoteCommand;
            bar.ClickCommand += UpdatePressState;
            bar.ClickCommand += ShowNoteUnits;
            bar.DoubleClickCommand += ShowNoteSetWindow;
            return bar;
        }

        private void ShowNoteUnits(object sender, MouseButtonEventArgs e)
        {
            try
            {
                CurrentNote = (sender as NoteUnitBar).Tag as Note;
                NoteTitle.Content = CurrentNote.Name;
                UpdateNoteUnitPanel();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 笔记右键点击
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void DeleteNoteCommand(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("删除提示", "确定要删除此记录吗？此操作不可恢复", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
            {
                try
                {
                    var note = (((sender as DecoratedButton).Tag as NoteUnitBar).Tag as Note);
                    note.Parent.Notes.Remove(note);
                    note.DeleteNoteFromFile();
                    MessageWindow.ShowTipWindow("记录已删除", Window.GetWindow(this));
                    NotePanel.Children.Remove(((sender as DecoratedButton).Tag as NoteUnitBar));
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("删除未完成：" + ex.Message, Window.GetWindow(this));
                }
            }
        }

        /// <summary>
        /// 更新高亮状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdatePressState(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in NotePanel.Children)
            {
                (item as NoteUnitBar).ReleasePressed();
            }
            (sender as NoteUnitBar).KeepPressed();
        }


        private NoteUnitBar CreateNoteUnitBar(NoteUnit note)
        {
            NoteUnitBar bar = new NoteUnitBar();
            bar.LoadNoteUnit(note);
            bar.Tag = note;
            bar.DeleteCommand += DeleteNoteUnitCommand;
            bar.ClickCommand += UpdateUnitPressState;
            bar.DoubleClickCommand += ShowNoteUnitWindow;
            return bar;
        }

        private void ShowNoteUnitWindow(object sender, MouseButtonEventArgs e)
        {
            changeNoteUnitWindow.LoadNoteUnit((sender as NoteUnitBar).Tag as NoteUnit);
            changeNoteUnitWindow.ImageViewer.ParentPage = ParentPage;
            changeNoteUnitWindow.Show();
        }

        private void UpdateUnitPressState(object sender, MouseButtonEventArgs e)
        {
        }

        private void DeleteNoteUnitCommand(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("删除提示", "确定要删除此记录吗？此操作不可恢复", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
            {
                try
                {
                    var note = (((sender as DecoratedButton).Tag as NoteUnitBar).Tag as NoteUnit);
                    note.Parent.NoteContents.Remove(note);
                    note.DeleteNoteUnitFromFile();
                    MessageWindow.ShowTipWindow("记录已删除", Window.GetWindow(this));
                    NoteUnitPanel.Children.Remove(((sender as DecoratedButton).Tag as NoteUnitBar));
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("删除未完成：" + ex.Message, Window.GetWindow(this));
                }
            }
        }

        #region 面板刷新
        public void UpdateNoteUnitPanel()
        {
            if (CurrentNote != null)
            {
                NoteUnitPanel.Children.Clear();
                foreach (var item in CurrentNote.NoteContents)
                {
                    NoteUnitPanel.Children.Add(CreateNoteUnitBar(item));
                }
            }
        }

        public void UpdateNotePanel()
        {
            if (CurrentNoteAssemble != null)
            {
                CurrentNote = null;
                NoteUnitPanel.Children.Clear();
                NotePanel.Children.Clear();
                foreach (var item in CurrentNoteAssemble.Notes)
                {
                    NotePanel.Children.Add(CreateNoteBar(item));
                }
            }
        }
        #endregion

        NoteUnitDisplayWindow addWindow = new NoteUnitDisplayWindow("新建条目");
        private void AddNoteUnit(object sender, RoutedEventArgs e)
        {
            if (CurrentNote == null) return;
            NoteUnit unit = new NoteUnit(CurrentNote, "", DateTime.Now);
            unit.ParentTags = CurrentNote.GlobalTags.Select((x) => x.GetOutlineCopy()).ToList();
            foreach (var item in unit.ParentTags)
            {
                item.IsPrivate = false;
            }
            unit.ChangeNoteUnitInFile();
            addNoteUnitWindow.ImageViewer.ParentPage = ParentPage;
            addNoteUnitWindow.LoadNoteUnit(unit);
            IsEnabled = false;
            addNoteUnitWindow.Show();
        }

        private void NoteUnitAppliedCommand(NoteUnit note)
        {
            try
            {
                note.ChangeNoteUnitInFile();
                note.Parent.NoteContents.Add(note);
                UpdateNoteUnitPanel();
            }
            catch (Exception)
            {
            }
        }

        private void NoteUnitChangedCommand(NoteUnit unit)
        {
            try
            {
                unit.ChangeNoteUnitInFile();
                UpdateNoteUnitPanel();
            }
            catch (Exception)
            {
            }
        }

        private void NoteUnitUnAppliedCommand(NoteUnit unit)
        {
            unit.DeleteNoteUnitFromFile();
        }
    }
}
