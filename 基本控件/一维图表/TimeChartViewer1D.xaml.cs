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
    public partial class TimeChartViewer1D : Grid
    {
        public TimeChartViewer1D()
        {
            InitializeComponent();
            InitChartView();
            InitDataView();
            //数据改变事件
            DataSource.ItemChanged += UpdateDataPanel;
            FitData.ItemChanged += UpdateDataPanel;
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
        private ItemsList<KeyValuePair<TimeChartData1D, NumricChartData1D>> dataSource = new ItemsList<KeyValuePair<TimeChartData1D, NumricChartData1D>>();
        /// <summary>
        /// 数据源，横轴的所有设置无效
        /// </summary>
        public ItemsList<KeyValuePair<TimeChartData1D, NumricChartData1D>> DataSource
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
            foreach (var data in DataSource)
            {
                groups.Add(data.Value.GroupName);
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
                    if (item.Value.GroupName != GroupName)
                    {
                        continue;
                    }
                    DataNames.AddItem(item, item.Value.Name, item.Value.GetCount());
                    if (item.Value.IsInDataDisplay)
                    {
                        DataNames.Select(DataNames.GetRowCount() - 1);
                    }
                }
            }
            if (ChartGroups.SelectedItem != null)
            {
                string GroupName = ChartGroups.SelectedItem.Tag as string;

                TimeDataSet.ClearItems();

                foreach (var item in DataSource)
                {
                    if (item.Value.GroupName != GroupName)
                    {
                        item.Value.IsSelectedAsY = false;
                        continue;
                    }
                    TimeDataSet.AddItem(item, item.Value.Name, item.Value.GetCount().ToString(), item.Value.IsSelectedAsY);
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
            for (int i = 0; i < TimeDataSet.GetRowCount(); i++)
            {
                var item = (KeyValuePair<TimeChartData1D, NumricChartData1D>)TimeDataSet.GetTag(i);
                TimeDataSet.SetCelValue(i, 1, item.Value.GetCount().ToString());
            }

            for (int i = 0; i < DataNames.GetRowCount(); i++)
            {
                var item = (KeyValuePair<TimeChartData1D, NumricChartData1D>)DataNames.GetTag(i);
                DataNames.SetCelValue(i, 1, item.Value.GetCount().ToString());
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
                ///刷新要添加的线
                List<KeyValuePair<TimeChartData1D, NumricChartData1D>> DataToAdd = DataSource.Where(x => x.Value.IsSelectedAsY).ToList();

                List<TimeDataSeries> PlotData = new List<TimeDataSeries>();
                foreach (var item in DataToAdd)
                {
                    //检查是否在原来的线中
                    int ind = chart.DataList.FindIndex(x => item.Value.Name == x.Name);
                    if (ind != -1)
                    {
                        PlotData.Add(chart.DataList[ind] as TimeDataSeries);
                        continue;
                    }
                    else
                    {
                        TimeDataSeries data = new TimeDataSeries(item.Value.Name, item.Key.Data, item.Value.Data);
                        data.LineColor = item.Value.DisplayColor;
                        //不在原来的线中
                        data.Smooth = StyleParam.IsSmooth.Value;
                        data.MarkerSize = StyleParam.PointSize.Value;
                        data.LineThickness = StyleParam.LineWidth.Value;
                        PlotData.Add(data);
                    }
                }

                //刷新拟合线
                foreach (var item in FitData)
                {
                    PlotData.Add(new TimeDataSeries("", item.FittedData.X.Select(x => DateTime.FromOADate(x)).ToList(), item.FittedData.Y));
                }
                chart.DataList = PlotData.Select(x => x as DataSeries).ToList();

                Dispatcher.Invoke(() =>
                {
                    chart.XAxisName = "时间";

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
                (item as DataListViewer).UpdatePointList((item as DataListViewer).CurrentPageIndex);
            }
        }

        #region 图表显示数据选择部分

        private void YDataSelectionChanged(int arg1, int arg2, object arg3)
        {
            var data = (KeyValuePair<TimeChartData1D, NumricChartData1D>)TimeDataSet.GetTag(arg1);
            data.Value.IsSelectedAsY = (bool)arg3;
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
                int count = item.Value.Data.Count;
                if (maxcount < count) maxcount = count;

                namestr += item.Value.Name + " time" + "\t" + item.Value.Name + "\t";
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
                    List<DateTime> data = item.Key.Data;
                    if (i > data.Count - 1)
                    {
                        temp += "" + "\t";
                    }
                    else
                    {
                        temp += data[i].ToString("yyyy/MM/dd HH:mm:ss:fff") + "\t";
                    }
                    List<double> ddata = item.Value.Data;
                    if (i > ddata.Count - 1)
                    {
                        temp += "" + "\t";
                    }
                    else
                    {
                        temp += ddata[i].ToString() + "\t";
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
            var data = (KeyValuePair<TimeChartData1D, NumricChartData1D>)arg2;
            data.Value.IsInDataDisplay = true;
            foreach (var item in DataPanel.Children)
            {
                if ((item as DataListViewer).Tag == data.Value) return;
            }
            DataListViewer v = new DataListViewer()
            {
                HeaderTemplate = new List<ViewerTemplate>()
                {
                    new ViewerTemplate("序号",ListDataTypes.Double,new GridLength(30),false),
                    new ViewerTemplate("值",ListDataTypes.String,new GridLength(1,GridUnitType.Star),false),
                }
            };
            var times = data.Key.GetDataCopyAsString().ToList();
            for (int i = 0; i < times.Count; i++)
            {
                v.AddItem(null, i, times[i]);
            }
            v.Width = 250;
            v.Tag = data.Key;
            DataPanel.Children.Add(v);
            v = new DataListViewer()
            {
                HeaderTemplate = new List<ViewerTemplate>()
                {
                    new ViewerTemplate("序号",ListDataTypes.Double,new GridLength(30),false),
                    new ViewerTemplate("值",ListDataTypes.String,new GridLength(1,GridUnitType.Star),false),
                }
            };
            var vals = data.Value.GetDataCopyAsString().ToList();
            for (int i = 0; i < vals.Count; i++)
            {
                v.AddItem(null, i, vals[i]);
            }
            v.Width = 250;
            v.Tag = data.Value;
            DataPanel.Children.Add(v);
        }
        private void DataNames_MultiItemUnSelected(int arg1, object arg2)
        {
            var data = (KeyValuePair<TimeChartData1D, NumricChartData1D>)arg2;
            data.Value.IsInDataDisplay = false;
            for (int i = 0; i < DataPanel.Children.Count; i++)
            {
                if ((DataPanel.Children[i] as DataListViewer).Tag == data.Key)
                {
                    DataPanel.Children.Remove(DataPanel.Children[i] as DataListViewer);
                    --i;
                    continue;
                };
                if ((DataPanel.Children[i] as DataListViewer).Tag == data.Value)
                {
                    DataPanel.Children.Remove(DataPanel.Children[i] as DataListViewer);
                    --i;
                    continue;
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
    }
}