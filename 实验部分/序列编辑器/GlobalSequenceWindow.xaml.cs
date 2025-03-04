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

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// GlobalSequenceWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GlobalSequenceWindow : Window
    {
        public GlobalSequenceWindow()
        {
            InitializeComponent();
            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterWindow(this, null, null, null, 0, 30);
            UpdateParams();
        }

        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Apply(object sender, RoutedEventArgs e)
        {
            //检查是否重名
            if (GlobalPulseParams.GlobalPulseConfigs.Where(x => x.PulseName == WaveName.Text).Count() != 0)
            {
                MessageWindow.ShowTipWindow("存在重名波形,无法添加", this);
                return;
            }
            try
            {
                GlobalPulseParams.GlobalPulseConfigs.Add(new GlobalPulseParams(WaveName.Text, int.Parse(WaveTime.Text), IsLocked.IsSelected));
                GlobalPulseParams.WriteToFile();
                WaveName.Text = "";
                WaveTime.Text = "";
                IsLocked.IsSelected = false;
                UpdateParams();
            }
            catch (Exception)
            {
                return;
            }
        }

        private void UpdateParams()
        {
            PulsesPanel.ClearItems();
            foreach (var item in GlobalPulseParams.GlobalPulseConfigs)
            {
                PulsesPanel.AddItem(item, item.PulseName, item.PulseLength);
            }
        }

        private void PulsesPanel_ItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            if (MessageBoxResult.Yes == MessageWindow.ShowMessageBox("提示", "确定要删除吗?", MessageBoxButton.YesNo, owner: this))
            {
                if ((arg3 as GlobalPulseParams).IsLocked)
                {
                    if (MessageBoxResult.Yes == MessageWindow.ShowMessageBox("提示", "删除此序列会导致部分实验功能不可用，是否继续?", MessageBoxButton.YesNo, owner: this))
                    {
                        GlobalPulseParams.GlobalPulseConfigs.Remove(arg3 as GlobalPulseParams);
                        GlobalPulseParams.WriteToFile();
                    }
                }
                else
                {
                    GlobalPulseParams.GlobalPulseConfigs.Remove(arg3 as GlobalPulseParams);
                    GlobalPulseParams.WriteToFile();
                }
            }
            UpdateParams();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
