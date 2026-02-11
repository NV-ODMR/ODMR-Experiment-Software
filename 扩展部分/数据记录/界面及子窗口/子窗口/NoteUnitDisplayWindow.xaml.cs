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
    public partial class NoteUnitDisplayWindow : Window
    {

        public event Action<NoteUnit> NoteUnitApplied = null;

        public event RoutedEventHandler HideAction = null;

        public event Action<NoteUnit> NoteUnitUnApplied = null;

        private NoteUnit OriginNoteUnit = null;

        public NoteUnitDisplayWindow(string wintitle)
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
            hel.AfterHide += AfterHide;
            Title = wintitle;
            title.Content = "     " + wintitle;
            TagsPanel.SetInnerTagMode();
        }


        private void AfterHide(object sender, RoutedEventArgs e)
        {
            HideAction?.Invoke(sender, e);
            NoteUnitUnApplied?.Invoke(OriginNoteUnit);
        }


        public void LoadNoteUnit(NoteUnit noteunit)
        {
            OriginNoteUnit = noteunit;
            NoteTitle.Text = noteunit.Description;
            TimeLabel.Content = noteunit.NoteTime.ToString("yyyy-MM-dd   HH:mm:ss");
            TagsPanel.LoadTags(noteunit.ParentTags, true);
            TagsPanel.AppendTags(noteunit.InnerTags, true);
            ImageViewer.LoadNoteUnit(noteunit);
            TagsPanel.SetInnerTagMode();
        }

        private void SaveNote(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NoteTitle.Text == "") throw new Exception("记录内容不能为空");
                OriginNoteUnit.Description = NoteTitle.Text;
                OriginNoteUnit.ParentTags = TagsPanel.GetTags().Where((x) => x.IsPrivate == false).ToList();
                OriginNoteUnit.InnerTags = TagsPanel.GetTags().Where((x) => x.IsPrivate).ToList();
                NoteUnitApplied?.Invoke(OriginNoteUnit);
                Hide();
                HideAction?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("记录本无法保存" + ex.Message, this);
            }
        }

        private void UnlockGlobalPanel(object sender, RoutedEventArgs e)
        {

        }
    }
}
