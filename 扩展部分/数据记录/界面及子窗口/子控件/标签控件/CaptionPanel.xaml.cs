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
    public partial class CaptionPanel : TagPanelBase
    {
        public override event RoutedEventHandler ColorSelectClicked = null;

        private CaptionTextTag captiontag = null;

        public bool CaptionEditable
        {
            get
            {
                return !CaptionContent.IsReadOnly;
            }
            set
            {
                CaptionContent.IsReadOnly = !value;
            }
        }

        public bool ContentEditable
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

        /// <summary>
        /// 标签内容
        /// </summary>
        public string Conetnt
        {
            get
            {
                return StringContent.Text;
            }
            set
            {
                StringContent.Text = value;
            }
        }

        public string Caption
        {
            get
            {
                return CaptionContent.Text;
            }
            set
            {
                CaptionContent.Text = value;
            }
        }

        public override void SetPartialEditable()
        {
            CaptionEditable = false;
            ContentEditable = true;
            CaptionLabel.Visibility = Visibility.Collapsed;
            ContentLabel.Visibility = Visibility.Collapsed;
            ColorBtn.IsHitTestVisible = false;
        }

        public override void SetFullyEditable()
        {
            CaptionEditable = true;
            ContentEditable = true;
            CaptionLabel.Visibility = Visibility.Visible;
            ContentLabel.Visibility = Visibility.Visible;
            ColorBtn.IsHitTestVisible = true;
        }

        public override void SetFullyUnEditable()
        {
            CaptionEditable = false;
            ContentEditable = false;
            CaptionLabel.Visibility = Visibility.Collapsed;
            ContentLabel.Visibility = Visibility.Collapsed;
            ColorBtn.IsHitTestVisible = false;
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

        public CaptionPanel(CaptionTextTag tag)
        {
            InitializeComponent();
            captiontag = tag;
            CaptionContent.Text = tag.Title;
            StringContent.Text = tag.Content;
        }

        public CaptionTextTag GenerateTag()
        {
            CaptionTextTag tag = new CaptionTextTag();
            captiontag.FullCopyTo(tag);
            tag.Title = CaptionContent.Text;
            tag.Content = StringContent.Text;
            return tag;
        }
    }
}
