using Controls;
using ODMR_Lab.实验部分.ODMR实验;
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

namespace ODMR_Lab.ODMR实验
{
    /// <summary>
    /// ExpSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExpSelectWindow : Window
    {
        private ODMRExpObject Exp = null;

        public ExpSelectWindow(DisplayPage parentPage)
        {
            InitializeComponent();
            ParentPage = parentPage;
        }

        DisplayPage ParentPage = null;

        public ODMRExpObject ShowDialog()
        {
            //刷新显示
            HashSet<string> gnames = new HashSet<string>(ParentPage.ExpObjects.Select(x => x.ODMRExperimentGroupName));
            ExpGroup.Items.Clear();
            ExpGroup.TemplateButton = ExpGroup;
            foreach (var item in gnames)
            {
                ExpGroup.Items.Add(new DecoratedButton() { Text = item, Height = 50 });
            }
            ExpGroup.Select(0);
            base.ShowDialog();
            return Exp;
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Exp = null;
            Close();
        }

        private void ExpGroupChanged(object sender, RoutedEventArgs e)
        {
            if (ExpGroup.SelectedItem == null) return;
            //刷新实验
            var li = ParentPage.ExpObjects.Where(x => x.ODMRExperimentGroupName == ExpGroup.SelectedItem.Text);
            ExpNames.ClearItems();
            foreach (var item in li)
            {
                ExpNames.AddItem(item, item.ODMRExperimentName);
            }
        }

        private void SelectExp(object sender, RoutedEventArgs e)
        {
            if (ExpNames.GetSelectedTag() != null)
                Exp = ExpNames.GetSelectedTag() as ODMRExpObject;
            else
                Exp = null;
            Close();
        }
    }
}
