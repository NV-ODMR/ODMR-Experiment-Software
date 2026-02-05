using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分.电源;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;
using Label = System.Windows.Controls.Label;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class TagCreateControl : Grid
    {
        public TagCreateControl()
        {
            InitializeComponent();
        }

        private void DecoratedButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private bool IsGlobal = false;

        public void SetGlobalTagMode()
        {
            PureColumn.Width = new GridLength(0, GridUnitType.Star);
            SingleColumn.Width = new GridLength(1, GridUnitType.Star);
            MultiColumn.Width = new GridLength(1, GridUnitType.Star);
            CaptionColumn.Width = new GridLength(1, GridUnitType.Star);
            IsGlobal = true;
        }

        public void SetInnerTagMode()
        {
            PureColumn.Width = new GridLength(1, GridUnitType.Star);
            SingleColumn.Width = new GridLength(0);
            MultiColumn.Width = new GridLength(0);
            CaptionColumn.Width = new GridLength(0);
            IsGlobal = false;
        }

        private void AddTag(object sender, RoutedEventArgs e)
        {
            if ((sender as DecoratedButton).Text == "纯文本")
            {
                NoteUnitPanel.Children.Add(CreateTag(new PureTextTag() { IsPrivate = true }, true));
            }
            if ((sender as DecoratedButton).Text == "单选")
            {
                CreateTag(new PureTextTag());
                NoteUnitPanel.Children.Add(CreateTag(new SingleOptionTag() { IsPrivate = true }, true));
            }
            if ((sender as DecoratedButton).Text == "多选")
            {
                NoteUnitPanel.Children.Add(CreateTag(new MultiOptionTag() { IsPrivate = true }, true));
            }
            if ((sender as DecoratedButton).Text == "标题文本")
            {
                NoteUnitPanel.Children.Add(CreateTag(new CaptionTextTag() { IsPrivate = true }, true));
            }
        }

        private TagPanelBase CreateTag(NoteTag tag, bool allowedit = true)
        {
            TagPanelBase panel = null;
            if (tag is PureTextTag)
            {
                panel = new PureTextPanel(tag as PureTextTag);
            }
            if (tag is SingleOptionTag)
            {
                panel = new OptionPanel(tag as SingleOptionTag);
            }
            if (tag is MultiOptionTag)
            {
                panel = new OptionPanel(tag as MultiOptionTag);
            }
            if (tag is CaptionTextTag)
            {
                panel = new CaptionPanel(tag as CaptionTextTag);
            }
            panel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2E2E2E"));
            if (tag.IsPrivate)
            {
                panel.SetUnLockDisplay();
                if (allowedit)
                {
                    panel.SetFullyEditable();
                }
                else
                {
                    panel.SetFullyUnEditable();
                }
            }
            else
            {
                panel.SetLockDisplay();
                if (allowedit)
                {
                    panel.SetPartialEditable();
                }
                else
                {
                    panel.SetFullyUnEditable();
                }
            }

            panel.SetSelectorColor(tag.TagColor == Colors.Transparent ? ColorHelper.GenerateHighContrastColor((Color)ColorConverter.ConvertFromString("#FF1A1A1A")) : tag.TagColor);
            panel.ColorSelectClicked += new RoutedEventHandler((sender, e) => { var btn = sender as DecoratedButton; ColorPopup.PlacementTarget = btn; ColorSelect.CurrentColor = (btn.Foreground as SolidColorBrush).Color; ColorPopup.IsOpen = true; ColorPopup.Focus(); });

            panel.Tag = tag;

            if (tag.IsPrivate)
            {
                ContextMenu menu = new ContextMenu();
                var deletebtn = new DecoratedButton() { Text = "删除标签" };
                templatebtn.CloneStyleTo(deletebtn);
                deletebtn.Click += new RoutedEventHandler((sender, e) =>
                {
                    if (MessageWindow.ShowMessageBox("删除提示", "确定要删除此标签吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            //删除标签
                            NoteUnitPanel.Children.Remove((sender as DecoratedButton).Tag as TagPanelBase);
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
                deletebtn.Tag = panel;
                menu.Items.Add(deletebtn);
                menu.ApplyToControl(panel);
            }
            return panel;
        }

        public List<NoteTag> GetTags()
        {
            List<NoteTag> result = new List<NoteTag>();
            foreach (var item in NoteUnitPanel.Children)
            {
                NoteTag tag = null;
                if ((item as FrameworkElement).Tag is PureTextTag)
                {
                    var rawtag = (item as FrameworkElement).Tag as PureTextTag;
                    string content = (item as PureTextPanel).Content;
                    tag = new PureTextTag() { Content = content, IsPrivate = rawtag.IsPrivate };
                    result.Add(tag);
                }
                if ((item as FrameworkElement).Tag is CaptionTextTag)
                {
                    var rawtag = (item as FrameworkElement).Tag as CaptionTextTag;
                    tag = new CaptionTextTag() { Title = (item as CaptionPanel).Caption, Content = (item as CaptionPanel).Conetnt, IsPrivate = rawtag.IsPrivate };
                    if ((item as CaptionPanel).Caption != "")
                        result.Add(tag);
                }
                if ((item as FrameworkElement).Tag is SingleOptionTag)
                {
                    var rawtag = (item as FrameworkElement).Tag as SingleOptionTag;
                    tag = (item as OptionPanel).GenerateSingleOptionTag();
                    tag.IsPrivate = rawtag.IsPrivate;
                    if ((tag as SingleOptionTag).Options.Count != 0) result.Add(tag);
                }
                if ((item as FrameworkElement).Tag is MultiOptionTag)
                {
                    var rawtag = (item as FrameworkElement).Tag as MultiOptionTag;
                    tag = (item as OptionPanel).GenerateMultiOptionTag();
                    tag.IsPrivate = rawtag.IsPrivate;
                    if ((tag as MultiOptionTag).Options.Count != 0) result.Add(tag);
                }
                tag.TagColor = (item as TagPanelBase).GetSelectorColor();
            }
            return result;
        }

        public void LoadTags(List<NoteTag> tags, bool allowEdit = true)
        {
            NoteUnitPanel.Children.Clear();
            foreach (NoteTag tag in tags)
            {
                NoteUnitPanel.Children.Add(CreateTag(tag, allowEdit));
            }
        }

        public void AppendTags(List<NoteTag> tags, bool allowEdit = true)
        {
            foreach (NoteTag tag in tags)
            {
                NoteUnitPanel.Children.Add(CreateTag(tag, allowEdit));
            }
        }

        private void ColorSelect_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (ColorPopup.PlacementTarget is DecoratedButton)
            {
                (ColorPopup.PlacementTarget as DecoratedButton).Foreground = new SolidColorBrush((sender as ColorSelector).CurrentColor);
            }
        }

        private void ColorSelect_LostFocus(object sender, RoutedEventArgs e)
        {
            ColorPopup.IsOpen = false;
        }
    }
}
