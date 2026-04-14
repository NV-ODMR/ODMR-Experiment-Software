using CodeHelper;
using Controls.Windows;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
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
using System.Windows.Shapes;
using static HardWares.示波器.汉泰.OscilloScope;

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// GlobalSequenceWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GroupChannelEditWindow : Window
    {
        GroupSequenceWaveSeg parentSeg = null;
        List<SequenceChannel> Channels = null;

        public bool IsChanged = false;


        public GroupChannelEditWindow(GroupSequenceWaveSeg seg, List<SequenceChannel> channels)
        {
            InitializeComponent();

            parentSeg = seg;
            Channels = channels;

            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterCloseWindow(this, null, null, CloseBtn, null, 4, 30);
            UpdateParams();
        }

        private void UpdateParams()
        {
            for (int i = 0; i < Channels.Count; i++)
            {
                ChannelPanel.AddItem(null, Channels[i].ToString(), parentSeg.GroupCollection[i].Key);
            }
        }

        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Confirm(object sender, RoutedEventArgs e)
        {
            List<string> strs = new List<string>();
            //检查防止重名
            for (int i = 0; i < ChannelPanel.GetRowCount(); i++)
            {
                strs.Add(ChannelPanel.GetCellValue(i, 1) as string);
            }
            if (strs.Count != strs.ToHashSet().Count)
            {
                MessageWindow.ShowTipWindow("通道名称不能重复", this);
                return;
            }
            if (strs.Contains(""))
            {
                MessageWindow.ShowTipWindow("通道名称不能为空", this);
                return;
            }
            for (int i = 0; i < strs.Count; i++)
            {
                parentSeg.GroupCollection[i] = new KeyValuePair<string, List<SingleSequenceWaveSeg>>(strs[i], parentSeg.GroupCollection[i].Value);
            }
            if (GroupName.Text == "")
            {
                throw new Exception("脉冲组合名不能为空");
            }
            parentSeg.PeakName = GroupName.Text;
            IsChanged = true;
            Close();
        }
    }
}
