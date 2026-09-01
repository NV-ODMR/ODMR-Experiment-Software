using CodeHelper;
using System;
using System.Linq;
using System.Windows;

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// GlobalSequenceWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChannelIDSelectWindow : Window
    {
        public Action AfterHideEvent = null;

        public ChannelIDSelectWindow()
        {
            InitializeComponent();

            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterHideWindow(this, null, null, CloseBtn, null, 4, 30);
            h.AfterHide += ((s, e) =>
            {
                AfterHideEvent?.Invoke();
            });
        }

        public SequenceChannel AddedChannel = SequenceChannel.None;

        public new void Show(SingleChannelData data)
        {
            AddedChannel = SequenceChannel.None;
            ChannelPanel.ClearItems();
            var es = Enum.GetNames(typeof(SequenceChannel));
            var channels = data.Channels.Select(x => Enum.GetName(typeof(SequenceChannel), x.Key)).ToList();
            foreach (var item in es)
            {
                if (!channels.Contains(item))
                {
                    ChannelPanel.AddItem(item, item);
                }
            }
            base.Show();
        }

        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Confirm(object sender, RoutedEventArgs e)
        {
            AddedChannel = (SequenceChannel)Enum.Parse(typeof(SequenceChannel), ChannelPanel.GetSelectedTag() as string);
            Hide();
            AfterHideEvent?.Invoke();
        }
    }
}
