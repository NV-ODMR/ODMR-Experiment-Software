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
            InitChartView();
            InitDataView();
        }

        public void UpdateDataPanel()
        {
            UpdateChartDataPanel();
            UpdateDataDataPanel();
        }

        #region 一维图表数据
        public List<ChartData1D> DataSource { get; set; } = new List<ChartData1D>();

        public ChartData1D SelectedXdata { get; set; } = null;
        #endregion

        #region 视图切换

        /// <summary>
        /// 切换视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeView(object sender, RoutedEventArgs e)
        {
            if (sender == GraphBtn)
            {
                ChartView.Visibility = Visibility.Visible;
                DataView.Visibility = Visibility.Collapsed;
                GraphBtn.KeepPressed = true;
                DataBtn.KeepPressed = false;
            }
            else
            {
                ChartView.Visibility = Visibility.Collapsed;
                DataView.Visibility = Visibility.Visible;
                GraphBtn.KeepPressed = false;
                DataBtn.KeepPressed = true;
            }
        }

        #endregion

        #region 图表视图

        /// <summary>
        /// 初始化图表区域
        /// </summary>
        private void InitChartView()
        {
            //复制图表样式到此图表
            StyleParam.LoadToPage(new FrameworkElement[] { this });
        }

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
                        chart.DataList[i].LineThickness = StyleParam.LineWidth.Value;
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
                    if (SelectedXdata == null) return;
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

        #region 图表显示数据选择部分
        /// <summary>
        /// 刷新数据显示区
        /// </summary>
        public void UpdateChartDataPanel()
        {
            XDataSet.ClearItems();
            YDataSet.ClearItems();
            foreach (var item in DataSource)
            {
                XDataSet.AddItem(item, item.Name, item.GetCount().ToString(), item.IsSelectedAsX);
            }

            foreach (var item in DataSource)
            {
                if (item is NumricChartData1D)
                {
                    YDataSet.AddItem(item, item.Name, item.GetCount().ToString(), item.IsSelectedAsY);
                }
            }
        }

        public void UpdateDataPoint()
        {
            for (int i = 0; i < DataSource.Count; i++)
            {
                XDataSet.SetCelValue(i, 1, DataSource[i].GetCount());
            }

            int ind = 0;
            for (int i = 0; i < DataSource.Count; i++)
            {
                if (DataSource[i] is NumricChartData1D)
                {
                    YDataSet.SetCelValue(ind, 1, DataSource[i].GetCount());
                    ++ind;
                }
            }
        }

        private void XDataSelectionChanged(int arg1, int arg2, object arg3)
        {
            int count = XDataSet.GetRowCount();
            for (int i = 0; i < count - 1; i++)
            {
                if (arg1 != i)
                {
                    XDataSet.SetCelValue(i, 2, false);
                }
            }

            ChartData1D data = XDataSet.GetTag(arg1) as ChartData1D;
            data.IsSelectedAsX = (bool)arg3;

            UpdateChart(true);
        }

        private void YDataSelectionChanged(int arg1, int arg2, object arg3)
        {
            ChartData1D data = YDataSet.GetTag(arg1) as ChartData1D;
            data.IsSelectedAsY = (bool)arg3;
            UpdateChart(true);
        }
        #endregion

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

        #region 文件保存
        /// <summary>
        /// 保存为userdat文件
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
        #endregion

        #endregion

        #region 数据视图

        /// <summary>
        /// 初始化数据区域
        /// </summary>
        private void InitDataView()
        {
        }

        /// <summary>
        /// 刷新数据显示区
        /// </summary>
        public void UpdateDataDataPanel()
        {
            DataNames.ClearItems();
            DataPanel.Children.Clear();
            foreach (var item in DataSource)
            {
                DataNames.AddItem(item, item.Name, item.GetCount().ToString());
                if (item.IsInDataDisplay)
                {
                    DataListViewer v = new DataListViewer();
                    v.Data = item;
                    v.UpdatePointList(0);
                    v.Width = 150;
                    DataPanel.Children.Add(v);
                }
            }
        }

        /// <summary>
        /// 刷新当前数据显示
        /// </summary>
        public void UpdateDataDisplay()
        {
            foreach (var item in DataPanel.Children)
            {
                (item as DataListViewer).UpdatePointList((item as DataListViewer).CurrentDisplayIndex);
            }
        }

        private void DataNames_MultiItemSelected(int arg1, object arg2)
        {
            ChartData1D data = (arg2 as ChartData1D);
            data.IsInDataDisplay = true;
            foreach (var item in DataPanel.Children)
            {
                if ((item as DataListViewer).Data == data) return;
            }
            DataListViewer v = new DataListViewer();
            v.Data = data;
            v.UpdatePointList(0);
            v.Width = 250;
            DataPanel.Children.Add(v);
        }
        private void DataNames_MultiItemUnSelected(int arg1, object arg2)
        {
            ChartData1D data = (arg2 as ChartData1D);
            data.IsInDataDisplay = false;
            foreach (var item in DataPanel.Children)
            {
                if ((item as DataListViewer).Data == data)
                {
                    DataPanel.Children.Remove(item as DataListViewer);
                    return;
                };
            }
        }
        #endregion

    }
}
