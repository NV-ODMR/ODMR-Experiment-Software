using CodeHelper;
using ODMR_Lab.基本控件;
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

namespace ODMR_Lab.基本窗口
{
    /// <summary>
    /// ChartViewerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChartViewerWindow : Window
    {
        public ChartViewerWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
        }

        ChartViewer1D C1D = new ChartViewer1D();

        public void ShowAs1D(List<ChartData1D> data)
        {
            if (ContentPanel.Children.Count == 0)
            {
                ContentPanel.Children.Add(C1D);
            }
            C1D.DataSource.Clear(false);
            C1D.DataSource.AddRange(data);
            C1D.UpdateChartAndDataFlow(true);
            Topmost = true;
            Show();
            Topmost = false;
        }

        ChartViewer2D C2D = new ChartViewer2D();
        public void ShowAs2D(List<ChartData2D> data)
        {
            if (ContentPanel.Children.Count == 0)
            {
                ContentPanel.Children.Add(C2D);
            }
            C2D.DataSource.Clear(false);
            C2D.DataSource.AddRange(data);
            C2D.UpdateChartAndDataFlow();
            Topmost = true;
            Show();
            Topmost = false;
        }

        public void UpdateChartAndDataFlow(bool autoscale)
        {
            if (ContentPanel.Children.Count != 0)
            {
                if (ContentPanel.Children[0] == C1D)
                {
                    C1D.UpdateChartAndDataFlow(true);
                }

                if (ContentPanel.Children[0] == C2D)
                {
                    C2D.UpdateChartAndDataFlow();
                }
            }
        }

        internal void SetTitle(string v)
        {
            Dispatcher.Invoke(() =>
            {
                Title = v;
                title.Content = v;
            });
        }
    }
}
