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
        public ChartData1D Data { get; set; } = new NumricChartData1D();

        public DataListViewer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示的点数量
        /// </summary>
        int DisplayCount = 10;

        int currentDisplayCount = 0;

        public void UpdatePointList(int startindex)
        {

            DataContent.Children.Clear();
            DataName.Content = Data.Name;
            if (startindex > Data.GetCount() - DisplayCount)
            {
                startindex = Data.GetCount() - DisplayCount;
            }
            if (startindex < 0)
            {
                startindex = 0;
            }

            currentDisplayCount = startindex;

            for (int i = startindex; i < startindex + DisplayCount; i++)
            {
                if (i > Data.GetCount() - 1) continue;
                DataContent.Children.Add(CreateDataListBar(i));
            }
        }

        public Grid CreateDataListBar(int index)
        {
            string value = "";
            if (Data is TimeChartData1D)
            {
                value = (Data as TimeChartData1D).Data[index].ToString("yyyy/MM/dd HH:mm:ss:FFF");
            }
            if (Data is NumricChartData1D)
            {
                value = (Data as NumricChartData1D).Data[index].ToString();
            }

            Grid g = new Grid();
            g.Background = Brushes.Transparent;
            g.Height = 40;
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { });
            TextBox l = new TextBox()
            {
                Background = Brushes.Transparent,
                Text = index.ToString(),
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                ToolTip = index.ToString(),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CaretBrush = Brushes.White,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            g.Children.Add(l);
            SetColumn(l, 0);

            l = new TextBox()
            {
                Background = Brushes.Transparent,
                Text = value,
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                ToolTip = value,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CaretBrush = Brushes.White,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            g.Children.Add(l);
            Grid.SetColumn(l, 1);

            return g;
        }

        private void FormerDataList(object sender, RoutedEventArgs e)
        {
            int ind = currentDisplayCount - DisplayCount;
            UpdatePointList(ind);
            DisplayIndex.Text = currentDisplayCount.ToString();
        }

        private void LaterDataList(object sender, RoutedEventArgs e)
        {
            int ind = currentDisplayCount + DisplayCount;
            UpdatePointList(ind);
            DisplayIndex.Text = currentDisplayCount.ToString();
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
                    DisplayIndex.Text = currentDisplayCount.ToString();
                }
                catch (Exception ex) { }
            }
        }
    }
}
