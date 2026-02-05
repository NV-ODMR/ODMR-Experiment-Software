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
    public partial class OptionPanel : TagPanelBase
    {
        private bool optionsEditable = true;
        private bool OptionsEditable
        {
            get { return optionsEditable; }
            set
            {
                if (value)
                {
                    foreach (var item in OptionDisplayPanel.Children)
                    {
                        ((item as Grid).Children[1] as TextBox).IsReadOnly = false;
                        AddRow.Height = new GridLength(50);
                        Addbtn.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    foreach (var item in OptionDisplayPanel.Children)
                    {
                        ((item as Grid).Children[1] as TextBox).IsReadOnly = true;
                        AddRow.Height = new GridLength(0);
                        Addbtn.Visibility = Visibility.Collapsed;
                    }
                }
                optionsEditable = value;
            }
        }

        private bool optionsSelectable = true;
        private bool OptionsSelectable
        {
            get { return optionsSelectable; }
            set
            {
                if (value)
                {
                    foreach (var item in OptionDisplayPanel.Children)
                    {
                        ((item as Grid).Children[0] as Chooser).IsEnabled = true;
                    }
                }
                else
                {
                    foreach (var item in OptionDisplayPanel.Children)
                    {
                        ((item as Grid).Children[0] as Chooser).IsEnabled = false;
                    }
                }
                optionsSelectable = value;
            }
        }

        private bool isMultiOption = false;

        public override event RoutedEventHandler ColorSelectClicked = null;

        bool IsMultiOption
        {
            get
            {
                return isMultiOption;
            }
            set
            {
                IsMultiSelection.IsSelected = value;
                isMultiOption = value;
            }
        }

        public override void SetPartialEditable()
        {
            OptionsSelectable = true;
            OptionsEditable = false;
            TitleLabel.Visibility = Visibility.Collapsed;
            TitleBox.IsReadOnly = true;
            ColorBtn.IsHitTestVisible = false;
            SetUnDeletable();
        }

        public override void SetFullyEditable()
        {
            OptionsSelectable = true;
            OptionsEditable = true;
            TitleLabel.Visibility = Visibility.Visible;
            TitleBox.IsReadOnly = false;
            ColorBtn.IsHitTestVisible = true;
            SetDeletable();
        }

        public override void SetFullyUnEditable()
        {
            OptionsSelectable = false;
            OptionsEditable = false;
            TitleLabel.Visibility = Visibility.Collapsed;
            TitleBox.IsReadOnly = true;
            ColorBtn.IsHitTestVisible = false;
            SetUnDeletable();
        }

        public override void SetLockDisplay()
        {
            LockColumn = new ColumnDefinition() { Width = new GridLength(40) };
            LockImage.Visibility = Visibility.Visible;
        }

        public override void SetUnLockDisplay()
        {
            LockColumn = new ColumnDefinition() { Width = new GridLength(0) };
            LockImage.Visibility = Visibility.Hidden;
        }

        public OptionPanel(SingleOptionTag tag)
        {
            InitializeComponent();
            OptionDisplayPanel.Children.Clear();
            TitleBox.Text = tag.Title;
            IsMultiSelection.IsSelected = false;
            //单选
            foreach (var item in tag.Options)
            {
                var bar = CreateOptionBar(item);
                OptionDisplayPanel.Children.Add(bar);
            }
            isMultiOption = false;
            if (tag.Options.Count > 0)
                Index(tag.OptionIndex, true);
        }
        public OptionPanel(MultiOptionTag tag)
        {
            InitializeComponent();
            OptionDisplayPanel.Children.Clear();
            TitleBox.Text = tag.Title;
            IsMultiSelection.IsSelected = true;
            //多选
            foreach (var item in tag.Options)
            {
                var bar = CreateOptionBar(item);
                OptionDisplayPanel.Children.Add(bar);
            }
            isMultiOption = true;
            foreach (var item in tag.OptionIndex)
            {
                Index(item, true);
            }
        }

        public SingleOptionTag GenerateSingleOptionTag()
        {
            SingleOptionTag singleop = new SingleOptionTag();
            singleop.Title = TitleBox.Text;
            for (int i = 0; i < OptionDisplayPanel.Children.Count; i++)
            {
                singleop.Options.Add(((OptionDisplayPanel.Children[i] as Grid).Children[1] as TextBox).Text);
                if (((OptionDisplayPanel.Children[i] as Grid).Children[0] as Chooser).IsSelected) singleop.OptionIndex = i;
            }

            return singleop;
        }

        public MultiOptionTag GenerateMultiOptionTag()
        {
            MultiOptionTag multiop = new MultiOptionTag();
            multiop.Title = TitleBox.Text;
            for (int i = 0; i < OptionDisplayPanel.Children.Count; i++)
            {
                multiop.Options.Add(((OptionDisplayPanel.Children[i] as Grid).Children[1] as TextBox).Text);
                if (((OptionDisplayPanel.Children[i] as Grid).Children[0] as Chooser).IsSelected) multiop.OptionIndex.Add(i);
            }

            return multiop;
        }

        private Grid CreateOptionBar(string optioncontent)
        {
            Grid grid = new Grid();
            grid.MinHeight = 30;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });
            Chooser c = new Chooser();
            c.IsSelected = false;
            c.UnChooseBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D"));
            c.ChooseBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3AA7F0"));
            c.Height = 13;
            c.Width = 25;
            c.HasAnimation = false;
            c.Selected += JudgeMultiOption;
            DecoratedButton btn = new DecoratedButton();
            Addbtn.CloneStyleTo(btn);
            btn.Width = 20;
            btn.Height = 20;
            btn.Text = "×";
            btn.Tag = grid;
            //删除选项
            btn.Click += new RoutedEventHandler((sender, e) => { OptionDisplayPanel.Children.Remove((sender as DecoratedButton).Tag as Grid); });
            grid.Children.Add(c);
            TextBox textBlock = new TextBox() { Text = optioncontent, ContextMenu = null };
            UIUpdater.CloneStyle(TextBoxTemplate, textBlock);
            grid.Children.Add(textBlock);
            grid.Children.Add(btn);
            Grid.SetColumn(textBlock, 1);
            Grid.SetColumn(btn, 2);
            return grid;
        }

        private void SetDeletable()
        {
            foreach (var item in OptionDisplayPanel.Children)
            {
                (item as Grid).ColumnDefinitions[2].Width = new GridLength(30);
            }
        }

        private void SetUnDeletable()
        {
            foreach (var item in OptionDisplayPanel.Children)
            {
                (item as Grid).ColumnDefinitions[2].Width = new GridLength(0);
            }
        }

        private void Index(int ind, bool state)
        {
            ((OptionDisplayPanel.Children[ind] as Grid).Children[0] as Chooser).IsSelected = state;
        }

        private void JudgeMultiOption(object sender, RoutedEventArgs e)
        {
            if (!isMultiOption)
            {
                foreach (var item in OptionDisplayPanel.Children)
                {
                    ((item as Grid).Children[0] as Chooser).SetSelectStateWithoutTrigger(false);
                }
                (sender as Chooser).SetSelectStateWithoutTrigger(true);
            }
        }

        private void MultiSelectionChanged(object sender, RoutedEventArgs e)
        {
            isMultiOption = IsMultiSelection.IsSelected;
            UpdateMultiOptionState();
        }

        private void UpdateMultiOptionState()
        {
            if (!isMultiOption)
            {
                int count = 0;
                foreach (var item in OptionDisplayPanel.Children)
                {
                    if (((item as Grid).Children[0] as Chooser).IsSelected) ++count;
                    if (count > 1)
                    {
                        ((item as Grid).Children[0] as Chooser).IsSelected = false;
                        count = 1;
                        continue;
                    }
                }
            }
        }

        private void AddOption(object sender, RoutedEventArgs e)
        {
            OptionDisplayPanel.Children.Add(CreateOptionBar(""));
        }

        private void ColorSelectorClicked(object sender, RoutedEventArgs e)
        {
            ColorSelectClicked?.Invoke(sender, e);
        }

        public override void SetSelectorColor(Color color)
        {
            ColorBtn.Foreground = new SolidColorBrush(color);
        }

        public override Color GetSelectorColor()
        {
            return (ColorBtn.Foreground as SolidColorBrush).Color;
        }
    }
}
