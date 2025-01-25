using Controls;
using OpenCvSharp.Flann;
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

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// DataListViewer.xaml 的交互逻辑
    /// </summary>
    public partial class DataListViewer : Grid
    {
        public ChartData1D Data { get; set; } = new NumricChartData1D("", "");

        public DataListViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示的点数量
        /// </summary>
        int DisplayCount = 10;

        public int CurrentDisplayIndex { private set; get; } = 0;

        public void UpdatePointList(int startindex, string name = "")
        {
            datacontentscroll.DataTemplate = new List<Controls.ViewerTemplate>() {
                new Controls.ViewerTemplate("序号",ListDataTypes.String,new GridLength(100),false),
                new Controls.ViewerTemplate("值",ListDataTypes.String,new GridLength(1,GridUnitType.Star),false) };
            if (name == "")
            {
                Title.Content = Data.Name;
            }
            else
            {
                Title.Content = Data.GroupName + ":" + Data.Name;
            }
            datacontentscroll.UpdateHeader();
            if (startindex > Data.GetCount() - DisplayCount)
            {
                startindex = Data.GetCount() - DisplayCount;
            }
            if (startindex < 0)
            {
                startindex = 0;
            }

            CurrentDisplayIndex = startindex;

            datacontentscroll.ClearItems();
            for (int i = startindex; i < startindex + DisplayCount; i++)
            {
                if (i > Data.GetCount() - 1) continue;
                string value = "";
                if (Data is TimeChartData1D)
                {
                    value = (Data as TimeChartData1D).Data[i].ToString("yyyy/MM/dd HH:mm:ss:FFF");
                }
                if (Data is NumricChartData1D)
                {
                    value = (Data as NumricChartData1D).Data[i].ToString();
                }
                datacontentscroll.AddItem(null, i, value);
            }
        }

        private void FormerDataList(object sender, RoutedEventArgs e)
        {
            int ind = CurrentDisplayIndex - DisplayCount;
            UpdatePointList(ind);
            DisplayIndex.Text = CurrentDisplayIndex.ToString();
        }

        private void LaterDataList(object sender, RoutedEventArgs e)
        {
            int ind = CurrentDisplayIndex + DisplayCount;
            UpdatePointList(ind);
            DisplayIndex.Text = CurrentDisplayIndex.ToString();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    TextBox b = sender as TextBox;
                    uint ind = uint.Parse(b.Text);
                    UpdatePointList((int)ind);
                    DisplayIndex.Text = CurrentDisplayIndex.ToString();
                }
                catch (Exception ex) { }
            }
        }
    }
}
