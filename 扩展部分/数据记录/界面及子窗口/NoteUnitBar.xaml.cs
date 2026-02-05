using CodeHelper;
using Controls;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class NoteUnitBar : Grid
    {
        public event RoutedEventHandler DeleteCommand = null;

        public event MouseButtonEventHandler DoubleClickCommand = null;

        public event MouseButtonEventHandler ClickCommand = null;

        private MouseColorHelper helper = null;

        public NoteUnitBar()
        {
            InitializeComponent();
            Background = Brushes.Transparent;
            helper = new MouseColorHelper(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2E2E2E")), new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")), new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D")), true, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D")));
            helper.RegistateTarget(this);
            ControlEventHelper hel = new ControlEventHelper(this);
            hel.Click += ClickEvent;
            hel.MouseDoubleClick += DoubleClickEvent;
            ContextMenu menu = new ContextMenu();
            DecoratedButton btn = new DecoratedButton() { };
            template.CloneStyleTo(btn);
            btn.Text = "删除条目";
            btn.Click += DeleteEvent;
            btn.Tag = this;
            menu.Items.Add(btn);
            menu.ApplyToControl(this);
        }

        private void DeleteEvent(object sender, RoutedEventArgs e)
        {
            DeleteCommand?.Invoke(sender, e);
        }

        private void DoubleClickEvent(object sender, MouseButtonEventArgs e)
        {
            DoubleClickCommand?.Invoke(sender, e);
        }

        private void ClickEvent(object sender, MouseButtonEventArgs e)
        {
            ClickCommand?.Invoke(sender, e);
        }

        public void LoadNoteUnit(NoteUnit noteunit)
        {
            TimeRow.Height = new GridLength(30);
            FileColumn.Width = new GridLength(30);
            Description.Text = noteunit.Description;
            Time.Content = noteunit.NoteTime.ToString("yyyy-MM-dd HH:mm:ss");
            FileCount.Content = noteunit.GetFileCount();
            //加载标签
            TagPanel.Children.Clear();
            foreach (var item in noteunit.ParentTags)
            {
                var tags = CreateTagButton(item);
                foreach (var tag in tags)
                {
                    TagPanel.Children.Add(tag);
                }
            }
            foreach (var item in noteunit.InnerTags)
            {
                var tags = CreateTagButton(item);
                foreach (var tag in tags)
                {
                    TagPanel.Children.Add(tag);
                }
            }
        }

        public void KeepPressed()
        {
            helper.KeepPressed = true;
        }

        public void ReleasePressed()
        {
            helper.KeepPressed = false;
        }

        public void LoadNote(Note note)
        {
            TimeRow.Height = new GridLength(0);
            FileColumn.Width = new GridLength(0);
            Description.Text = note.Name;
            //加载标签
            TagPanel.Children.Clear();
            foreach (var item in note.ParentTags)
            {
                var tags = CreateTagButton(item);
                foreach (var tag in tags)
                {
                    TagPanel.Children.Add(tag);
                }
            }

            foreach (var item in note.InnerTags)
            {
                var tags = CreateTagButton(item);
                foreach (var tag in tags)
                {
                    TagPanel.Children.Add(tag);
                }
            }
        }

        public List<Border> CreateTagButton(NoteTag tag)
        {
            if (tag is PureTextTag)
            {
                Border b = new Border();
                b.Margin = new Thickness(3);
                b.CornerRadius = new CornerRadius(10);
                b.MaxWidth = 300;
                b.MinWidth = 30;
                b.Background = new SolidColorBrush(tag.TagColor);
                b.ToolTip = (tag as PureTextTag).Content;
                b.Child = new Label() { Foreground = Brushes.White, Content = (tag as PureTextTag).Content, VerticalAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
                return new List<Border>() { b };
            }
            if (tag is CaptionTextTag)
            {
                Border b = new Border();
                b.Margin = new Thickness(3);
                b.CornerRadius = new CornerRadius(10);
                b.MaxWidth = 300;
                b.MinWidth = 30;
                b.Background = new SolidColorBrush(tag.TagColor);
                b.ToolTip = (tag as CaptionTextTag).Title + " : " + (tag as CaptionTextTag).Content;
                b.Child = new Label() { Foreground = Brushes.White, Content = (tag as CaptionTextTag).Title + " : " + (tag as CaptionTextTag).Content, VerticalAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
                return new List<Border>() { b };
            }
            if (tag is SingleOptionTag)
            {
                Border b = new Border();
                b.Margin = new Thickness(3);
                b.CornerRadius = new CornerRadius(10);
                b.MaxWidth = 300;
                b.MinWidth = 30;
                b.Background = new SolidColorBrush(tag.TagColor);
                string content = (tag as SingleOptionTag).Title + ":" + (tag as SingleOptionTag).Options[(tag as SingleOptionTag).OptionIndex];
                b.ToolTip = content;
                b.Child = new Label() { Foreground = Brushes.White, Content = content, VerticalAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
                return new List<Border>() { b };
            }
            if (tag is MultiOptionTag)
            {
                List<Border> results = new List<Border>();
                Border b = new Border();
                b.Margin = new Thickness(3);
                b.CornerRadius = new CornerRadius(10);
                b.MaxWidth = 300;
                b.MinWidth = 30;
                b.Background = new SolidColorBrush(tag.TagColor);
                string content = (tag as MultiOptionTag).Title + " : ";
                for (int i = 0; i < (tag as MultiOptionTag).OptionIndex.Count; i++)
                {
                    int index = (tag as MultiOptionTag).OptionIndex[i];
                    content += (tag as MultiOptionTag).Options[index];
                    if (i < (tag as MultiOptionTag).OptionIndex.Count - 1)
                    {
                        content += " , ";
                    }
                }
                b.ToolTip = content;
                b.Child = new Label() { Foreground = Brushes.White, Content = content, VerticalAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
                results.Add(b);
                return results;
            }
            return new List<Border>();
        }
    }
}
