using CodeHelper;
using ODMR_Lab.Windows;
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
using MathLib.NormalMath.Decimal.Function;
using Controls;
using System.Threading;
using System.Windows.Markup;
using System.Windows.Forms;
using PythonHandler;
using System.IO;
using Controls.Windows;
using System.Text.RegularExpressions;
using System.Data;
using Controls.Charts;
using ODMR_Lab.数据处理.数据处理窗口.数据处理方法界面;
using System.Windows.Controls.Primitives;

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// DataProcessWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataProcessWindow : Window
    {

        DataVisualPage ParentPage = null;

        public DataProcessWindow(DataVisualPage parentPage)
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, PinBtn, 5, 40);

            ParentPage = parentPage;

            //加载处理方法
            var types = ClassHelper.GetSubClassTypes(typeof(ProcesspageBase));
            foreach (var item in types)
            {
                var it = (ProcesspageBase)Activator.CreateInstance(item);
                ProcessMethods.Add(it);
            }

            UpdateProdessMethodList();
        }

        public void UpdateParentSource(DataVisualSource source)
        {
            ParentDataSource = source;
            foreach (var item in ProcessMethods)
            {
                item.ParentDataSource = source;
                item.UpdateMethod();
            }
        }

        public DataVisualSource ParentDataSource { get; set; } = null;

        public List<ProcesspageBase> ProcessMethods { get; set; } = new List<ProcesspageBase>();
        private ProcesspageBase CurrentMethod = null;

        #region 数据显示区域

        /// <summary>
        /// 刷新数据分组栏
        /// </summary>
        private void UpdateDataGroup(object sender, RoutedEventArgs e)
        {
            DataGroupName.TemplateButton = UIUpdater.ButtonTemplate;
            HashSet<string> d1names = new HashSet<string>(ParentDataSource.ChartDataSource1D.Select(x => x.GroupName));
            HashSet<string> d2names = new HashSet<string>(ParentDataSource.ChartDataSource2D.Select(x => x.GroupName));
            var btns = d1names.Select(x => new DecoratedButton() { Text = x + "(一维)", Tag = new List<string> { x, "一维" } }).ToList();
            btns.AddRange(d2names.Select(x => new DecoratedButton() { Text = x + "(二维)", Tag = new List<string> { x, "二维" } }));
            DataGroupName.UpdateItems(btns);
        }

        private void ShowDatas(object sender, RoutedEventArgs e)
        {
            if (DataGroupName.SelectedItem == null)
                UpdateDataDisplay("");
            else
                UpdateDataDisplay((DataGroupName.SelectedItem.Tag as List<string>)[0]);
            foreach (var item in ProcessMethods)
            {
                item.UpdateMethod();
            }
        }

        /// <summary>
        /// 刷新数据条目栏
        /// </summary>
        public void UpdateDataDisplay(string groupname)
        {
            DataList.ClearItems();
            int count = 0;
            //设置计算面板变量
            DataList.ClearItems();
            var data = ParentDataSource.ChartDataSource1D.Where(x => x.GroupName == groupname);
            foreach (var item in data)
            {
                string varname = "O" + ParentDataSource.ChartDataSource1D.IndexOf(item).ToString();
                DataList.AddItem(new List<object>() { item, varname }, item.Name, varname, "一维", item.GetCount().ToString());
            }
            var data2 = ParentDataSource.ChartDataSource2D.Where(x => x.GroupName == groupname);
            foreach (var item in data2)
            {
                string varname = "T" + ParentDataSource.ChartDataSource2D.IndexOf(item).ToString();
                DataList.AddItem(new List<object>() { item, varname }, item.Data.XName + " " + item.Data.YName + " " + item.Data.ZName, varname, "二维", item.GetXCount().ToString() + "×" + item.GetYCount().ToString());
            }
        }

        public void UpdateProdessMethodList()
        {
            ProcessNames.ClearItems();
            foreach (var item in ProcessMethods)
            {
                ProcessNames.AddItem(item, item.ProcessName);
            }
        }

        /// <summary>
        /// 数据列表右键菜单事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void DataListMenuEvents(int menuind, int itemind, object tag)
        {
            if (menuind == 0)
            {
                if ((tag as List<object>)[0] is ChartData1D)
                {
                    ChartData1D data = (tag as List<object>)[0] as ChartData1D;
                    if (MessageWindow.ShowMessageBox("提示", "确定要删除数据" + data.Name + "吗？", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
                    {
                        ParentDataSource.ChartDataSource1D.Remove(data);
                        UpdateDataDisplay(data.GroupName);
                    }
                }

                if ((tag as List<object>)[0] is ChartData2D)
                {
                    ChartData2D data = (tag as List<object>)[0] as ChartData2D;
                    if (MessageWindow.ShowMessageBox("提示", "确定要删除数据" + data.Data.XName + " " + data.Data.YName + " " + data.Data.ZName + "吗？", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
                    {
                        ParentDataSource.ChartDataSource2D.Remove(data);
                        UpdateDataDisplay(data.GroupName);
                    }
                }

                ParentPage.UpdateFromSource(ParentDataSource);
                foreach (var item in ProcessMethods)
                {
                    item.UpdateMethod();
                }
            }
        }
        #endregion

        //计算得到的数据
        ChartDataBase CalculatedData = null;

        private void Calculate(object sender, RoutedEventArgs e)
        {
            if (CurrentMethod == null) return;
            Thread t = new Thread(() =>
            {
                try
                {
                    CalculatedData = CurrentMethod.CalculateMethod();
                    if (CalculatedData == null) throw new Exception("计算结果为空");
                    //设置显示状态
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        string datatype = (CalculatedData is ChartData1D) ? "一维线数据" : "二维面数据";
                        CalculateState.Text = "计算完成, 数据类型：" + datatype + "  计算时间:" + DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
                        TimeWindow tw = new TimeWindow();
                        tw.Owner = Window.GetWindow(this);
                        tw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        tw.ShowWindow("计算完成");
                    });
                    return;
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        MessageWindow.ShowTipWindow("计算未完成,原因:" + ex.Message, Window.GetWindow(this));
                    });
                }
            });
            t.Start();
        }

        ProcessedDataSaveWindow savewin = new ProcessedDataSaveWindow();
        private void SaveData(object sender, RoutedEventArgs e)
        {
            if (CalculatedData == null)
            {
                MessageWindow.ShowTipWindow("没有可以保存的数据", this);
                return;
            }
            ParentPage.IsEnabled = false;
            savewin.Owner = this;
            savewin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            savewin.AfterHided = new Action<ProcessedDataSaveWindow>((win) =>
            {
                ParentPage.IsEnabled = true;
                if (ParentDataSource == null) return;
                //保存
                if (win.IsChanged)
                {
                    //查重
                    if (win.Data is ChartData1D)
                    {
                        if (ParentDataSource.ChartDataSource1D.Where((x) => x.GroupName == (win.Data as ChartData1D).GroupName && x.Name == (win.Data as ChartData1D).Name).Count() != 0)
                        {
                            MessageWindow.ShowTipWindow("已存在相同数据，无法保存", this);
                            return;
                        }
                        ParentDataSource.ChartDataSource1D.Add(win.Data as ChartData1D);
                        //清空计算数据
                        CalculatedData = null;
                        CalculateState.Text = "";
                    }
                    if (win.Data is ChartData2D)
                    {
                        if (ParentDataSource.ChartDataSource2D.Where((x) => x.GroupName == (win.Data as ChartData2D).GroupName &&
                        x.Data.XName == (win.Data as ChartData2D).Data.XName &&
                         x.Data.YName == (win.Data as ChartData2D).Data.YName &&
                          x.Data.ZName == (win.Data as ChartData2D).Data.ZName
                        ).Count() != 0)
                        {
                            MessageWindow.ShowTipWindow("已存在相同数据，无法保存", this);
                            return;
                        }
                        ParentDataSource.ChartDataSource2D.Add(win.Data as ChartData2D);
                        //清空计算数据
                        CalculatedData = null;
                        CalculateState.Text = "";
                    }
                    if (DataGroupName.SelectedItem != null)
                        UpdateDataDisplay(DataGroupName.SelectedItem.Text);
                    ParentPage.UpdateFromSource(ParentDataSource);
                    foreach (var item in ProcessMethods)
                    {
                        item.UpdateMethod();
                    }
                    TimeWindow twin = new TimeWindow();
                    win.Owner = this;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    twin.ShowWindow("保存成功");
                }
                else
                {
                    return;
                }
            });
            ParentPage.IsEnabled = false;
            savewin.ShowDialog(CalculatedData);
        }

        private void DataViewing(object sender, RoutedEventArgs e)
        {

        }

        private void ProcessNames_ItemSelected(int arg1, object arg2)
        {
            ProcessPanel.Children.Clear();
            ProcessPanel.Children.Add(arg2 as ProcesspageBase);
            CurrentMethod = arg2 as ProcesspageBase;
        }
    }
}
