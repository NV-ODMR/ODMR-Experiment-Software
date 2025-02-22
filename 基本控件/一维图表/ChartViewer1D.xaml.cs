using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.基本窗口.数据拟合;
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
        /// <summary>
        /// 是否仅为时间轴
        /// </summary>
        public bool IsOnlyTime
        {
            get { return (bool)GetValue(IsOnlyTimeProperty); }
            set { SetValue(IsOnlyTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOnlyTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOnlyTimeProperty =
            DependencyProperty.Register("IsOnlyTime", typeof(bool), typeof(ChartViewer1D), new PropertyMetadata(false));



        public ChartViewer1D()
        {
            InitializeComponent();
            InitChartView();
            InitDataView();
            //数据改变事件
            DataSource.ItemChanged += UpdateDataPanel;
            FitData.ItemChanged += UpdateDataPanel;
            OnlyTimeDataSource.ItemChanged += UpdateDataPanel;
        }

        /// <summary>
        /// 修改数据显示条元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDataPanel(object sender, RoutedEventArgs e)
        {
            UpdateGroups();
            UpdateDataPanel();
        }

        #region 一维图表数据
        private ItemsList<ChartData1D> dataSource = new ItemsList<ChartData1D>();
        public ItemsList<ChartData1D> DataSource
        {
            get { return dataSource; }
            set
            {
                dataSource = value;
                UpdateDataPanel(this, new RoutedEventArgs());
            }
        }

        private ItemsList<FittedData1D> fitData = new ItemsList<FittedData1D>();
        public ItemsList<FittedData1D> FitData
        {
            get { return fitData; }
            set
            {
                fitData = value;
                UpdateDataPanel(this, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// 仅时间轴数据
        /// </summary>
        public ItemsList<KeyValuePair<TimeChartData1D, NumricChartData1D>> OnlyTimeDataSource { get; set; } = new ItemsList<KeyValuePair<TimeChartData1D, NumricChartData1D>>();

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

        #region 刷新显示部分
        #endregion

        /// <summary>
        /// 刷新数据显示组
        /// </summary>
        private void UpdateGroups()
        {
            #region 查找所有GroupNames
            HashSet<string> groups = new HashSet<string>();
            foreach (ChartData1D data in DataSource)
            {
                groups.Add(data.GroupName);
            }
            ChartGroups.Items.Clear();
            foreach (var item in groups)
            {
                DecoratedButton btn = new DecoratedButton() { Text = item };
                GraphBtn.CloneStyleTo(btn);
                btn.Tag = item;
                ChartGroups.Items.Add(btn);
            }

            DataGroups.Items.Clear();
            foreach (var item in groups)
            {
                DecoratedButton btn = new DecoratedButton() { Text = item };
                GraphBtn.CloneStyleTo(btn);
                btn.Tag = item;
                DataGroups.Items.Add(btn);
            }
            #endregion
        }

        /// <summary>
        /// 数据显示组改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeGroup(object sender, RoutedEventArgs e)
        {
            UpdateDataPanel();
            UpdateChartAndDataFlow(true);
        }

        /// <summary>
        /// 刷新图表数据显示
        /// </summary>
        public void UpdateDataPanel()
        {
            if (DataGroups.SelectedItem == null && DataGroups.Items.Count != 0)
            {
                DataGroups.Select(0);
                return;
            }
            if (ChartGroups.SelectedItem == null && ChartGroups.Items.Count != 0)
            {
                ChartGroups.Select(0);
                return;
            }
            if (DataGroups.SelectedItem != null)
            {
                string GroupName = DataGroups.SelectedItem.Tag as string;

                DataNames.ClearItems();

                foreach (var item in DataSource)
                {
                    if (item.GroupName != GroupName)
                    {
                        continue;
                    }
                    DataNames.AddItem(item, item.Name, item.GetCount());
                    if (item.IsInDataDisplay)
                    {
                        DataNames.Select(DataNames.GetRowCount() - 1);
                    }
                }
            }
            if (ChartGroups.SelectedItem != null)
            {
                string GroupName = ChartGroups.SelectedItem.Tag as string;

                XDataSet.ClearItems();
                YDataSet.ClearItems();

                foreach (var item in DataSource)
                {
                    if (item.GroupName != GroupName)
                    {
                        item.IsSelectedAsX = false;
                        item.IsSelectedAsY = false;
                        continue;
                    }
                    if (item.DataAxisType != ChartDataType.Y || (item is TimeChartData1D))
                        XDataSet.AddItem(item, item.Name, item.GetCount().ToString(), item.IsSelectedAsX);
                    if (item is NumricChartData1D && item.DataAxisType != ChartDataType.X)
                    {
                        YDataSet.AddItem(item, item.Name, item.GetCount().ToString(), item.IsSelectedAsY);
                    }
                }
            }

            FitDataSet.ClearItems();
            foreach (var item in FitData)
            {
                FitDataSet.AddItem(item, item.ToString(), item.FitFunction.ScanArea[0].LowerLimit, item.FitFunction.ScanArea[0].UpperLimit, item.FitFunction.ScanArea[0].ScanPrecision);
                if (item.IsDisplay) { FitDataSet.MultiSelect(FitDataSet.GetRowCount() - 1, false); }
                else
                {
                    FitDataSet.MultiUnSelect(FitDataSet.GetRowCount() - 1, false);
                }
            }

        }

        private void SelectedFitData(int ind, object obj)
        {
            (obj as FittedData1D).IsDisplay = true;
            UpdateDataPanel();
            UpdateChartAndDataFlow(true);
        }

        private void UnSelectedFitData(int ind, object obj)
        {
            (obj as FittedData1D).IsDisplay = false;
            UpdateDataPanel();
            UpdateChartAndDataFlow(true);
        }

        /// <summary>
        /// 选中组
        /// </summary>
        public void SelectGroup(string name)
        {
            ChartGroups.Select(name);
        }

        /// <summary>
        /// 刷新点数显示
        /// </summary>
        private void UpdateDataPoint()
        {
            for (int i = 0; i < XDataSet.GetRowCount(); i++)
            {
                XDataSet.SetCelValue(i, 1, (XDataSet.GetTag(i) as ChartData1D).GetCount());
            }

            for (int i = 0; i < YDataSet.GetRowCount(); i++)
            {
                YDataSet.SetCelValue(i, 1, (YDataSet.GetTag(i) as ChartData1D).GetCount());
            }
        }

        Thread UpdateThread = null;
        /// <summary>
        /// 从DataSource中同步数据并刷新绘图
        /// </summary>
        public void UpdateChartAndDataFlow(bool IsAutoScale)
        {
            //刷新数据点数
            UpdateDataPoint();
            //刷新数据显示
            UpdateDataDisplay();

            while (UpdateThread != null && UpdateThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(50);
            }
            UpdateThread = new Thread(() =>
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
                    HasName = (chart.XAxisName == SelectedXdata.Name);
                });

                ///刷新要添加的线
                List<ChartData1D> DataToAdd = DataSource.Where(x => x.IsSelectedAsY).ToList();

                List<DataSeries> PlotData = new List<DataSeries>();
                foreach (var item in DataToAdd)
                {
                    DataSeries data = null;
                    if (SelectedXdata is TimeChartData1D) data = new TimeDataSeries(item.Name)
                    {
                        X = (SelectedXdata as TimeChartData1D).Data,
                        Y = item.GetDataCopyAsDouble().ToList()
                    };
                    if (SelectedXdata is NumricChartData1D) data = new NumricDataSeries(item.Name)
                    {
                        X = (SelectedXdata as NumricChartData1D).Data,
                        Y = item.GetDataCopyAsDouble().ToList()
                    };
                    data.LineColor = item.DisplayColor;
                    //检查是否在原来的线中
                    int ind = chart.DataList.FindIndex(x => item.Name == x.Name);
                    if (ind != -1)
                    {
                        data.LineThickness = chart.DataList[ind].LineThickness;
                        data.MarkerSize = chart.DataList[ind].MarkerSize;
                        data.Smooth = chart.DataList[ind].Smooth;
                        data.Name = chart.DataList[ind].Name;
                    }
                    else
                    {
                        //不在原来的线中
                        data.Smooth = StyleParam.IsSmooth.Value;
                        data.MarkerSize = StyleParam.PointSize.Value;
                        data.LineThickness = StyleParam.LineWidth.Value;
                    }
                    PlotData.Add(data);
                }

                //刷新拟合线
                foreach (var item in FitData)
                {
                    if (item.IsDisplay && item.XData == SelectedXdata)
                    {
                        item.FittedData.Name = item.FitName;
                        PlotData.Add(item.FittedData);
                    }
                }
                chart.DataList = PlotData;

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
            UpdateThread.Start();
        }

        /// <summary>
        /// 刷新当前数据显示
        /// </summary>
        private void UpdateDataDisplay()
        {
            foreach (var item in DataPanel.Children)
            {
                (item as DataListViewer).UpdatePointList((item as DataListViewer).CurrentDisplayIndex);
            }
        }

        #region 图表显示数据选择部分

        private void XDataSelectionChanged(int arg1, int arg2, object arg3)
        {
            int count = XDataSet.GetRowCount();
            for (int i = 0; i < count; i++)
            {
                ChartData1D dat = XDataSet.GetTag(i) as ChartData1D;
                dat.IsSelectedAsX = false;
            }

            ChartData1D data = XDataSet.GetTag(arg1) as ChartData1D;
            data.IsSelectedAsX = (bool)arg3;

            UpdateDataPanel();
            UpdateChartAndDataFlow(true);
        }

        private void YDataSelectionChanged(int arg1, int arg2, object arg3)
        {
            ChartData1D data = YDataSet.GetTag(arg1) as ChartData1D;
            data.IsSelectedAsY = (bool)arg3;
            UpdateChartAndDataFlow(true);
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
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        private void FitCurve(object sender, RoutedEventArgs e)
        {
            CurveFitWindow win = new CurveFitWindow();
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FittedData1D data = win.ShowDialog(DataSource);
            if (data == null) return;
            FitData.Add(data);
            data.IsDisplay = true;
            UpdateDataPanel();
            UpdateChartAndDataFlow(true);
        }

        #endregion

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
        /// 保存为文本文件
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
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("文件未成功保存，原因：" + ex.Message, Window.GetWindow(this));
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
            v.UpdatePointList(0, data.GroupName + ":" + data.Name);
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

        /// <summary>
        /// 拟合参数菜单
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void FitDataMenu(int arg1, int arg2, object arg3)
        {
            #region 删除
            if (arg1 == 0)
            {
                FittedData1D d = arg3 as FittedData1D;
                string pcontent = "";
                int pcount = d.FitFunction.GetParamCount();
                for (int i = 0; i < pcount; i++)
                {
                    var p = d.FitFunction.FindParamAt(i);
                    pcontent += p.Key + "\t\t" + p.Value.ToString() + "\n";
                }

                MessageWindow.ShowTipWindow("函数信息：\n表达式:" + d.Expression + "\n\n" + "参数值：\n" + pcontent, Window.GetWindow(this));
            }
            if (arg1 == 1)
            {
                if (MessageWindow.ShowMessageBox("提示", "确定要删除吗？", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    FitData.Remove(arg3 as FittedData1D);
                    UpdateDataPanel();
                    UpdateChartAndDataFlow(true);
                }
            }
            //参数设置
            if (arg1 == 2)
            {
                FitCurveParamSetWindow win = new FitCurveParamSetWindow();
                win.Owner = Window.GetWindow(this);
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                FittedData1D d = arg3 as FittedData1D;
                win.ShowDialog(d.LoRange, d.HiRange, d.SampleCount);
                Thread t = new Thread(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FitDataSet.IsEnabled = false;
                    });
                    d.UpdatePoint(win.Lo, win.Hi, win.Count);
                    Dispatcher.Invoke(() =>
                    {
                        UpdateDataPanel();
                        UpdateChartAndDataFlow(true);
                        FitDataSet.IsEnabled = true;
                    });
                });
                t.Start();
            }
            #endregion
        }

        #region 光标
        private void AddCursor(object sender, RoutedEventArgs e)
        {
            chart.AddCursorCenter();
            CursorCount.Content = chart.GetCursorCount();
        }

        private void BringCursorToCenter(object sender, RoutedEventArgs e)
        {
            chart.BringAllCursorToCenter();
            CursorCount.Content = chart.GetCursorCount();
        }
        #endregion
    }
}