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

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// DataProcessWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataProcessWindow : Window
    {
        public DataProcessWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, 5, 40);

            DataList.ItemSelected += ShowDataList;
            DataList.ItemContextMenuSelected += DataListMenuEvents;
        }

        public new void ShowDialog()
        {
            UpdateDataDisplay();
            base.ShowDialog();
        }

        public DataVisualSource ParentDataSource { get; set; } = null;

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                return;
            }
            if (WindowState == WindowState.Normal)
            {
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                WindowState = WindowState.Maximized;
                return;
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        #region 数据显示区域
        /// <summary>
        /// 刷新数据条目栏
        /// </summary>
        public void UpdateDataDisplay()
        {
            DataList.ClearItems();
            int count = 0;
            //设置计算面板变量
            SimpleMappingBox.Variables.Clear();
            foreach (var item in ParentDataSource.ChartDataSource1D)
            {
                DataList.AddItem(new List<object>() { item, "V" + count.ToString() }, item.Name, "V" + count.ToString(), item.GetCount().ToString());
                SimpleMappingBox.Variables.Add("V" + count.ToString());
                ++count;
            }
        }

        /// <summary>
        /// 显示数据内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ShowDataList(int index, object obj)
        {
            ChartData1D data = (obj as List<object>)[0] as ChartData1D;
            DataListView.Data = data;
            if (int.TryParse(DataListView.DisplayIndex.Text, out int value) == false)
            {
                DataListView.UpdatePointList(0);
                DataListView.DisplayIndex.Text = "0";
                return;
            }
            DataListView.UpdatePointList(value);
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
                ChartData1D data = (tag as List<object>)[0] as ChartData1D;
                if (MessageWindow.ShowMessageBox("提示", "确定要删除数据" + data.Name + "吗？", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
                {
                    ParentDataSource.ChartDataSource1D.Remove(data);
                    UpdateDataDisplay();
                }
            }
        }
        #endregion

        #region 简单映射部分
        private void ShowSimpleMappingInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("1.映射计算的数组必须保证元素个数相同。\n2.映射计算的具体操作为对给定数组中的每个元素进行相同计算，得到的结果构成新的数组\n3.此功能仅支持简单函数混合映射运算，复杂函数的映射可能会导致计算速度变慢", this);
        }

        private void Calculate1DSimpleMapping(object sender, RoutedEventArgs e)
        {

            foreach (var item in ParentDataSource.ChartDataSource1D)
            {
                if (D1DataName.Text == item.Name)
                {
                    MessageWindow.ShowTipWindow("已存在同名数据，请修改名称", this);
                    return;
                }
            };
            List<ChartData1D> DataToCalc = new List<ChartData1D>();
            int count = -1;
            string expression = SimpleMappingBox.Text;
            NormalRealFunction func = new NormalRealFunction(expression) { };
            foreach (var item in SimpleMappingBox.Variables)
            {
                if (expression.Contains(item))
                {
                    ChartData1D d = ParentDataSource.ChartDataSource1D[int.Parse(item.Replace("V", ""))];
                    func.AddVariable(item, new RealDomain(0, 1, 1));
                    DataToCalc.Add(d);
                    if (count < d.GetCount())
                    {
                        count = d.GetCount();
                    }
                }
            }
            if (DataToCalc.Count == 0) return;

            func.Process();

            Thread t = new Thread(() =>
            {
                NumricChartData1D numricChartData1D = new NumricChartData1D();
                Dispatcher.Invoke(() =>
                {
                    D1CalcBtn.IsEnabled = false;
                    numricChartData1D.Name = D1DataName.Text;
                });
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        List<double> datatocal = new List<double>();
                        foreach (var item in DataToCalc)
                        {
                            if (i > item.GetCount() - 1)
                            {
                                datatocal.Add(double.NaN);
                                continue;
                            }
                            if (item is TimeChartData1D)
                            {
                                datatocal.Add((item as TimeChartData1D).Data[i].ToOADate());
                            }
                            else
                            {
                                datatocal.Add((item as NumricChartData1D).Data[i]);
                            }
                        }
                        numricChartData1D.Data.Add(func.GetValueAt(datatocal).Content.Last());
                    }
                    ParentDataSource.ChartDataSource1D.Add(numricChartData1D);
                    Dispatcher.Invoke(() =>
                    {
                        UpdateDataDisplay();
                        D1CalcBtn.IsEnabled = true;
                    });

                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("计算遇到问题:" + ex.Message, this);
                    Dispatcher.Invoke(() =>
                    {
                        D1CalcBtn.IsEnabled = true;
                    });
                }
            });
            t.Start();

        }
        #endregion
    }
}
