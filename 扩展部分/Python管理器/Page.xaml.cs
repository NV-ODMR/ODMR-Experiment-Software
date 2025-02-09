using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
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
using 温度监控程序.Windows;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.Python管理器
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class ExtPage : PageBase
    {

        public ExtPage()
        {
            InitializeComponent();
            Manager.DeleteConfirmMethod = DeleteConfirm;
        }

        private bool DeleteConfirm(string packagename)
        {
            if (MessageWindow.ShowMessageBox("提示", "确定要删除包" + packagename + "吗?", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
            { return true; }
            return false;
        }

        public override void Init()
        {
        }

        public override void CloseBehaviour()
        {
        }
    }
}
