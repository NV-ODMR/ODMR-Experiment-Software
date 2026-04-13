using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares;
using HardWares.端口基类;
using HardWares.端口基类部分;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.实验部分.设备参数监测
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ParamSelectWindow : Window
    {
        Parameter SelectedParam = null;

        bool IsExit = true;

        public ParamSelectWindow()
        {
            InitializeComponent();
            WindowResizeHelper helper = new WindowResizeHelper();
            helper.RegisterCloseWindow(this, null, null, CloseBtn);
            helper.BeforeClose += new RoutedEventHandler((s, e) => { if (IsExit) { SelectedParam = null; } });
        }

        /// <summary>
        /// 显示窗口，返回选中的设备，如果未选择返回（null,null）,如果选中不带通道的设备则返回（设备，null）,如果选中通道则返回（设备，通道）
        /// </summary>
        /// <returns></returns>
        public Parameter ShowDialog(PortElement dev)
        {
            //获取设备列表
            var list = dev.AvailableParameterNames();
            ParamList.ClearItems();
            foreach (var item in list)
            {
                ParamList.AddItem(item, item.Description);
            }
            ShowDialog();
            return SelectedParam;
        }
        public Parameter ShowDialog(PortObject dev)
        {
            //获取设备列表
            var list = dev.AvailableParameterNames();
            ParamList.ClearItems();
            foreach (var item in list)
            {
                ParamList.AddItem(item, item.Description);
            }
            ShowDialog();
            return SelectedParam;
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            IsExit = false;
            Close();
        }

        private void ParamList_ItemSelected(int arg1, object arg2)
        {
            SelectedParam = arg2 as Parameter;
        }
    }
}
