using CodeHelper;
using Controls.Windows;
using ODMR_Lab.数据记录;
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

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddNoteWindow : Window
    {

        public event Action<Note> NoteApplied = null;

        public event Action<NoteAssemble> NoteAssembleApplied = null;

        public event RoutedEventHandler HideAction = null;

        private Note OriginNote = null;

        private NoteAssemble OriginAssemble = null;

        private bool IsAssembleMode = false;

        public AddNoteWindow(string wintitle)
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.AfterHide += new RoutedEventHandler((sender, e) => HideAction?.Invoke(sender, e));
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
            Title = wintitle;
            title.Content = "     " + wintitle;
        }


        public void LoadNote(Note note)
        {
            OriginNote = note;
            InnerColumn.Width = new GridLength(1, GridUnitType.Star);
            NoteTitle.Text = note.Name;
            GlobalTagsPanel.LoadTags(note.GlobalTags, true);
            InnerTagsPanel.LoadTags(note.ParentTags, true);
            InnerTagsPanel.AppendTags(note.InnerTags, true);
            InnerTagsPanel.SetInnerTagMode();
            GlobalTagsPanel.SetGlobalTagMode();
            IsAssembleMode = false;
        }

        public void LoadNoteAssemble(NoteAssemble note)
        {
            OriginAssemble = note;
            InnerColumn.Width = new GridLength(0);
            NoteTitle.Text = note.Name;
            InnerTagsPanel.LoadTags(new List<NoteTag>(), true);
            GlobalTagsPanel.LoadTags(note.GlobalTags, true);
            InnerTagsPanel.SetInnerTagMode();
            GlobalTagsPanel.SetGlobalTagMode();
            IsAssembleMode = true;
        }

        private void SaveNote(object sender, RoutedEventArgs e)
        {
            if (IsAssembleMode == false)
            {
                try
                {
                    if (NoteTitle.Text == "") throw new Exception("条目标题不能为空");
                    OriginNote.Name = NoteTitle.Text;
                    var privatetags = InnerTagsPanel.GetTags();
                    OriginNote.InnerTags = privatetags.Where((x) => x.IsPrivate).ToList();
                    OriginNote.ParentTags = privatetags.Where((x) => x.IsPrivate == false).ToList();
                    OriginNote.GlobalTags = GlobalTagsPanel.GetTags();
                    NoteApplied?.Invoke(OriginNote);
                    Hide();
                    HideAction?.Invoke(sender, e);
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("条目无法保存" + ex.Message, this);
                }
            }
            else
            {
                try
                {
                    if (NoteTitle.Text == "") throw new Exception("记录本标题不能为空");
                    OriginAssemble.Name = NoteTitle.Text;
                    OriginAssemble.GlobalTags = GlobalTagsPanel.GetTags();
                    NoteAssembleApplied?.Invoke(OriginAssemble);
                    Hide();
                    HideAction?.Invoke(sender, e);
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("记录本无法保存" + ex.Message, this);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
