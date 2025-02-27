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
            hel.RegisterWindow(this, MinBtn, MaxBtn, null, 5, 40);

            ParentPage = parentPage;
        }

        public new void ShowDialog()
        {
            UpdateDataDisplay();
            base.ShowDialog();
        }

        public DataVisualSource ParentDataSource { get; set; } = null;

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

        #region 映射计算部分

        private void ChangePanel(object sender, RoutedEventArgs e)
        {
            PythonPanel.Visibility = Visibility.Collapsed;
            SimplePanel.Visibility = Visibility.Collapsed;
            if (sender == SimpleBtn)
            {
                SimpleBtn.KeepPressed = true;
                PythonBtn.KeepPressed = false;
                SimplePanel.Visibility = Visibility.Visible;
            }
            if (sender == PythonBtn)
            {
                SimpleBtn.KeepPressed = false;
                PythonBtn.KeepPressed = true;
                PythonPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 计算映射
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calculate1DMapping(object sender, RoutedEventArgs e)
        {

            #region 数据保存格式验证
            foreach (var item in ParentDataSource.ChartDataSource1D)
            {
                if (D1DataName.Text == item.Name)
                {
                    MessageWindow.ShowTipWindow("已存在同名数据，请修改名称", this);
                    return;
                }
            };

            string dataname = D1DataName.Text;
            if (GroupName.SelectedItem == null || DataType.SelectedItem == null) return;
            ChartDataType type = (ChartDataType)Enum.Parse(typeof(ChartDataType), DataType.SelectedItem.Text);
            string groupname = GroupName.SelectedItem.Text;
            #endregion

            #region 计算参数格式验证
            bool CalculateSimple = false;
            if (SimplePanel.Visibility == Visibility.Visible) CalculateSimple = true;


            FuncInstance Func = null;

            int timeout = 10000;

            string expression = "";

            if (CalculateSimple)
            {
                if (SimpleMappingBox.Text == "")
                {
                    MessageWindow.ShowTipWindow("计算表达式不能为空:", this);
                    return;
                }
                expression = SimpleMappingBox.Text;
            }
            else
            {
                if (PyhtonFunc.SelectedItem == null)
                {
                    MessageWindow.ShowTipWindow("未选择参与计算的函数", this);
                    return;
                }
                try
                {
                    timeout = int.Parse(PythonTimeout.Text);
                }
                catch (Exception) { }
                Func = PyhtonFunc.SelectedItem.Tag as FuncInstance;
            }
            #endregion

            NumricChartData1D numricChartData1D = new NumricChartData1D(dataname, groupname, type);

            Thread t = new Thread(() =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        D1CalcBtn.IsEnabled = false;
                    });

                    if (CalculateSimple)
                        CalculateSimple1DMapping(expression, numricChartData1D);
                    else
                        CalculatePython1DMapping(timeout, Func, numricChartData1D);

                    ParentDataSource.ChartDataSource1D.Add(numricChartData1D);
                    Dispatcher.Invoke(() =>
                    {
                        UpdateDataDisplay();
                        D1CalcBtn.IsEnabled = true;
                    });
                    MessageWindow.ShowTipWindow("数据" + dataname + "已添加", this);
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

        #region 简单映射部分
        private void ShowSimpleMappingInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("1.映射计算的数组必须保证元素个数相同。\n2.映射计算的具体操作为对给定数组中的每个元素进行相同计算，得到的结果构成新的数组\n3.此功能仅支持简单函数混合映射运算，复杂函数的映射可能会导致计算速度变慢", this);
        }

        /// <summary>
        /// 计算简单映射
        /// </summary>
        /// <param name="targetData"></param>
        /// <exception cref="Exception"></exception>
        private void CalculateSimple1DMapping(string expression, NumricChartData1D targetData)
        {
            List<ChartData1D> DataToCalc = new List<ChartData1D>();
            int count = -1;
            NormalRealFunction func = null;

            Dispatcher.Invoke(() =>
            {
                func = new NormalRealFunction(expression) { };

                //添加变量
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

            });

            //函数预处理
            func.Process();

            if (DataToCalc.Count == 0) throw new Exception("待计算数据为空");

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
                targetData.Data.Add(func.GetValueAt(datatocal).Content.Last());
            }

        }

        #endregion

        #region Python脚本计算部分
        private void ShowPythonInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("此功能用于使用Python脚本计算映射值，Python脚本需要包含用于计算的函数，函数格式如下：\n1.函数输出必须为单值。\n2.输入参数必须和参与映射的数据集数量相同。\n3.传入脚本的参数为列表类型。", this);
        }

        /// <summary>
        /// 计算Python脚本
        /// </summary>
        private void CalculatePython1DMapping(int timeout, FuncInstance Func, NumricChartData1D targetData)
        {

            //获取数据和变量名
            List<List<double>> Inpputvalues = new List<List<double>>();
            List<string> VarNames = new List<string>();

            int rows = -1;
            Dispatcher.Invoke(() =>
            {
                rows = DataList.GetRowCount();
            });

            for (int i = 0; i < rows; i++)
            {
                ChartData1D data = (DataList.GetTag(i) as List<object>)[0] as ChartData1D;
                List<double> temp = new List<double>();
                if (data is TimeChartData1D)
                {
                    temp.AddRange((data as TimeChartData1D).Data.Select(x => x.ToOADate()));
                }
                else
                {
                    temp.AddRange((data as NumricChartData1D).Data.Select(x => x));
                }
                Inpputvalues.Add(temp);
                VarNames.Add((DataList.GetTag(i) as List<object>)[1] as string);
            }


            targetData.Data.AddRange((Func.Excute(timeout, Inpputvalues, VarNames) as List<object>).Select(x => (double)x).ToList());
        }

        /// <summary>
        /// 选择Python脚本路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectPythonFilePath(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Python脚本文件 (*.py)|*.py";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PythonFileDir.Content = dlg.FileName;
                PythonFileDir.ToolTip = dlg.FileName;
            }
            //刷新函数列表
            UpdateFuncPanel();
        }

        /// <summary>
        /// 刷新函数列表
        /// </summary>
        public void UpdateFuncPanel()
        {
            try
            {
                List<FuncInstance> funcs = Python_NetInterpretor.GetFuncs(PythonFileDir.Content.ToString());
                PyhtonFunc.Items.Clear();
                foreach (var item in funcs)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item.FuncName };
                    SimpleBtn.CloneStyleTo(btn);
                    btn.FontSize = 10;
                    btn.Tag = item;
                    btn.Click += ShowPythonFuncInformation;
                    PyhtonFunc.Items.Add(btn);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 显示函数信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPythonFuncInformation(object sender, RoutedEventArgs e)
        {
            InputParam.ClearItems();
            DecoratedButton btn = sender as DecoratedButton;
            foreach (var item in (btn.Tag as FuncInstance).InputParams)
            {
                InputParam.AddItem(null, item);
            }
        }
        #endregion

        #endregion

        #region 数据保存部分
        /// <summary>
        /// 刷新分组列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateGroup(object sender, RoutedEventArgs e)
        {
            HashSet<string> groups = new HashSet<string>();
            foreach (var item in ParentDataSource.ChartDataSource1D)
            {
                groups.Add(item.GroupName);
            }
            GroupName.Items.Clear();
            foreach (var item in groups)
            {
                DecoratedButton bt = new DecoratedButton() { Text = item };
                D1CalcBtn.CloneStyleTo(bt);
                GroupName.Items.Add(bt);
            }
        }
        #endregion

    }
}
