using Controls;
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

namespace ODMR_Lab.实验部分.序列编辑器.子控件
{
    /// <summary>
    /// MultiChannel_SingleSequenceControl.xaml 的交互逻辑
    /// </summary>
    public partial class MultiChannelSequenceControl : ChannelSequenceControlBase
    {
        public override event Action<ChannelSequenceControlBase> SequenceNameSelectionEvent = null;

        public override event Action<ChannelSequenceControlBase> GroupNameSelectionEvent = null;

        public event Action<ChannelSequenceControlBase> GroupChannelSelectionEvent = null;

        public MultiChannelSequenceControl()
        {
            InitializeComponent();
            SetContextMenu();
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
                SingleSegPanel.Visibility = Visibility.Visible;
                MultiSegPanel.Visibility = Visibility.Hidden;

                var singleseg = seg as SingleSequenceWaveSeg;

                if (singleseg.PeakName == "")
                {
                    Sequencename.Text = "新序列";
                    WaveState.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D"));
                    IsTrigger.SetSelectStateWithoutTrigger(false);
                    WaveLength.Content = 0.ToString();
                }
                else
                {
                    Sequencename.Text = singleseg.PeakName;
                    WaveState.Background = singleseg.WaveValue == WaveValues.One ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00DC45")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D4D"));
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
                    GroupChannelName.Text = "待选";
                }
                else
                {
                    SequenceGroupname.Text = groupseg.PeakName;
                    GroupChannelName.Text = groupseg.GroupCollection[groupseg.SelectChnnelInd].Key;
                }
            }
        }

        private void SetWaveState(object sender, MouseButtonEventArgs e)
        {
            if (parentWaveSeg == null || !(parentWaveSeg is SingleSequenceWaveSeg)) return;
            (parentWaveSeg as SingleSequenceWaveSeg).WaveValue = (parentWaveSeg as SingleSequenceWaveSeg).WaveValue == WaveValues.One ? WaveValues.Zero : WaveValues.One;
            UpdateDisplay(ParentWaveSeg);
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

        private void SelectSequenceGroupChannel(object sender, RoutedEventArgs e)
        {
            if (parentWaveSeg == null) return;
            GroupChannelSelectionEvent?.Invoke(this);
            UpdateDisplay(parentWaveSeg);
        }
    }
}
