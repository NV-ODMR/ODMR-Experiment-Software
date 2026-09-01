using Controls.Windows;
using System.Windows;

namespace ODMR_Lab.Python管理器
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class ExtPage : PageBase
    {
        public override string PageName { get; set; } = "Python管理器";

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

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }
    }
}
