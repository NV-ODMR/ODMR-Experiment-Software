using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.实验部分.序列编辑器.子控件;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Path = System.IO.Path;

namespace ODMR_Lab.序列编辑器
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public override string PageName { get; set; } = "序列编辑器";
        public DisplayPage()
        {
            InitializeComponent();
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }

        #region 操作
        private void ResizeMultiPlot(object sender, RoutedEventArgs e)
        {
            chart.RefreshPlotWithAutoScale();
        }

        private void MultiSnap(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetImage(SnapHelper.GetControlSnap(chart));
                TimeWindow window = new TimeWindow();
                window.Owner = Window.GetWindow(this);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowWindow("截图已复制到剪切板");
            }
            catch (Exception)
            {
            }
        }

        private void ResizeSinglePlot(object sender, RoutedEventArgs e)
        {
            RefreshSingleChannelPlot();
        }

        private void SnapSinglePlot(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetImage(SnapHelper.GetControlSnap(singlechart));
                TimeWindow window = new TimeWindow();
                window.Owner = Window.GetWindow(this);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowWindow("截图已复制到剪切板");
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region 序列保存，读取和新建
        private void NewSequence(object sender, RoutedEventArgs e)
        {
            Sequence = new SequenceDataAssemble() { Name = "Newseq" };
            SingleChannelSequence = new SingleChannelData();
            SingleChannelSequence.ReadFromSequenceAssemble(Sequence);
            UpdateSequenceData();
            OneChannelSequencePannel.Visibility = Visibility.Visible;
            MultiChannelSequencePannel.Visibility = Visibility.Hidden;
            TipPanel.Visibility = Visibility.Hidden;
        }

        private void SaveSequence(object sender, RoutedEventArgs e)
        {
            if (TipPanel.Visibility == Visibility.Visible) return;
            try
            {
                string sequencename = "";
                if (MultiChannelSequencePannel.Visibility == Visibility.Visible)
                {
                    if (MultiSequenceName.Text == "" || MultiSequenceName.Text == null) throw new Exception("序列名不能为空");
                    sequencename = MultiSequenceName.Text;
                }
                if (OneChannelSequencePannel.Visibility == Visibility.Visible)
                {
                    if (SingleSequenceName.Text == "" || SingleSequenceName.Text == null) throw new Exception("序列名不能为空");
                    sequencename = SingleSequenceName.Text;
                }

                //检查是否已存在同名文件
                DirectoryInfo info = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequences"));
                var files = info.GetFiles("*", SearchOption.AllDirectories);
                string path = "";
                foreach (var item in files)
                {
                    try
                    {
                        var dic = FileObject.ReadDescription(item.FullName);
                        if (dic.Keys.Contains("SequenceAssembleName") && dic["SequenceAssembleName"] == sequencename)
                        {
                            if (MessageWindow.ShowMessageBox("提示", "已存在同名序列，是否覆盖?", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.No)
                            {
                                return;
                            }
                            path = item.FullName;
                            break;
                        }
                    }
                    catch (Exception) { }
                }
                //读取参数
                SequenceDataAssemble a = new SequenceDataAssemble();
                //多通道模式
                if (MultiChannelSequencePannel.Visibility == Visibility.Visible)
                {
                    a.Name = MultiSequenceName.Text;
                    foreach (var item in Sequence.Channels)
                    {
                        a.Channels.Add(item);
                    }
                }
                //单通道模式
                if (OneChannelSequencePannel.Visibility == Visibility.Visible)
                {
                    a = SingleChannelSequence.GenerateSequenceAssemble();
                    a.Name = SingleSequenceName.Text;
                }
                a.WriteToFile(path);

                TipPanel.Visibility = Visibility.Visible;
                MultiChannelSequencePannel.Visibility = Visibility.Hidden;
                OneChannelSequencePannel.Visibility = Visibility.Hidden;

                TimeWindow win = new TimeWindow();
                win.Owner = Window.GetWindow(this);
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.ShowWindow("序列已保存");
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("保存未完成:" + ex.Message, Window.GetWindow(this));
            }
        }

        private void OpenSequence(object sender, RoutedEventArgs e)
        {
            //保存到目标文件夹
            DirectoryInfo info = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequences"));
            var files = info.GetFiles("*", SearchOption.AllDirectories);
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            foreach (var item in files)
            {
                try
                {
                    var dic = FileObject.ReadDescription(item.FullName);
                    if (dic.Keys.Contains("SequenceAssembleName"))
                    {
                        values.Add(new KeyValuePair<string, string>(dic["SequenceAssembleName"], item.FullName));
                    }
                }
                catch (Exception) { }
            }
            SequenceFileWindow window = new SequenceFileWindow(values);
            window.Owner = Window.GetWindow(this);
            string path = window.ShowDialog();
            if (path == "") return;
            try
            {
                Sequence = SequenceDataAssemble.ReadFromFile(FileObject.ReadFromFile(path));
                Sequence.UpdateGlobalPulsesLength();
                SingleChannelSequence = new SingleChannelData();
                SingleChannelSequence.ReadFromSequenceAssemble(Sequence);
                UpdateSequenceData();
                SequenceFileName.Content = Sequence.Name;
                TipPanel.Visibility = Visibility.Hidden;
                MultiChannelSequencePannel.Visibility = Visibility.Hidden;
                OneChannelSequencePannel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("序列未成功打开，原因:" + ex.Message, Window.GetWindow(this));
            }
        }

        private void OpenGroup(object sender, RoutedEventArgs e)
        {
            //保存到目标文件夹
            DirectoryInfo info = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "SequenceGroup"));
            var files = info.GetFiles("*", SearchOption.AllDirectories);
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            foreach (var item in files)
            {
                try
                {
                    var dic = FileObject.ReadDescription(item.FullName);
                    if (dic.Keys.Contains("GroupName"))
                    {
                        values.Add(new KeyValuePair<string, string>(dic["GroupName"], item.FullName));
                    }
                }
                catch (Exception) { }
            }
            SequenceFileWindow window = new SequenceFileWindow(values);
            window.Owner = Window.GetWindow(this);
            string path = window.ShowDialog();
            if (path == "") return;
            try
            {
                GroupSequenceWaveSeg seg = new GroupSequenceWaveSeg();
                seg.ReadFromFile(values.Where(x => x.Value == path).ElementAt(0).Key);
                Sequence = seg.ConvertToAssemble();
                Sequence.UpdateGlobalPulsesLength();
                SingleChannelSequence = new SingleChannelData();
                SingleChannelSequence.ReadFromSequenceAssemble(Sequence);
                UpdateSequenceData();
                SequenceFileName.Content = Sequence.Name;
                TipPanel.Visibility = Visibility.Hidden;
                MultiChannelSequencePannel.Visibility = Visibility.Hidden;
                OneChannelSequencePannel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("脉冲组合未成功打开，原因:" + ex.Message, Window.GetWindow(this));
            }
        }

        private void SaveGroup(object sender, RoutedEventArgs e)
        {
            if (TipPanel.Visibility == Visibility.Visible) return;
            try
            {
                //创建用于保存的序列组合
                GroupSequenceWaveSeg seg = new GroupSequenceWaveSeg();
                List<SequenceChannel> channels = new List<SequenceChannel>();
                if (MultiChannelSequencePannel.Visibility == Visibility.Visible)
                {
                    Sequence.CheckCommandFormat();
                    Sequence.UpdateGlobalPulsesLength(true);
                    foreach (var item in Sequence.Channels)
                    {
                        seg.GroupCollection.Add(new KeyValuePair<string, List<SingleSequenceWaveSeg>>("", SequenceChannelData.GetExpandedPeakArray(item.Peaks)));
                    }
                    channels = Sequence.Channels.Select(x => x.ChannelInd).ToList();
                }
                if (OneChannelSequencePannel.Visibility == Visibility.Visible)
                {
                    var sequence = SingleChannelSequence.GenerateSequenceAssemble();
                    sequence.CheckCommandFormat();
                    sequence.UpdateGlobalPulsesLength(true);
                    foreach (var item in sequence.Channels)
                    {
                        seg.GroupCollection.Add(new KeyValuePair<string, List<SingleSequenceWaveSeg>>("", SequenceChannelData.GetExpandedPeakArray(item.Peaks)));
                    }
                    channels = sequence.Channels.Select(x => x.ChannelInd).ToList();
                }
                GroupChannelEditWindow window = new GroupChannelEditWindow(seg, channels);
                window.Owner = Window.GetWindow(this);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
                if (window.IsChanged)
                {
                    seg.WriteToFile();
                    TimeWindow w = new TimeWindow();
                    w.Owner = Window.GetWindow(this);
                    w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    w.ShowWindow("保存成功");
                    UpdatePulsesGroupPanel();
                    TipPanel.Visibility = Visibility.Visible;
                    MultiChannelSequencePannel.Visibility = Visibility.Hidden;
                    OneChannelSequencePannel.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("保存未完成:" + ex.Message, Window.GetWindow(this));
            }
        }

        private void OpenGlobalSeqPanel(object sender, RoutedEventArgs e)
        {
            GlobalSequenceWindow win = new GlobalSequenceWindow();
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.ShowDialog();
            UpdateGlobalPulsePanel();
            UpdatePulsesGroupPanel();
        }

        SequenceTestWindow seqWin = new SequenceTestWindow();
        /// <summary>
        /// 测试序列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SeuqenceTest(object sender, RoutedEventArgs e)
        {
            if (Sequence == null) return;
            seqWin.Show(Sequence);
        }
        #endregion

        #region 刷新全局脉冲选择面板

        public void UpdateGlobalContent()
        {
            Sequence.UpdateGlobalPulsesLength();
            SingleChannelSequence.UpdateGlobalPulsesLength();
            foreach (var item in MultiChannelWavePanel.Children)
            {
                if (item is MultiChannelSequenceControl)
                {
                    (item as MultiChannelSequenceControl).UpdateDisplay();
                }
            }
            foreach (var item in SingleChannelWavePanel.Children)
            {
                if (item is SingleChannelSequenceControl)
                {
                    (item as SingleChannelSequenceControl).UpdateDisplay();
                }
            }
        }

        public void UpdateGlobalPulsePanel()
        {
            GlobalPulsesPopPanel.Children.Clear();
            foreach (var item in GlobalPulseParams.GetGlobalPulses())
            {
                DecoratedButton btn = new DecoratedButton() { Text = item, Height = 30, Width = 200 };
                UIUpdater.SetDefaultTemplate(btn);
                btn.Click += new RoutedEventHandler((s, e) =>
                {
                    if (SequenceNamePop.Tag is ChannelSequenceControlBase)
                    {
                        ((SequenceNamePop.Tag as ChannelSequenceControlBase).ParentWaveSeg as SingleSequenceWaveSeg).PeakName = btn.Text;
                        ((SequenceNamePop.Tag as ChannelSequenceControlBase).ParentWaveSeg as SingleSequenceWaveSeg).PeakSpan = GlobalPulseParams.GetGlobalPulseLength(btn.Text);
                        SequenceNamePop.IsOpen = false;
                        UpdateGlobalContent();
                    }
                });
                GlobalPulsesPopPanel.Children.Add(btn);
            }
            GlobalPulsesPopPanel.UpdateLayout();
        }

        public void UpdatePulsesGroupPanel()
        {
            PulsesGroupPanel.Children.Clear();
            var names = GroupSequenceWaveSeg.EnumerateGroupNames();
            foreach (var item in names)
            {
                DecoratedButton btn = new DecoratedButton() { Text = item, Height = 30, Width = 200 };
                UIUpdater.SetDefaultTemplate(btn);
                btn.Click += new RoutedEventHandler((s, e) =>
                {
                    try
                    {
                        if (SequenceGroupNamePop.Tag is MultiChannelSequenceControl)
                        {
                            var seg = (SequenceGroupNamePop.Tag as ChannelSequenceControlBase).ParentWaveSeg as GroupSequenceWaveSeg;
                            seg.ReadFromFile(item);
                            seg.ParentChannel = (SequenceGroupNamePop.Tag as ChannelSequenceControlBase).ParentWaveSeg.ParentChannel;
                            seg.AttachToChannel(seg.ParentChannel);
                            (SequenceGroupNamePop.Tag as ChannelSequenceControlBase).SetParentWaveSeg(seg, MultiChannelWavePanel);
                            SequenceGroupNamePop.IsOpen = false;
                            UpdateGlobalContent();
                        }
                        if (SequenceGroupNamePop.Tag is SingleChannelSequenceControl)
                        {
                            var seg = (SequenceGroupNamePop.Tag as SingleChannelSequenceControl).ParentWaveSeg as GroupSequenceWaveSeg;
                            seg.ReadFromFile(item);
                            seg.ParentChannel = (SequenceGroupNamePop.Tag as SingleChannelSequenceControl).ParentWaveSeg.ParentChannel;
                            seg.AttachToChannel(seg.ParentChannel);
                            (SequenceGroupNamePop.Tag as SingleChannelSequenceControl).SetParentWaveSeg(seg, SingleChannelWavePanel);
                            SequenceGroupNamePop.IsOpen = false;
                            UpdateGlobalContent();
                        }
                    }
                    catch (Exception ex) { }
                });
                PulsesGroupPanel.Children.Add(btn);
            }
            PulsesGroupPanel.UpdateLayout();
        }

        public void UpdateGroutChannelPanel(GroupSequenceWaveSeg seg)
        {
            GroupChannelPanel.Children.Clear();
            foreach (var item in seg.GroupCollection)
            {
                DecoratedButton btn = new DecoratedButton() { Text = item.Key, Height = 30, Width = 200 };
                UIUpdater.SetDefaultTemplate(btn);
                btn.Click += new RoutedEventHandler((s, e) =>
                {
                    try
                    {
                        if (SequenceGroupChannelPop.Tag is ChannelSequenceControlBase)
                        {
                            var groupseg = (SequenceGroupChannelPop.Tag as ChannelSequenceControlBase).ParentWaveSeg as GroupSequenceWaveSeg;
                            groupseg.SelectChnnelInd = groupseg.GroupCollection.Select(x => x.Key).ToList().IndexOf(item.Key);
                            (SequenceGroupChannelPop.Tag as ChannelSequenceControlBase).UpdateDisplay();
                            SequenceGroupChannelPop.IsOpen = false;
                            UpdateGlobalContent();
                        }
                    }
                    catch (Exception ex) { }
                });
                GroupChannelPanel.Children.Add(btn);
            }
            GroupChannelPanel.UpdateLayout();
            scroll2.Height = GroupChannelPanel.ActualHeight;
        }
        #endregion

        private string GetChannelDescription(SequenceChannel ChannelInd)
        {
            string desc = "Undefined";
            return desc;
        }

        /// <summary>
        /// 多通道序列数据
        /// </summary>
        public SequenceDataAssemble Sequence { get; set; } = new SequenceDataAssemble();
        /// <summary>
        /// 单通道序列数据
        /// </summary>
        public SingleChannelData SingleChannelSequence { get; set; } = new SingleChannelData();

        public void UpdateSequenceData()
        {
            #region 多通道视图更新
            try
            {
                #region 刷新通道列表
                MultiChannelPanel.ClearItems();
                foreach (var item in Sequence.Channels)
                {

                    var desc = GetChannelDescription(item.ChannelInd);
                    MultiChannelPanel.AddItem(item, item.ChannelInd, desc, item.IsDisplay);
                }
                #endregion
                MultiChannelWavePanel.Children.Clear();
                MultiSequenceName.Text = Sequence.Name;
            }
            catch { }
            MultiSequenceName.Text = Sequence.Name;
            #endregion
            #region 单通道视图更新
            try
            {
                #region 刷新通道列表
                SingleChannelPanel.Children.Clear();
                foreach (var item in SingleChannelSequence.Channels)
                {
                    SingleChannelPanel.Children.Add(new SingleChannelBar(item.Key, item.Value, SingleChannelSequence, SingleChannelPanel, this) { Height = 30 });
                }
                SingleSequenceName.Text = Sequence.Name;
                #endregion
                #region 刷新脉冲列表
                SingleChannelWavePanel.Children.Clear();
                var segs = SingleChannelSequence.GetSequences();
                foreach (var item in segs)
                {
                    var control = new SingleChannelSequenceControl(item, SingleChannelSequence) { };
                    control.SetParentWaveSeg(item, SingleChannelWavePanel);
                    control.ParentPage = this;
                    AppendSingleChannelPopEvent(control);
                    SingleChannelWavePanel.Children.Add(control);
                }
                #endregion
            }
            catch { }
            #endregion

            RefreshMultiChannelPlot();
            UpdateGlobalPulsePanel();
            UpdatePulsesGroupPanel();
        }

        private void MultiChannelPanel_ItemSelected(int arg1, object arg2)
        {
            if (MultiChannelPanel.SelectedIndex != arg1 || MultiChannelWavePanel.Children.Count == 0)
            {
                MultiChannelWavePanel.Children.Clear();
                SequenceChannelData data = arg2 as SequenceChannelData;
                var pulses = GlobalPulseParams.GetGlobalPulses();
                foreach (var item in data.Peaks)
                {
                    MultiChannelSequenceControl c = new MultiChannelSequenceControl() { HorizontalAlignment = HorizontalAlignment.Stretch };
                    c.SetParentWaveSeg(item, MultiChannelWavePanel);
                    c.ParentPage = this;
                    c.ExternalUpdateEvent += RefreshMultiChannelPlot;
                    MultiChannelWavePanel.Children.Add(c);
                }
            }
        }

        private void RefreshMultiChannelPlot()
        {
            if (Sequence == null) return;

            List<int> SeqTimes = new List<int>();

            double loloc = double.NaN;
            double hiloc = double.NaN;

            SingleSequenceWaveSeg segbefore = null;
            SingleSequenceWaveSeg segafter = null;

            foreach (var item in MultiChannelWavePanel.Children)
            {
                if (item is ChannelSequenceControlBase)
                {
                    if ((item as ChannelSequenceControlBase).IsSelected)
                    {
                        var seg = (item as ChannelSequenceControlBase).ParentWaveSeg;
                        if (seg is SingleSequenceWaveSeg)
                        {
                            segbefore = seg as SingleSequenceWaveSeg;
                            segafter = seg as SingleSequenceWaveSeg;
                        }
                        if (seg is GroupSequenceWaveSeg)
                        {
                            try
                            {
                                segbefore = (seg as GroupSequenceWaveSeg).GetSelectedChannelSegs().First();
                                segafter = (seg as GroupSequenceWaveSeg).GetSelectedChannelSegs().Last();
                            }
                            catch (Exception)
                            {
                            }
                        }
                        break;
                    }
                }
            }

            foreach (var item in Sequence.Channels)
            {
                item.ChannelWaveData.Name = Enum.GetName(item.ChannelInd.GetType(), item.ChannelInd);
                item.ChannelWaveData.X.Clear();
                item.ChannelWaveData.Y.Clear();
                if (item.Peaks.Count == 0) continue;
                int timex = 0;
                var exppeaks = SequenceChannelData.GetExpandedPeakArray(item.Peaks);
                foreach (var peak in exppeaks)
                {
                    if (peak.WaveValue == WaveValues.Zero)
                    {
                        item.ChannelWaveData.X.Add(timex);
                        item.ChannelWaveData.Y.Add(0);
                        item.ChannelWaveData.X.Add(timex + peak.PeakSpan);
                        item.ChannelWaveData.Y.Add(0);
                    }
                    if (peak.WaveValue == WaveValues.One)
                    {
                        item.ChannelWaveData.X.Add(timex);
                        item.ChannelWaveData.Y.Add(1);
                        item.ChannelWaveData.X.Add(timex + peak.PeakSpan);
                        item.ChannelWaveData.Y.Add(1);
                    }
                    if (segbefore == peak)
                    {
                        loloc = timex;
                    }
                    if (segafter == peak)
                    {
                        hiloc = timex + peak.PeakSpan;
                    }
                    timex += peak.PeakSpan;
                }
                SeqTimes.Add(timex);
            }
            if (SeqTimes.Count == 0)
            {
                chart.DataList.Clear();
                chart.RefreshPlotWithAutoScale();
                return;
            }
            int maxtime = SeqTimes.Max();
            foreach (var item in Sequence.Channels)
            {
                if (item.ChannelWaveData.GetXCount() == 0) continue;
                if (item.ChannelWaveData.X.Last() <= maxtime)
                {
                    item.ChannelWaveData.X.Add(item.ChannelWaveData.X.Last());
                    item.ChannelWaveData.Y.Add(0);
                    item.ChannelWaveData.X.Add(maxtime);
                    item.ChannelWaveData.Y.Add(0);
                }
            }

            chart.DataList.Clear();
            //重新排列
            double ind = 0;
            foreach (var item in Sequence.Channels)
            {
                if (item.IsDisplay == false) continue;
                item.ChannelWaveData.Y = item.ChannelWaveData.Y.Select(x => x + ind).ToList();
                ind += 1.1;
                chart.DataList.Add(item.ChannelWaveData);
            }
            if (!double.IsNaN(loloc))
                chart.DataList.Add(new NumricDataSeries("", new List<double>() { loloc, loloc }, new List<double>() { 0, ind }) { LineColor = Colors.LightGreen, LineThickness = 1, MarkerSize = 0 });
            if (!double.IsNaN(hiloc))
                chart.DataList.Add(new NumricDataSeries("", new List<double>() { hiloc, hiloc }, new List<double>() { 0, ind }) { LineColor = Colors.LightGreen, LineThickness = 1, MarkerSize = 0 });
            if (!double.IsNaN(loloc) && !double.IsNaN(hiloc))
                chart.RefreshPlotWithCustomScale(loloc, hiloc);
        }

        #region 新建多通道波形和通道
        private void NewMultiChannel(object sender, RoutedEventArgs e)
        {
            Sequence.Channels.Add(new SequenceChannelData(SequenceChannel.None));
            UpdateSequenceData();
        }

        public void AppendMultiChannelPopEvent(MultiChannelSequenceControl c)
        {
            c.SequenceNameSelectionEvent += new Action<ChannelSequenceControlBase>((v) =>
            {
                SequenceNamePop.PlacementTarget = v;
                SequenceNamePop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                SequenceNamePop.IsOpen = true;
                scroll.Height = GlobalPulsesPopPanel.ActualHeight;
                SequenceNamePop.StaysOpen = false;
                SequenceNamePop.Tag = v;
            });
            c.GroupNameSelectionEvent += new Action<ChannelSequenceControlBase>((v) =>
            {
                SequenceGroupNamePop.PlacementTarget = v;
                SequenceGroupNamePop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                SequenceGroupNamePop.IsOpen = true;
                scroll1.Height = PulsesGroupPanel.ActualHeight;
                SequenceGroupNamePop.StaysOpen = false;
                SequenceGroupNamePop.Tag = v;
            });
            c.GroupChannelSelectionEvent += new Action<ChannelSequenceControlBase>((v) =>
            {
                UpdateGroutChannelPanel(v.ParentWaveSeg as GroupSequenceWaveSeg);
                SequenceGroupChannelPop.PlacementTarget = v;
                SequenceGroupChannelPop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                SequenceGroupChannelPop.IsOpen = true;
                SequenceGroupChannelPop.StaysOpen = false;
                SequenceGroupChannelPop.Tag = v;
            });
        }

        private void NewMultiChannelPeak(object sender, RoutedEventArgs e)
        {
            if (MultiChannelPanel.GetSelectedTag() == null) return;
            var channel = MultiChannelPanel.GetSelectedTag() as SequenceChannelData;
            SingleSequenceWaveSeg newseg = new SingleSequenceWaveSeg("", 0, WaveValues.Zero, channel);
            channel.Peaks.Add(newseg);
            MultiChannelSequenceControl control = new MultiChannelSequenceControl() { HorizontalAlignment = HorizontalAlignment.Stretch };
            control.ParentPage = this;
            control.SetParentWaveSeg(newseg, MultiChannelWavePanel);
            control.ExternalUpdateEvent += RefreshMultiChannelPlot;
            AppendMultiChannelPopEvent(control);
            MultiChannelWavePanel.Children.Add(control);
            RefreshMultiChannelPlot();
        }
        #endregion

        private void MultiChannelPanel_ItemValueChanged(int arg1, int arg2, object arg3)
        {
            try
            {
                var channel = MultiChannelPanel.GetTag(arg1) as SequenceChannelData;
                channel.ChannelInd = (SequenceChannel)Enum.Parse(typeof(SequenceChannel), MultiChannelPanel.GetCellValue(arg1, 0) as string);
                channel.IsDisplay = (bool)MultiChannelPanel.GetCellValue(arg1, 2);
                MultiChannelPanel.SetCelValue(arg1, 1, GetChannelDescription(channel.ChannelInd));
                RefreshMultiChannelPlot();
            }
            catch (Exception) { }
        }

        private void MultiChannelPanel_ItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            //删除
            if (arg1 == 0)
            {
                var seg = arg3 as SequenceChannelData;
                Sequence.Channels.Remove(seg);
                MultiChannelPanel.RemoveItemAt(arg2);
            }
        }

        #region 切换视图
        private void ChangeDisplayView(object sender, RoutedEventArgs e)
        {
            DecoratedButton btn = sender as DecoratedButton;
            if (btn.Text == "单通道视图")
            {
                OneChannelSequencePannel.Visibility = Visibility.Visible;
                MultiChannelSequencePannel.Visibility = Visibility.Hidden;
                MultiChannelBtn.KeepPressed = false;
            }
            if (btn.Text == "多通道视图")
            {
                OneChannelSequencePannel.Visibility = Visibility.Hidden;
                MultiChannelSequencePannel.Visibility = Visibility.Visible;
                OneChannelBtn.KeepPressed = false;
            }
            btn.KeepPressed = true;
        }
        #endregion

        #region 单通道视图
        ChannelIDSelectWindow window = new ChannelIDSelectWindow();

        private void NewSingleChannel(object sender, RoutedEventArgs e)
        {
            ///新建通道窗口
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show(SingleChannelSequence);
            window.AfterHideEvent = new Action(() =>
            {
                if (window.AddedChannel != SequenceChannel.None)
                {
                    SingleChannelSequence.AddChannel(window.AddedChannel);
                    UpdateSequenceData();
                }
            });
        }

        public void AppendSingleChannelPopEvent(SingleChannelSequenceControl c)
        {
            c.SequenceNameSelectionEvent += new Action<ChannelSequenceControlBase>((v) =>
            {
                SequenceNamePop.PlacementTarget = v;
                SequenceNamePop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                SequenceNamePop.IsOpen = true;
                scroll.Height = GlobalPulsesPopPanel.ActualHeight;
                SequenceNamePop.StaysOpen = false;
                SequenceNamePop.Tag = v;
            });
            c.GroupNameSelectionEvent += new Action<ChannelSequenceControlBase>((v) =>
            {
                SequenceGroupNamePop.PlacementTarget = v;
                SequenceGroupNamePop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                SequenceGroupNamePop.IsOpen = true;
                scroll1.Height = PulsesGroupPanel.ActualHeight;
                SequenceGroupNamePop.StaysOpen = false;
                SequenceGroupNamePop.Tag = v;
            });
            c.ChannelRelationSetEvent += new Action<ChannelSequenceControlBase>((v) =>
            {
                GroupChannelRelationPop.PlacementTarget = v;
                GroupChannelRelationPop.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                var group = c.ParentWaveSeg as GroupSequenceWaveSeg;
                var list = c.ParentData.GetGroupPulseChannelRelations(group);
                if (list.Count == 0)
                {
                    list = c.ParentData.Channels.Select(x => new KeyValuePair<string, SequenceChannel>("", x.Key)).ToList();
                }
                GroupChannelRelationPop.Show(c.ParentWaveSeg as GroupSequenceWaveSeg, SingleChannelSequence, list);
                SequenceGroupNamePop.StaysOpen = false;
                SequenceGroupNamePop.Tag = v;
            });
        }

        private void NewSinglePeak(object sender, RoutedEventArgs e)
        {
            if (SingleChannelSequence.Channels.Count == 0) return;
            SingleSequenceWaveSeg newseg = new SingleSequenceWaveSeg("新序列", 0, WaveValues.Zero, null, false);
            SingleChannelSequence.AddSeg(newseg, new List<SequenceChannel>());
            SingleChannelSequenceControl control = new SingleChannelSequenceControl(newseg, SingleChannelSequence);
            control.SetParentWaveSeg(newseg, SingleChannelWavePanel);
            control.ParentPage = this;
            control.ExternalUpdateEvent += RefreshMultiChannelPlot;
            AppendSingleChannelPopEvent(control);
            SingleChannelWavePanel.Children.Add(control);
            RefreshSingleChannelPlot();
        }

        private void RefreshSingleChannelPlot()
        {
            if (Sequence == null) return;

            List<int> SeqTimes = new List<int>();

            double loloc = double.NaN;
            double hiloc = double.NaN;

            SingleSequenceWaveSeg segbefore = null;
            SingleSequenceWaveSeg segafter = null;

            foreach (var item in SingleChannelWavePanel.Children)
            {
                if ((item as SingleChannelSequenceControl).IsSelected)
                {
                    var seg = (item as SingleChannelSequenceControl).ParentWaveSeg;
                    if (seg is SingleSequenceWaveSeg)
                    {
                        segbefore = seg as SingleSequenceWaveSeg;
                        segafter = seg as SingleSequenceWaveSeg;
                    }
                    if (seg is GroupSequenceWaveSeg)
                    {
                        try
                        {
                            segbefore = (seg as GroupSequenceWaveSeg).GetSelectedChannelSegs().First();
                            segafter = (seg as GroupSequenceWaveSeg).GetSelectedChannelSegs().Last();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    break;
                }
            }
            singlechart.DataList.Clear();
            SingleChannelSequence.UpdatePlotData();
            foreach (var item in SingleChannelSequence.ChannelWaveDatas)
            {
                singlechart.DataList.Add(item);
            }
            singlechart.RefreshPlotWithAutoScale();

            if (!double.IsNaN(loloc))
                singlechart.DataList.Add(new NumricDataSeries("", new List<double>() { loloc, loloc }, new List<double>() { 0, 1.1 * (SingleChannelSequence.ChannelWaveDatas.Count + 1) }) { LineColor = Colors.LightGreen, LineThickness = 2, MarkerSize = 0 });
            if (!double.IsNaN(hiloc))
                singlechart.DataList.Add(new NumricDataSeries("", new List<double>() { hiloc, hiloc }, new List<double>() { 0, 1.1 * (SingleChannelSequence.ChannelWaveDatas.Count + 1) }) { LineColor = Colors.LightGreen, LineThickness = 2, MarkerSize = 0 });
            if (!double.IsNaN(loloc) && !double.IsNaN(hiloc))
                singlechart.RefreshPlotWithCustomScale(loloc, hiloc);
        }

        #endregion
    }
}
