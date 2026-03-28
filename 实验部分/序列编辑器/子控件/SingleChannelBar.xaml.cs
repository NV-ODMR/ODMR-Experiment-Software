using Controls;
using ODMR_Lab.序列编辑器;
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

namespace ODMR_Lab.实验部分.序列编辑器.子控件
{
    /// <summary>
    /// GroupRelationSelectionPanel.xaml 的交互逻辑
    /// </summary>
    public partial class SingleChannelBar : Border
    {
        private static ChannelIDSelectWindow window = new ChannelIDSelectWindow();

        private Color channelcolor = Colors.Transparent;
        public Color ChannelColor
        {
            get
            {
                return channelcolor;
            }
            set
            {
                channelcolor = value;
                BorderBrush = new SolidColorBrush(value);
                ChannelColorLabel.Background = new SolidColorBrush(value);
            }
        }

        private SequenceChannel channelID = SequenceChannel.None;
        public SequenceChannel ChannelID
        {
            get
            {
                return channelID;
            }
            set
            {
                channelID = value;
                ChannelInd.Content = Enum.GetName(typeof(SequenceChannel), value);
            }
        }

        SingleChannelData ParentData = null;

        Panel ParentPanel = null;

        DisplayPage ParentPage = null;

        public SingleChannelBar(SequenceChannel channelid, Color channelcolor, SingleChannelData parentData, Panel parentPanel, DisplayPage displayPage)
        {
            InitializeComponent();
            ParentData = parentData;
            ParentPanel = parentPanel;
            ParentPage = displayPage;
            SetContextMenu();
            ChannelID = channelid;
            ChannelColor = channelcolor;
        }

        protected static DecoratedButton ButtonTemplate = new DecoratedButton()
        {
            FontSize = 12,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")),
            Foreground = Brushes.White,
            MoveInColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF424242")),
            PressedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF39393A")),
            MoveInForeground = Brushes.White,
            PressedForeground = Brushes.White,
        };

        protected void SetContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            DecoratedButton btn = new DecoratedButton() { Text = "删除通道" };
            ButtonTemplate.CloneStyleTo(btn);
            btn.Click += DeleteEvent;
            menu.Items.Add(btn);
            menu.ItemHeight = 30;
            btn = new DecoratedButton() { Text = "修改通道序号" };
            ButtonTemplate.CloneStyleTo(btn);
            btn.Click += ChangeIDEvent;
            menu.Items.Add(btn);
            menu.ApplyToControl(this);
        }

        private void ChangeIDEvent(object sender, RoutedEventArgs e)
        {
            window.Owner = Window.GetWindow(ParentPanel);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.AfterHideEvent += new Action(() =>
            {
                if (window.AddedChannel != SequenceChannel.None)
                {
                    ParentPage.IsEnabled = true;
                    ParentData.RemoveChannel(channelID);
                    ParentData.AddChannel(window.AddedChannel);
                }
            });
            ParentPage.IsEnabled = false;
            window.Show(ParentData);
        }

        private void DeleteEvent(object sender, RoutedEventArgs e)
        {
            ParentData.RemoveChannel(channelID);
            ParentPanel.Children.Remove(this);
            ParentPage.UpdateSequenceData();
        }
    }
}
