using Controls;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.序列编辑器;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.实验部分.序列编辑器.子控件
{
    public abstract class ChannelSequenceControlBase : Grid
    {
        public abstract event Action<ChannelSequenceControlBase> SequenceNameSelectionEvent;

        public abstract event Action<ChannelSequenceControlBase> GroupNameSelectionEvent;

        public event Action ExternalUpdateEvent = null;

        protected static SequenceSegBase copyseg = null;

        protected static object segConfig = null;

        private bool isSelected = false;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                if (value)
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0090CB"));
                }
                else
                {
                    Background = Brushes.Transparent;
                }
            }
        }

        protected SequenceSegBase parentWaveSeg = null;
        public SequenceSegBase ParentWaveSeg
        {
            get
            {
                return parentWaveSeg;
            }
        }

        protected Panel parentPanel = null;
        public Panel ParentPanel
        {
            get
            {
                return parentPanel;
            }
        }

        public DisplayPage ParentPage = null;


        protected void SetContextMenu()
        {
            MouseLeftButtonDown += Select;
            ContextMenu menu = new ContextMenu();
            DecoratedButton btn = new DecoratedButton() { Text = "删除波形" };
            UIUpdater.SetDefaultTemplate(btn);
            btn.Click += DeleteEvent;
            menu.Items.Add(btn);
            menu.ItemHeight = 30;
            btn = new DecoratedButton() { Text = "在上方插入" };
            UIUpdater.SetDefaultTemplate(btn);
            btn.Click += InsrtBeforeEvent;
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "在下方插入" };
            UIUpdater.SetDefaultTemplate(btn);
            btn.Click += InsrtAfterEvent;
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "复制波形" };
            UIUpdater.SetDefaultTemplate(btn);
            btn.Click += CopyEvent;
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "粘贴波形" };
            btn.Click += PasteEvent;
            UIUpdater.SetDefaultTemplate(btn);
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "切换为脉冲组合" };
            btn.Click += ToGroupPulse;
            UIUpdater.SetDefaultTemplate(btn);
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "切换为单脉冲" };
            btn.Click += ToSinglePulse;
            UIUpdater.SetDefaultTemplate(btn);
            menu.Items.Add(btn);
            menu.ApplyToControl(this);
        }

        private void ToSinglePulse(object sender, RoutedEventArgs e)
        {
            if (this is MultiChannelSequenceControl)
            {
                if (parentWaveSeg is SingleSequenceWaveSeg) return;
                int ind = parentWaveSeg.ParentChannel.Peaks.IndexOf(parentWaveSeg);
                parentWaveSeg = new SingleSequenceWaveSeg("", 0, WaveValues.Zero, parentWaveSeg.ParentChannel, false);
                UpdateDisplay();
                parentWaveSeg.ParentChannel.Peaks[ind] = parentWaveSeg;
            }
            else
            {

            }
        }

        private void ToGroupPulse(object sender, RoutedEventArgs e)
        {
            if (parentWaveSeg is GroupSequenceWaveSeg) return;
            if (this is MultiChannelSequenceControl)
            {
                int ind = parentWaveSeg.ParentChannel.Peaks.IndexOf(parentWaveSeg);
                parentWaveSeg = new GroupSequenceWaveSeg() { ParentChannel = parentWaveSeg.ParentChannel };
                UpdateDisplay();
                parentWaveSeg.ParentChannel.Peaks[ind] = parentWaveSeg;
            }
            if (this is SingleChannelSequenceControl)
            {
                int ind = (this as SingleChannelSequenceControl).ParentData.GetSegIndex(parentWaveSeg as SingleSequenceWaveSeg);
                var newseg = new GroupSequenceWaveSeg();
                (this as SingleChannelSequenceControl).ParentData.RemoveSegAt(ind);
                (this as SingleChannelSequenceControl).ParentData.InsertSeg(ind, newseg, new List<KeyValuePair<string, SequenceChannel>>());
                parentWaveSeg = newseg;
                UpdateDisplay();
            }
        }

        private void Select(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in ParentPanel.Children)
            {
                if (item is ChannelSequenceControlBase)
                {
                    (item as ChannelSequenceControlBase).IsSelected = false;
                }
            }
            IsSelected = true;
        }

        private void InsrtBeforeEvent(object sender, RoutedEventArgs e)
        {
            var seg = ParentWaveSeg;
            int index = ParentPanel.Children.IndexOf(this);
            var waveseg = new SingleSequenceWaveSeg("newseg", 0, WaveValues.Zero, seg.ParentChannel);
            if (this is MultiChannelSequenceControl)
            {
                MultiChannelSequenceControl c = new MultiChannelSequenceControl();
                c.ParentPage = ParentPage;
                c.SetParentWaveSeg(waveseg, ParentPanel);
                c.ExternalUpdateEvent = ExternalUpdateEvent;
                c.ParentPage.AppendMultiChannelPopEvent(c);
                seg.ParentChannel.Peaks.Insert(index < 0 ? 0 : index, waveseg);
                ParentPanel.Children.Insert(index < 0 ? 0 : index, c);
            }
            if (this is SingleChannelSequenceControl)
            {
                SingleChannelSequenceControl c = new SingleChannelSequenceControl(waveseg, (this as SingleChannelSequenceControl).ParentData);
                c.ParentPage = ParentPage;
                (this as SingleChannelSequenceControl).ParentData.InsertSeg(index < 0 ? 0 : index, waveseg, new List<SequenceChannel>());
                c.SetParentWaveSeg(waveseg, ParentPanel);
                c.ExternalUpdateEvent = ExternalUpdateEvent;
                c.ParentPage.AppendSingleChannelPopEvent(c);
                ParentPanel.Children.Insert(index < 0 ? 0 : index, c);
            }
        }

        private void InsrtAfterEvent(object sender, RoutedEventArgs e)
        {
            var seg = ParentWaveSeg;
            int index = ParentPanel.Children.IndexOf(this);
            var waveseg = new SingleSequenceWaveSeg("newseg", 0, WaveValues.Zero, seg.ParentChannel);
            if (this is MultiChannelSequenceControl)
            {
                MultiChannelSequenceControl c = new MultiChannelSequenceControl();
                c.ParentPage = ParentPage;
                c.ParentPage.AppendMultiChannelPopEvent(c);
                c.SetParentWaveSeg(waveseg, ParentPanel);
                c.ExternalUpdateEvent = ExternalUpdateEvent;
                seg.ParentChannel.Peaks.Insert(index + 1, waveseg);
                ParentPanel.Children.Insert(index + 1, c);
            }
            if (this is SingleChannelSequenceControl)
            {
                SingleChannelSequenceControl c = new SingleChannelSequenceControl(waveseg, (this as SingleChannelSequenceControl).ParentData);
                c.ParentPage = ParentPage;
                c.ParentPage.AppendSingleChannelPopEvent(c);
                (this as SingleChannelSequenceControl).ParentData.InsertSeg(index + 1, waveseg, new List<SequenceChannel>());
                c.SetParentWaveSeg(waveseg, ParentPanel);
                c.ExternalUpdateEvent = ExternalUpdateEvent;
                ParentPanel.Children.Insert(index + 1, c);
            }
        }

        private void CopyEvent(object sender, RoutedEventArgs e)
        {
            copyseg = ParentWaveSeg;
            if (this is SingleChannelSequenceControl)
            {
                if (ParentWaveSeg is SingleSequenceWaveSeg)
                    segConfig = (this as SingleChannelSequenceControl).ParentData.GetSinglePulseChannels(ParentWaveSeg as SingleSequenceWaveSeg);
                if (ParentWaveSeg is GroupSequenceWaveSeg)
                    segConfig = (this as SingleChannelSequenceControl).ParentData.GetGroupPulseChannelRelations(ParentWaveSeg as GroupSequenceWaveSeg);
            }
            else
            {
                segConfig = null;
            }
        }

        private void PasteEvent(object sender, RoutedEventArgs e)
        {
            if (copyseg == null) return;
            var seg = copyseg.Copy();
            int index = ParentPanel.Children.IndexOf(this);
            if (this is MultiChannelSequenceControl)
            {
                MultiChannelSequenceControl c = new MultiChannelSequenceControl();
                c.ParentPage = ParentPage;
                c.ParentPage.AppendMultiChannelPopEvent(c);
                c.SetParentWaveSeg(seg, ParentPanel);
                c.ExternalUpdateEvent = ExternalUpdateEvent;
                seg.ParentChannel.Peaks.Insert(index + 1, seg);
                ParentPanel.Children.Insert(index + 1, c);
            }
            if (this is SingleChannelSequenceControl)
            {
                SingleChannelSequenceControl c = new SingleChannelSequenceControl(seg, (this as SingleChannelSequenceControl).ParentData);
                c.ParentPage = ParentPage;
                c.ParentPage.AppendSingleChannelPopEvent(c);
                object config = null;
                if (segConfig != null)
                {
                    if (segConfig is List<SequenceChannel>)
                    {
                        config = (segConfig as List<SequenceChannel>).Select(x => x).ToList();
                    }
                    if (segConfig is List<KeyValuePair<string, SequenceChannel>>)
                    {
                        config = (segConfig as List<KeyValuePair<string, SequenceChannel>>).Select(x => new KeyValuePair<string, SequenceChannel>(x.Key, x.Value)).ToList();
                    }
                }
                if (seg is SingleSequenceWaveSeg)
                    (this as SingleChannelSequenceControl).ParentData.InsertSeg(index + 1, seg as SingleSequenceWaveSeg, config == null ? new List<SequenceChannel>() : config as List<SequenceChannel>);
                if (seg is GroupSequenceWaveSeg)
                    (this as SingleChannelSequenceControl).ParentData.InsertSeg(index + 1, seg as GroupSequenceWaveSeg, config == null ? new List<KeyValuePair<string, SequenceChannel>>() : config as List<KeyValuePair<string, SequenceChannel>>);
                c.SetParentWaveSeg(seg, ParentPanel);
                c.ExternalUpdateEvent = ExternalUpdateEvent;
                ParentPanel.Children.Insert(index + 1, c);
            }
        }

        private void DeleteEvent(object sender, RoutedEventArgs e)
        {
            var seg = ParentWaveSeg;
            if (this is MultiChannelSequenceControl)
            {
                seg.ParentChannel.Peaks.Remove(seg);
                ParentPanel.Children.Remove(this);
            }
            if (this is SingleChannelSequenceControl)
            {
                (this as SingleChannelSequenceControl).ParentData.RemoveSeg(seg);
                ParentPanel.Children.Remove(this);
            }
        }

        public void SetParentWaveSeg(SequenceSegBase seg, Panel parentPanel)
        {
            parentWaveSeg = seg;
            this.parentPanel = parentPanel;
            //刷新显示
            UpdateDisplay(seg);
        }

        protected abstract void InnerUpdateDisplay(SequenceSegBase seg);

        protected void UpdateDisplay(SequenceSegBase seg)
        {
            InnerUpdateDisplay(seg);
            ExternalUpdateEvent?.Invoke();
        }

        public void UpdateDisplay()
        {
            UpdateDisplay(parentWaveSeg);
        }
    }
}
