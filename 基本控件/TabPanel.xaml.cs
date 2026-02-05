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

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// TabViewer.xaml 的交互逻辑
    /// </summary>
    public partial class TabPanel : Grid
    {
        /// <summary>
        /// 导航栏点击事件
        /// </summary>
        public event Action<Tuple<string, string, object>> TabClicked = null;

        public TabPanel()
        {
            InitializeComponent();
        }

        private Panel content = null;
        public Panel ContentPanel
        {
            get
            {
                return content;
            }
            set
            {
                if (content != null)
                    Children.Remove(content);
                content = value;
                Children.Add(value);
                SetRow(value, 1);
                SetColumn(value, 0);
                SetColumnSpan(value, 2);
            }
        }

        List<Tuple<string, string, object>> tabPair = new List<Tuple<string, string, object>>();

        /// <summary>
        /// 添加实验导航栏
        /// </summary>
        public void AddTabElement(string tabName, string tooltip, object tabTag)
        {
            if (tabPair.Where((x) => x.Item1 == tabName).Count() == 0)
                tabPair.Add(new Tuple<string, string, object>(tabName, tooltip, tabTag));
            UpdateTabs();
        }

        public void RemoveTabElement(string tabName)
        {
            try
            {
                tabPair.Remove(tabPair.Where((x) => x.Item1 == tabName).ElementAt(0));
                UpdateTabs();
            }
            catch (Exception)
            {
            }
        }

        public void ChangeTabElement(string tabname, Tuple<string, string, object> newele)
        {
            try
            {
                int ind= tabPair.IndexOf(tabPair.Where((x) => x.Item1 == tabname).ElementAt(0));
                tabPair[ind] = newele;
                UpdateTabs();
            }
            catch (Exception)
            {
            }
        }

        public void ClearTabElement()
        {
            tabPair.Clear();
            UpdateTabs();
        }

        private int DisplayNumber = 0;

        public void UpdateTabs()
        {
            TabBar.Children.Clear();
            //计算需要显示多少个内容
            int number = (int)(TabBar.ActualWidth / 150);
            for (int i = 0; i < Math.Min(number, tabPair.Count); i++)
            {
                TabBar.Children.Add(CreateTab(tabPair[i]));
            }
            DisplayNumber = number;
        }

        private Grid CreateTab(Tuple<string, string, object> pair)
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
            l.Content = pair.Item1;
            g.ToolTip = pair.Item2;
            #endregion

            l.Tag = g;
            btn.Tag = g;
            g.Tag = pair;

            ControlEventHelper h = new ControlEventHelper(l);
            h.Click += SelectExpAndOpen;
            btn.Click += CloseTab;

            return g;
        }

        private void CloseTab(object sender, RoutedEventArgs e)
        {
            PopList.CancelPop();
            tabPair.Remove((Tuple<string, string, object>)((sender as DecoratedButton).Tag as Grid).Tag);
            UpdateTabs();
        }

        private void SelectExpAndOpen(object sender, MouseButtonEventArgs e)
        {
            PopList.CancelPop();
            var ele = (Tuple<string, string, object>)((sender as Label).Tag as Grid).Tag;
            TabClicked?.Invoke(ele);
            foreach (var g in TabBar.Children)
            {
                if ((g as Grid).Tag == ele)
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
            for (int i = DisplayNumber; i < tabPair.Count; i++)
            {
                Grid g = CreateTab(tabPair[i]);
                g.Width = 300;
                TabStorePanel.Children.Add(g);
            }
            PopScroll.Height = TabStorePanel.Children.Count * 35 + 4;
        }
    }
}
