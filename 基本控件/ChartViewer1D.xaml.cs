using CodeHelper;
using Controls;
using Controls.Charts;
using ODMR_Lab.Windows;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using Label = System.Windows.Controls.Label;

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// ChartViewer1D.xaml 的交互逻辑
    /// </summary>
    public partial class ChartViewer1D : Grid
    {
        public Window ParentWindow { get; set; } = null;

        public ChartViewer1D()
        {
            InitializeComponent();
            StyleParam.LoadToPage(new FrameworkElement[] { this });
        }

        #region 一维图表数据
        public List<ChartData1D> DataSource { get; set; } = new List<ChartData1D>();

        public ChartData1D SelectedXdata { get; set; } = null;
        #endregion

        /// <summary>
        /// 刷新图表
        /// </summary>
        public void UpdateChart(bool IsAutoScale)
        {
            Thread t = new Thread(() =>
            {
                SelectedXdata = null;
                foreach (var item in DataSource)
                {
                    if (item.IsSelectedAsX) SelectedXdata = item;
                }
                if (SelectedXdata == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        chart.DataList.Clear();
                        chart.RefreshPlotWithAutoScaleY();
                    });
                    return;
                }
                bool HasName = false;
                Dispatcher.Invoke(() =>
                {
                    HasName = chart.XAxisName == SelectedXdata.Name;
                });
                if (HasName)
                {
                    //不需要改变所有线
                    for (int i = 0; i < chart.DataList.Count; i++)
                    {
                        chart.DataList[i].Smooth = IsSmooth.IsSelected;
                        chart.DataList[i].LineThickness = LineThickness;
                        bool exist = false;
                        foreach (var item in DataSource)
                        {
                            if (item is TimeChartData1D) continue;
                            NumricChartData1D temp = item as NumricChartData1D;
                            if (item.IsSelectedAsY && item.Name == chart.DataList[i].Name)
                            {
                                exist = true;
                                if (SelectedXdata is NumricChartData1D)
                                {
                                    (chart.DataList[i] as NumricDataSeries).ClearData();
                                    (chart.DataList[i] as NumricDataSeries).AddDatas((SelectedXdata as NumricChartData1D).Data, temp.Data);
                                }
                                if (SelectedXdata is TimeChartData1D)
                                {
                                    (chart.DataList[i] as TimeDataSeries).ClearData();
                                    (chart.DataList[i] as TimeDataSeries).AddDatas((SelectedXdata as TimeChartData1D).Data, temp.Data);
                                }
                                break;
                            }
                        }
                        if (exist == false)
                        {
                            //旧线不包括在新线中
                            chart.DataList.RemoveAt(i);
                            --i;
                        }
                    }

                    //添加进之前没有的线
                    foreach (var item in DataSource)
                    {
                        if (item is TimeChartData1D) continue;
                        NumricChartData1D temp = item as NumricChartData1D;
                        bool exist = false;
                        for (int i = 0; i < chart.DataList.Count; i++)
                        {
                            if (item.IsSelectedAsY && item.Name == chart.DataList[i].Name)
                            {
                                exist = true;
                            }
                        }
                        if (exist == false && item.IsSelectedAsY)
                        {
                            //添加新线进入图表
                            if (SelectedXdata is NumricChartData1D)
                            {
                                if ((SelectedXdata as NumricChartData1D).Data.Count != (item as NumricChartData1D).Data.Count) continue;
                                NumricDataSeries ser = new NumricDataSeries(temp.Name);
                                ser.AddDatas((SelectedXdata as NumricChartData1D).Data, temp.Data);
                                chart.DataList.Add(ser);
                            }
                            if (SelectedXdata is TimeChartData1D)
                            {
                                if ((SelectedXdata as TimeChartData1D).Data.Count != (item as NumricChartData1D).Data.Count) continue;
                                TimeDataSeries ser = new TimeDataSeries(temp.Name) { };
                                ser.AddDatas((SelectedXdata as TimeChartData1D).Data, temp.Data);
                                chart.DataList.Add(ser);
                            }
                        }
                    }
                }
                else
                {
                    chart.DataList.Clear();
                    foreach (var item in DataSource)
                    {
                        if (item.IsSelectedAsY)
                        {
                            if (item is TimeChartData1D) continue;
                            NumricChartData1D temp = item as NumricChartData1D;
                            //添加新线进入图表
                            if (SelectedXdata is NumricChartData1D)
                            {
                                NumricDataSeries ser = new NumricDataSeries(temp.Name);
                                ser.AddDatas((SelectedXdata as NumricChartData1D).Data, temp.Data);
                                chart.DataList.Add(ser);
                            }
                            if (SelectedXdata is TimeChartData1D)
                            {
                                TimeDataSeries ser = new TimeDataSeries(temp.Name);
                                ser.AddDatas((SelectedXdata as TimeChartData1D).Data, temp.Data);
                                chart.DataList.Add(ser);
                            }
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    chart.XAxisName = SelectedXdata.Name;

                    ApplyChartStyle(StyleParam);

                    if (IsAutoScale)
                    {
                        chart.RefreshPlotWithAutoScale();
                    }
                    else
                    {
                        chart.RefreshPlotWithAutoScaleY();
                    }
                });

            });
            t.Start();
        }

        /// <summary>
        /// 刷新数据显示区
        /// </summary>
        public void UpdateDataPanel()
        {
            XDataSet.Children.Clear();
            YDataSet.Children.Clear();
            foreach (var item in DataSource)
            {
                Grid g = CreateDataViewBar(item);
                XDataSet.Children.Add(g);
            }

            foreach (var item in DataSource)
            {
                if (item is NumricChartData1D)
                {
                    YDataSet.Children.Add(CreateDataViewBar(item));
                }
            }
        }

        public void UpdateDataPoint()
        {
            for (int i = 0; i < DataSource.Count; i++)
            {
                int datacount = 0;
                if (DataSource[i] is NumricChartData1D) datacount = (DataSource[i] as NumricChartData1D).Data.Count;
                if (DataSource[i] is TimeChartData1D) datacount = (DataSource[i] as TimeChartData1D).Data.Count;
                ((XDataSet.Children[i] as Grid).Children[2] as Label).Content = "点数：" + datacount.ToString();
            }

            for (int i = 0; i < DataSource.Count; i++)
            {
                if (DataSource[i] is NumricChartData1D)
                {
                    int datacount = 0;
                    if (DataSource[i] is NumricChartData1D) datacount = (DataSource[i] as NumricChartData1D).Data.Count;
                    if (DataSource[i] is TimeChartData1D) datacount = (DataSource[i] as TimeChartData1D).Data.Count;
                    ((YDataSet.Children[i] as Grid).Children[2] as Label).Content = "点数：" + datacount.ToString();
                }
            }
        }

        private Grid CreateDataViewBar(ChartData1D data)
        {
            Grid g = new Grid();

            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });

            g.Height = 40;
            g.Margin = new Thickness(5);
            Label l = new Label() { Content = data.Name, Foreground = Brushes.White, HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            l.FontSize = 10;
            l.ToolTip = data.Name;
            Grid.SetColumn(l, 0);
            Grid.SetRow(l, 0);
            g.Children.Add(l);

            Chooser c = new Chooser() { Width = 40, Height = 20 };
            Grid.SetColumn(c, 1);
            c.Selected += DataSelected;
            c.UnSelected += DataSelected;
            Grid.SetRow(c, 0);
            g.Children.Add(c);


            int datacount = 0;
            if (data is NumricChartData1D) datacount = (data as NumricChartData1D).Data.Count;
            if (data is TimeChartData1D) datacount = (data as TimeChartData1D).Data.Count;

            l = new Label() { Content = "点数：" + datacount.ToString(), Foreground = Brushes.White, HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            l.FontSize = 10;
            Grid.SetColumn(l, 2);
            Grid.SetRow(l, 0);
            g.Children.Add(l);

            g.Tag = data;

            return g;
        }

        /// <summary>
        /// 选中数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataSelected(object sender, RoutedEventArgs e)
        {
            if (((sender as Chooser).Parent as Grid).Parent == XDataSet)
            {
                foreach (var item in XDataSet.Children)
                {
                    if ((item as Grid).Children[1] != sender)
                    {
                        ((item as Grid).Children[1] as Chooser).IsSelected = false;
                    }
                }
                foreach (var item in DataSource)
                {
                    item.IsSelectedAsX = false;
                }
               (((sender as Chooser).Parent as Grid).Tag as ChartData1D).IsSelectedAsX = (sender as Chooser).IsSelected;
            }
            else
            {
                (((sender as Chooser).Parent as Grid).Tag as ChartData1D).IsSelectedAsY = (sender as Chooser).IsSelected;
            }
            UpdateChart(true);
        }

        double LineThickness = 1;
        /// <summary>
        /// 设置图表样式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Apply(object sender, RoutedEventArgs e)
        {
            try
            {
                double v = double.Parse(LineWidth.Text);
                LineThickness = v;
                UpdateChart(false);
            }
            catch
            {
                MessageWindow.ShowTipWindow("参数格式错误", MainWindow.Handle);
            }
        }

        #region 图表操作
        private void ResizePlot(object sender, RoutedEventArgs e)
        {
            chart.RefreshPlotWithAutoScale();
        }

        private void Snap(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(chart));
            TimeWindow window = new TimeWindow();
            window.Owner = MainWindow.Handle;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }
        #endregion

        private void chart_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                chart.RefreshPlotWithAutoScaleY();
            }
        }

        #region 图表样式设置
        Chart1DStyleParams StyleParam = (Chart1DStyleParams)Chart1DStyleParams.GlobalChartPlotParam.Copy();

        private void ApplyLineStyle(object sender, RoutedEventArgs e)
        {
            StyleParam.ReadFromPage(new FrameworkElement[] { this });
            Chart1DStyleParams.GlobalChartPlotParam = (Chart1DStyleParams)StyleParam.Copy();
            ApplyChartStyle(StyleParam);
        }


        private void ApplyLineStyleKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StyleParam.ReadFromPage(new FrameworkElement[] { this });
                Chart1DStyleParams.GlobalChartPlotParam = (Chart1DStyleParams)StyleParam.Copy();
                ApplyChartStyle(StyleParam);
            }
        }

        private void ApplyChartStyle(Chart1DStyleParams para)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var item in chart.DataList)
                {
                    item.Smooth = para.IsSmooth.Value;
                    try
                    {
                        item.LineThickness = para.LineWidth.Value;
                    }
                    catch (Exception) { }

                    string style = para.PlotStyle.Value;
                    if (style == "点状图")
                    {
                        item.MarkerSize = para.PointSize.Value;
                        item.LineThickness = 0;
                    }
                    if (style == "线状图")
                    {
                        item.LineThickness = para.LineWidth.Value;
                        item.MarkerSize = 0;
                    }
                    if (style == "点线图")
                    {
                        item.LineThickness = para.LineWidth.Value;
                        item.MarkerSize = para.PointSize.Value;
                    }
                    chart.RefreshPlotWithAutoScaleY();
                }
            });
        }
        #endregion

        /// <summary>
        /// 保存为其他类型文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsExternal(object sender, RoutedEventArgs e)
        {
            string name = (sender as DecoratedButton).Name;
            string save = "";
            int maxcount = -1;

            string namestr = "";
            foreach (var item in DataSource)
            {
                if (item is TimeChartData1D)
                {
                    int count = (item as TimeChartData1D).Data.Count;
                    if (maxcount < count) maxcount = count;
                }
                if (item is NumricChartData1D)
                {
                    int count = (item as NumricChartData1D).Data.Count;
                    if (maxcount < count) maxcount = count;
                }

                namestr += item.Name + "\t";
            }

            if (namestr != "")
            {
                namestr = namestr.Remove(namestr.Length - 1, 1);
            }
            namestr += "\n";
            save += namestr;

            string datastr = "";
            for (int i = 0; i < maxcount; i++)
            {
                string temp = "";
                foreach (var item in DataSource)
                {
                    if (item is TimeChartData1D)
                    {
                        List<DateTime> data = (item as TimeChartData1D).Data;
                        if (i > data.Count - 1)
                        {
                            temp += "" + "\t";
                        }
                        else
                        {
                            temp += data[i].ToString("yyyy/MM/dd HH:mm:ss:fff") + "\t";
                        }
                    }
                    if (item is NumricChartData1D)
                    {
                        List<double> data = (item as NumricChartData1D).Data;
                        if (i > data.Count - 1)
                        {
                            temp += "" + "\t";
                        }
                        else
                        {
                            temp += data[i].ToString() + "\t";
                        }
                    }
                }
                if (temp != "")
                {
                    temp = temp.Remove(temp.Length - 1, 1);
                }
                datastr += temp + "\n";
            }

            if (datastr != null)
            {
                datastr = datastr.Remove(datastr.Length - 1, 1);
            }

            save += datastr;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "文本文件 (*.txt)|*.txt";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter wr = new StreamWriter(new FileStream(dlg.FileName, FileMode.Create)))
                    {
                        wr.Write(save);
                    }
                    TimeWindow win = new TimeWindow();
                    win.Owner = MainWindow.Handle;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("文件未成功保存，原因：" + ex.Message, ParentWindow);
                }
            }

        }
    }
}
