using Controls.Charts;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// MagnetZWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MagnetZWindow : Window
    {
        private CWPointObject cwPoint1 = null;
        public CWPointObject CWPoint1
        {
            get { return cwPoint1; }
            set
            {
                cwPoint1 = value;
                UpdatePointDisplay();
            }
        }

        private CWPointObject cwPoint2 = null;
        public CWPointObject CWPoint2
        {
            get { return cwPoint2; }
            set
            {
                cwPoint2 = value;
                UpdatePointDisplay();
            }
        }

        private NumricChartData1D LineCW1;
        private NumricChartData1D LineCW2;
        private NumricChartData1D Freq1;
        private NumricChartData1D Freq2;

        private DisplayPage ParentPage { get; set; } = null;

        public MagnetZWindow(string windowtitle, DisplayPage parent)
        {
            InitializeComponent();

            LineCW1 = new NumricChartData1D("CW1", "CW扫描数据", ChartDataType.Y);
            LineCW2 = new NumricChartData1D("CW2", "CW扫描数据", ChartDataType.Y);
            Freq1 = new NumricChartData1D("Freq1", "CW扫描数据", ChartDataType.X);
            Freq2 = new NumricChartData1D("Freq2", "CW扫描数据", ChartDataType.X);

            ParentPage = parent;
            Title = windowtitle;
            WindowTitle.Content = windowtitle;
            CodeHelper.WindowResizeHelper helper = new CodeHelper.WindowResizeHelper();
            helper.RegisterWindow(this, dragHeight: 30);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #region 刷新显示部分
        /// <summary>
        /// 刷新CW点显示
        /// </summary>
        private void UpdatePointDisplay()
        {
            PointList.ClearItems();
            if (CWPoint1 == null)
            {
                PointList.AddItem(null, "", "", "", "", "", "");
            }
            else
            {
                PointList.AddItem(null, Math.Round(CWPoint1.MoverLoc, 4), Math.Round(CWPoint1.CW1, 4).ToString(), Math.Round(CWPoint1.CW2, 4).ToString(), Math.Round(CWPoint1.Bv, 4).ToString(), Math.Round(CWPoint1.Bp, 4).ToString(), Math.Round(CWPoint1.B, 4).ToString());
            }

            if (CWPoint2 == null)
            {
                PointList.AddItem(null, "", "", "", "", "", "");
            }
            else
            {
                PointList.AddItem(null, Math.Round(CWPoint2.MoverLoc, 4), Math.Round(CWPoint2.CW1, 4).ToString(), Math.Round(CWPoint2.CW2, 4).ToString(), Math.Round(CWPoint2.Bv, 4).ToString(), Math.Round(CWPoint2.Bp, 4).ToString(), Math.Round(CWPoint2.B, 4).ToString());
            }
        }

        /// <summary>
        /// 刷新图表显示
        /// </summary>
        public void UpdateChartAndDataFlow(bool IsAutoScale)
        {
            if (PointList.GetSelectedTag() == null) return;
            CWPointObject point = PointList.GetSelectedTag() as CWPointObject;

            LineCW1.Data = point.CW1Values;
            LineCW2.Data = point.CW2Values;
            Freq1.Data = point.CW1Freqs;
            Freq2.Data = point.CW2Freqs;

            CWChart.UpdateChartAndDataFlow(IsAutoScale);
        }


        #endregion

        public void ClearPoints()
        {
            CWPoint1 = null;
            CWPoint2 = null;
            UpdatePointDisplay();

            LineCW1.Data = new List<double>();
            LineCW2.Data = new List<double>();
            Freq1.Data = new List<double>();
            Freq2.Data = new List<double>();
            CWChart.UpdateChartAndDataFlow(true);
        }
    }
}
