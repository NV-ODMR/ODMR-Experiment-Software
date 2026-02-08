using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using ODMR_Lab.扩展部分.数据记录;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分.电源;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class NoteSearchBar : Grid
    {
        /// <summary>
        /// 搜索指令触发事件
        /// </summary>
        public event Action<List<Note>> NoteSearchFinished = null;

        /// <summary>
        /// 搜索指令触发事件
        /// </summary>
        public event Action<List<NoteUnit>> NoteUnitSearchFinished = null;

        public event RoutedEventHandler BeforeSearch = null;

        public List<Note> SourceNote { get; set; } = null;

        public List<NoteUnit> SourceNoteUnit { get; set; } = null;

        public List<KeyValuePair<string, object>> TitleSearchData { get; set; } = new List<KeyValuePair<string, object>>();

        public List<KeyValuePair<string, object>> TagSearchData { get; set; } = new List<KeyValuePair<string, object>>();

        public NoteSearchBar()
        {
            InitializeComponent();
        }

        public void SetNoteSource(List<Note> source)
        {
            SourceNote = source;
            TitleSearchData = source.Select((x) => new KeyValuePair<string, object>(x.Name, x)).ToList();
            TagSearchData.Clear();
            HashSet<string> set = new HashSet<string>();
            foreach (var item in source)
            {
                set = set.Concat(item.ParentTags.Select((x) => x.GetDescription()).Concat(item.InnerTags.Select((x) => x.GetDescription()))).ToHashSet();
            }
            List<KeyValuePair<string, object>> tagresult = new List<KeyValuePair<string, object>>();
            foreach (var item in set)
            {
                var notes = source.Where((x) => x.InnerTags.Where((innert) => innert.GetDescription() == item).Count() != 0 || x.ParentTags.Where((parentt) => parentt.GetDescription() == item).Count() != 0).ToList();
                TagSearchData.Add(new KeyValuePair<string, object>(item, notes));
            }
            TagSearcher.SearchList = TagSearchData;
        }

        public void SetNoteUnitSource(List<NoteUnit> source)
        {
            SourceNoteUnit = source;
            TitleSearchData = source.Select((x) => new KeyValuePair<string, object>(x.Description, x)).ToList();
            TagSearchData.Clear();
            HashSet<string> set = new HashSet<string>();
            foreach (var item in source)
            {
                set = set.Concat(item.ParentTags.Select((x) => x.GetDescription()).Concat(item.InnerTags.Select((x) => x.GetDescription()))).ToHashSet();
            }
            List<KeyValuePair<string, object>> tagresult = new List<KeyValuePair<string, object>>();
            foreach (var item in set)
            {
                var notes = source.Where((x) => x.InnerTags.Where((innert) => innert.GetDescription() == item).Count() != 0 && x.ParentTags.Where((parentt) => parentt.GetDescription() == item).Count() != 0).ToList();
                TagSearchData.Add(new KeyValuePair<string, object>(item, notes));
            }
            TagSearcher.SearchList = TagSearchData;
        }

        public List<NoteUnit> GetNoteUnitTitleSearchResult()
        {
            return SourceNoteUnit;
        }

        public List<NoteUnit> GetNoteTitleSearchResult()
        {
            return SourceNoteUnit;
        }

        private void SearchTitle(object sender, RoutedEventArgs e)
        {
            BeforeSearch?.Invoke(this, e);
            var result = SearchHelper.GetFuzzySearchResult(TitleInput.Text, TitleSearchData, 0.9);
            if (TitleInput.Text == "")
                result = TitleSearchData;
            var notes = result.Select((x) => x.Value).ToHashSet();

            if (SourceNote != null)
            {
                NoteSearchFinished?.Invoke(notes.Select((x) => x as Note).ToList());
                return;
            }
            if (SourceNoteUnit != null)
            {
                NoteUnitSearchFinished?.Invoke(notes.Select((x) => x as NoteUnit).ToList());
                return;
            }
        }

        private void TitleInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchTitle(this, e);
            }
        }

        private void TagSearcher_ResultSelected(KeyValuePair<string, object> obj)
        {
            if (SourceNote != null)
            {
                NoteSearchFinished?.Invoke((List<Note>)obj.Value);
                return;
            }
            if (SourceNoteUnit != null)
            {
                NoteUnitSearchFinished?.Invoke((List<NoteUnit>)obj.Value);
                return;
            }
        }
    }
}
