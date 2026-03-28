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

namespace ODMR_Lab.实验部分.序列编辑器.子控件
{
    /// <summary>
    /// GroupRelationSelectionPanel.xaml 的交互逻辑
    /// </summary>
    public partial class GroupRelationSelectionPop : Popup
    {
        public Action<GroupSequenceWaveSeg, List<KeyValuePair<string, SequenceChannel>>> PopCloseAction = null;

        public GroupSequenceWaveSeg parentseg = null;

        public SingleChannelData ParentData = null;

        public GroupRelationSelectionPop()
        {
            InitializeComponent();
        }

        public void Show(GroupSequenceWaveSeg seg, SingleChannelData parentData, List<KeyValuePair<string, SequenceChannel>> relations)
        {
            parentseg = seg;
            ParentData = parentData;
            //设置relations
            panel.ClearItems();
            var strs = seg.GroupCollection.Select(x => x.Key).ToList();
            foreach (var item in relations)
            {
                panel.AddItem(null, item.Value.ToString(), strs);
                panel.SetCelValue(panel.GetRowCount() - 1, 1, item.Key);
            }
            IsOpen = true;
            StaysOpen = false;
        }

        private void Popup_Closed(object sender, EventArgs e)
        {
            List<KeyValuePair<string, SequenceChannel>> relations = new List<KeyValuePair<string, SequenceChannel>>();
            //判断修改内容是否合法
            int rows = panel.GetRowCount();
            var strs = parentseg.GroupCollection.Select(x => x.Key).ToList();
            for (int i = 0; i < rows; i++)
            {
                SequenceChannel cahnnelind = (SequenceChannel)Enum.Parse(typeof(SequenceChannel), panel.GetCellValue(i, 0) as string);
                relations.Add(new KeyValuePair<string, SequenceChannel>(panel.GetCellValue(i, 1) as string, cahnnelind));
            }
            ParentData.SetGroupPulseChannelRelations(parentseg, relations);
            ParentData.UpdateGlobalPulsesLength();
            IsOpen = false;
        }
    }
}
