using Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace ODMR_Lab.实验部分.序列编辑器.子控件
{
    /// <summary>
    /// MultiChannel_SingleSequenceControl.xaml 的交互逻辑
    /// </summary>
    public partial class SingleChannelSequenceControl : ChannelSequenceControlBase
    {
        public override event Action<ChannelSequenceControlBase> SequenceNameSelectionEvent = null;

        public override event Action<ChannelSequenceControlBase> GroupNameSelectionEvent = null;

        public event Action<ChannelSequenceControlBase> ChannelRelationSetEvent = null;

        public static Label LabelTemplate = new Label() { Cursor = Cursors.Hand, Width = 10, Height = 10, Margin = new Thickness(5), BorderThickness = new Thickness(0.1) };

        public Color ChannelUnSelectedColor = (Color)ColorConverter.ConvertFromString("#FF4D4D4D");

        public SingleChannelData ParentData = null;

        public SingleChannelSequenceControl(SequenceSegBase seg, SingleChannelData ParentData)
        {
            parentWaveSeg = seg;
            this.ParentData = ParentData;
            InitializeComponent();
            SetContextMenu();
            if (seg is SingleSequenceWaveSeg)
            {
                ValidateChannelTags();
            }
        }

        public void ValidateChannelTags()
        {
            SingleSequenceChannelPanel.Children.Clear();
            var channels = ParentData.GetSinglePulseChannels(parentWaveSeg as SingleSequenceWaveSeg);
            foreach (var item in ParentData.Channels)
            {
                Label l = new Label() { Margin = new Thickness(5) };
                UIUpdater.CloneStyle(LabelTemplate, l);
                l.BorderThickness = new Thickness(0.5);
                l.Tag = item;
                l.ToolTip = Enum.GetName(typeof(SequenceChannel), item.Key) + " 通道值";
                l.BorderBrush = new SolidColorBrush(item.Value);
                l.Background = channels.Contains(item.Key) ? new SolidColorBrush(item.Value) : new SolidColorBrush(ChannelUnSelectedColor);
                l.MouseLeftButtonDown += new MouseButtonEventHandler((s, e) =>
                {
                    var channs = ParentData.GetSinglePulseChannels(parentWaveSeg as SingleSequenceWaveSeg);
                    if (!channels.Contains(item.Key))
                    {
                        l.Background = new SolidColorBrush(item.Value);
                        ParentData.AddSinglePulseChannel(parentWaveSeg as SingleSequenceWaveSeg, item.Key);
                    }
                    else
                    {
                        l.Background = new SolidColorBrush(ChannelUnSelectedColor);
                        ParentData.DeleteSinglePulseChannel(parentWaveSeg as SingleSequenceWaveSeg, item.Key);
                    }
                });
                SingleSequenceChannelPanel.Children.Add(l);
            }
        }

        private void SelectSequenceName(object sender, RoutedEventArgs e)
        {
            if (parentWaveSeg == null) return;
            SequenceNameSelectionEvent?.Invoke(this);
            UpdateDisplay(parentWaveSeg);
        }

        protected override void InnerUpdateDisplay(SequenceSegBase seg)
        {
            if (seg is SingleSequenceWaveSeg)
            {
                ValidateChannelTags();
                SingleSegPanel.Visibility = Visibility.Visible;
                MultiSegPanel.Visibility = Visibility.Hidden;

                var singleseg = seg as SingleSequenceWaveSeg;

                if (singleseg.PeakName == "")
                {
                    Sequencename.Text = "新序列";
                    IsTrigger.SetSelectStateWithoutTrigger(false);
                    WaveLength.Content = 0.ToString();
                }
                else
                {
                    Sequencename.Text = singleseg.PeakName;
                    IsTrigger.SetSelectStateWithoutTrigger(singleseg.IsTriggerCommand);
                    WaveLength.Content = singleseg.PeakSpan.ToString();
                }
            }
            if (seg is GroupSequenceWaveSeg)
            {
                SingleSegPanel.Visibility = Visibility.Hidden;
                MultiSegPanel.Visibility = Visibility.Visible;

                var groupseg = seg as GroupSequenceWaveSeg;
                if (groupseg.PeakName == "")
                {
                    SequenceGroupname.Text = "新序列组合";
                }
                else
                {
                    SequenceGroupname.Text = groupseg.PeakName;
                }
            }
        }

        private void TriggerSelected(object sender, RoutedEventArgs e)
        {
            if (parentWaveSeg == null || !(parentWaveSeg is SingleSequenceWaveSeg)) return;
            (parentWaveSeg as SingleSequenceWaveSeg).IsTriggerCommand = IsTrigger.IsSelected;
            UpdateDisplay(ParentWaveSeg);
        }

        private void SelectSequenceGroupName(object sender, RoutedEventArgs e)
        {
            if (parentWaveSeg == null) return;
            GroupNameSelectionEvent?.Invoke(this);
            UpdateDisplay(parentWaveSeg);
        }

        private void SetChannelRelation(object sender, RoutedEventArgs e)
        {
            if (parentWaveSeg == null) return;
            ChannelRelationSetEvent?.Invoke(this);
            UpdateDisplay(parentWaveSeg);
        }
    }
}
