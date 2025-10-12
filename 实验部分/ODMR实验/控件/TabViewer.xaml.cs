using CodeHelper;
using Controls;
using ODMR_Lab.ODMR实验;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace ODMR_Lab.实验部分.ODMR实验.控件
{
    /// <summary>
    /// TabViewer.xaml 的交互逻辑
    /// </summary>
    public partial class TabViewer : Grid
    {
        public TabViewer()
        {
            InitializeComponent();
        }

        private List<ODMRExpObject> Exps { get; set; } = new List<ODMRExpObject>();

        public DisplayPage ParentPage { get; set; } = null;

        /// <summary>
        /// 添加实验导航栏
        /// </summary>
        public void AddExp(ODMRExpObject exp)
        {
            if (Exps.IndexOf(exp) != -1)
            {
                SelectExpWithoutOpen(exp);
            }
            else
            {
                Exps.Insert(0, exp);
                UpdateTabs();
            }
        }

        private int DisplayNumber = 0;

        public void UpdateTabs()
        {
            TabPanel.Children.Clear();
            //计算需要显示多少个实验
            int number = (int)(TabPanel.ActualWidth / 150);
            for (int i = 0; i < Math.Min(number, Exps.Count); i++)
            {
                TabPanel.Children.Add(CreateTab(Exps[i]));
            }
            DisplayNumber = number;
        }

        private Grid CreateTab(ODMRExpObject exp)
        {
            #region 创建标签卡
            Grid g = new Grid();
            g.Width = 150;
            g.Height = 35;
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(35) });
            DecoratedButton btn = new DecoratedButton();
            ButtonTemplate.CloneStyleTo(btn);
            btn.Text = "×";
            g.Children.Add(btn);
            Grid.SetColumn(btn, 1);
            Label l = new Label();
            UIUpdater.CloneStyle(LabelTemplate, l);
            g.Children.Add(l);
            Grid.SetColumn(l, 0);
            l.Content = exp.ODMRExperimentName;
            g.ToolTip = exp.ODMRExperimentName + "  " + exp.ODMRExperimentGroupName;
            #endregion

            l.Tag = g;
            btn.Tag = g;
            g.Tag = exp;

            ControlEventHelper h = new ControlEventHelper(l);
            h.Click += SelectExpAndOpen;
            btn.Click += CloseTab;

            return g;
        }

        private void CloseTab(object sender, RoutedEventArgs e)
        {
            PopList.CancelPop();
            Exps.Remove(((sender as DecoratedButton).Tag as Grid).Tag as ODMRExpObject);
            UpdateTabs();
        }

        private void SelectExpAndOpen(object sender, MouseButtonEventArgs e)
        {
            PopList.CancelPop();
            ODMRExpObject exp = ((sender as Label).Tag as Grid).Tag as ODMRExpObject;
            ParentPage?.SelectExp(ParentPage.ExpObjects.IndexOf(exp));
        }

        private void SelectExpWithoutOpen(ODMRExpObject exp)
        {
            PopList.CancelPop();
            int ind = Exps.IndexOf(exp);
            if (ind >= DisplayNumber)
            {
                //不在显示列表中
                Exps.Remove(exp);
                Exps.Insert(0, exp);
                UpdateTabs();
            }
            foreach (var g in TabPanel.Children)
            {
                if ((g as Grid).Tag as ODMRExpObject == exp)
                {
                    (g as Grid).Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1792E5"));
                }
                else
                {
                    (g as Grid).Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2E2E2E"));
                }
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTabs();
        }

        private void PopButton_PreviewPopOpen(object sender, RoutedEventArgs e)
        {
            TabStorePanel.Children.Clear();
            for (int i = DisplayNumber; i < Exps.Count; i++)
            {
                Grid g = CreateTab(Exps[i]);
                g.Width = 300;
                TabStorePanel.Children.Add(g);
            }
            PopScroll.Height = TabStorePanel.Children.Count * 35 + 4;
        }
    }
}
