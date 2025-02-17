using ODMR_Lab.基本窗口;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace ODMR_Lab
{
    public abstract class PageBase : Grid
    {
        /// <summary>
        /// 初始化步骤
        /// </summary>
        public void Init()
        {
            DisplayWindow = new EmptyWindow(PageName, this);
            InnerInit();
        }

        public abstract void InnerInit();

        /// <summary>
        /// 当程序停止时需要进行的操作
        /// </summary>
        public abstract void CloseBehaviour();

        /// <summary>
        /// 更新UI参数到后台
        /// </summary>
        public abstract void UpdateParam();


        public abstract string PageName { get; set; }

        public EmptyWindow DisplayWindow = null;

        private bool isDisplayedInPage = true;
        public bool IsDisplayedInPage
        {
            get
            {
                return isDisplayedInPage;
            }
            set
            {
                isDisplayedInPage = value;
                if (value)
                {
                    DisplayWindow.Hide();
                    DisplayWindow.ContentPanel.Children.Clear();
                    MainWindow.Handle.AddPageToView(this);
                }
                else
                {
                    MainWindow.Handle.DisplayInWindowBtn.Visibility = Visibility.Hidden;
                    MainWindow.Handle.PageContent.Children.Clear();
                    MainWindow.Handle.PageContent.Children.Add(new TextBlock()
                    {
                        Text = "正在独立窗口中显示...",
                        FontSize = 40,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    });
                    DisplayWindow.ContentPanel.Children.Add(this);
                    DisplayWindow.Show();
                    DisplayWindow.Topmost = true;
                    DisplayWindow.Topmost = false;
                }
            }
        }

    }
}
