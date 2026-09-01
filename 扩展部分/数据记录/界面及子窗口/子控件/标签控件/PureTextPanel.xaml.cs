using ODMR_Lab.数据记录;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class PureTextPanel : TagPanelBase
    {
        private PureTextTag texttag = null;

        public override event RoutedEventHandler ColorSelectClicked = null;

        private bool ContentEditable
        {
            get
            {
                return !StringContent.IsReadOnly;
            }
            set
            {
                StringContent.IsReadOnly = !value;
            }
        }

        public string Content
        {
            get { return StringContent.Text; }
            set
            {
                StringContent.Text = value;
            }
        }

        public PureTextPanel(PureTextTag tag)
        {
            InitializeComponent();
            texttag = tag;
            StringContent.Text = tag.Content;
        }

        public PureTextTag GenerateTag()
        {
            PureTextTag tag = new PureTextTag();
            texttag.FullCopyTo(tag);
            tag.Content = StringContent.Text;
            return tag;
        }

        public override void SetPartialEditable()
        {
            ContentEditable = true;
            ColorBtn.IsHitTestVisible = false;
        }

        public override void SetFullyEditable()
        {
            ContentEditable = true;
            ColorBtn.IsHitTestVisible = true;
        }

        public override void SetFullyUnEditable()
        {
            ContentEditable = false;
            ColorBtn.IsHitTestVisible = false;
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
