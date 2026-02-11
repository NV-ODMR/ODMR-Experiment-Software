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
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);

            ParentPage = parentPage;
        }

        public DataVisualSource ParentDataSource { get; set; } = null;

        #region 数据显示区域

        /// <summary>
        /// 刷新数据分组栏
        /// </summary>
        private void UpdateDataGroup(object sender, RoutedEventArgs e)
        {
            DataGroupName.TemplateButton = DataGroupName;
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
        }

        public new void ShowDialog()
        {
            UpdateVariables();
            base.ShowDialog();
        }

        private void UpdateVariables()
        {
            //添加变量
            SimpleMappingBox.Variables.Clear();
            SimpleMappingBox.Variables.AddRange(ParentDataSource.ChartDataSource1D.Select(x => "O" + ParentDataSource.ChartDataSource1D.IndexOf(x)));
            SimpleMappingBox.Variables.AddRange(ParentDataSource.ChartDataSource2D.Select(x => "T" + ParentDataSource.ChartDataSource2D.IndexOf(x)));
            SimpleMappingBox.UpdateTextDisplay();
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

        /// <summary>
        /// 显示数据内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ShowDataList(int index, object obj)
        {
            if ((obj as List<object>)[0] is ChartData2D) return;
            ChartData1D data = (obj as List<object>)[0] as ChartData1D;
            var values = data.GetDataCopyAsString().ToList();
            DataListView.ClearItems();
            for (int i = 0; i < values.Count; i++)
            {
                DataListView.AddItem(null, i, values[i]);
            }
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
                if ((tag as List<object>)[0] is ChartData1D)
                {
                    ChartData1D data = (tag as List<object>)[0] as ChartData1D;
                    if (MessageWindow.ShowMessageBox("提示", "确定要删除数据" + data.Name + "吗？", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
                    {
                        ParentDataSource.ChartDataSource1D.Remove(data);
                        UpdateDataDisplay(data.GroupName);
                        UpdateVariables();
                    }
                }

                if ((tag as List<object>)[0] is ChartData2D)
                {
                    ChartData2D data = (tag as List<object>)[0] as ChartData2D;
                    if (MessageWindow.ShowMessageBox("提示", "确定要删除数据" + data.Data.XName + " " + data.Data.YName + " " + data.Data.ZName + "吗？", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
                    {
                        ParentDataSource.ChartDataSource2D.Remove(data);
                        UpdateDataDisplay(data.GroupName);
                        UpdateVariables();
                    }
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

        private void ChangeResultType(object sender, RoutedEventArgs e)
        {
            D1Result.KeepPressed = false;
            D2Result.KeepPressed = false;
            D1ResultPanel.Visibility = Visibility.Hidden;
            D2ResultPanel.Visibility = Visibility.Hidden;

            if (sender == D1Result)
            {
                D1Result.KeepPressed = true;
                D1ResultPanel.Visibility = Visibility.Visible;
            }
            else
            {
                D2Result.KeepPressed = true;
                D2ResultPanel.Visibility = Visibility.Visible;
            }
        }


        private void UpdateD1Group(object sender, RoutedEventArgs e)
        {
            D1GroupName.TemplateButton = DataGroupName;
            HashSet<string> d1names = new HashSet<string>(ParentDataSource.ChartDataSource1D.Select(x => x.GroupName));
            var btns = d1names.Select(x => new DecoratedButton() { Text = x + "(一维)", Tag = new List<string> { x, "一维" } }).ToList();
            D1GroupName.UpdateItems(btns);
        }

        private void UpdateD2Group(object sender, RoutedEventArgs e)
        {
            D2GroupName.TemplateButton = DataGroupName;
            HashSet<string> d2names = new HashSet<string>(ParentDataSource.ChartDataSource2D.Select(x => x.GroupName));
            var btns = d2names.Select(x => new DecoratedButton() { Text = x + "(二维)", Tag = new List<string> { x, "二维" } }).ToList();
            D2GroupName.UpdateItems(btns);
        }

        /// <summary>
        /// 是否是二维计算表达式并且是否合法
        /// </summary>
        /// <returns></returns>
        private bool IsD2CalCulate(string expression, out int xcount, out int ycount, out double xlo, out double xhi, out double ylo, out double yhi, out List<ChartData2D> datas, out List<string> datavs)
        {
            xcount = -1;
            ycount = -1;
            xlo = 0;
            ylo = 0;
            xhi = 0;
            yhi = 0;
            datas = new List<ChartData2D>();
            datavs = new List<string>();
            Regex reg1 = new Regex("[O][0-9]+");
            Regex reg2 = new Regex("[T][0-9]+");
            MatchCollection coll1 = reg1.Matches(expression);
            MatchCollection coll2 = reg2.Matches(expression);
            if (coll1.Count != 0) return false;
            //查找变量
            foreach (Match m in coll2)
            {
                ChartData2D dd = ParentDataSource.ChartDataSource2D[int.Parse(m.Value.Replace("T", ""))];
                if (!datavs.Contains(m.Value))
                {
                    datas.Add(dd);
                    datavs.Add(m.Value);
                }
            }
            HashSet<int> xs = new HashSet<int>(datas.Select(x => x.Data.XCounts));
            HashSet<int> ys = new HashSet<int>(datas.Select(x => x.Data.YCounts));
            HashSet<double> xlos = new HashSet<double>(datas.Select(x => x.Data.XLo));
            HashSet<double> ylos = new HashSet<double>(datas.Select(x => x.Data.YLo));
            HashSet<double> xhis = new HashSet<double>(datas.Select(x => x.Data.XHi));
            HashSet<double> yhis = new HashSet<double>(datas.Select(x => x.Data.YHi));
            if (xs.Count != 1 || ys.Count != 1 || xlos.Count != 1 || ylos.Count != 1 || xhis.Count != 1 || yhis.Count != 1) throw new Exception("数据总点数不同，无法进行映射计算");
            xcount = xs.ElementAt(0);
            ycount = ys.ElementAt(0);
            xlo = xlos.ElementAt(0);
            ylo = ylos.ElementAt(0);
            xhi = xhis.ElementAt(0);
            yhi = yhis.ElementAt(0);
            return true;
        }

        /// <summary>
        /// 是否是一维计算表达式
        /// </summary>
        /// <returns></returns>
        private bool IsD1CalCulate(string expression, out int count, out List<ChartData1D> datas, out List<string> datavs)
        {
            datas = new List<ChartData1D>();
            datavs = new List<string>();
            Regex reg1 = new Regex("[O][0-9]+");
            Regex reg2 = new Regex("[T][0-9]+");
            count = -1;
            MatchCollection coll1 = reg1.Matches(expression);
            MatchCollection coll2 = reg2.Matches(expression);
            if (coll2.Count != 0) return false;
            //查找变量
            foreach (Match m in coll1)
            {
                ChartData1D dd = ParentDataSource.ChartDataSource1D[int.Parse(m.Value.Replace("T", ""))];
                if (!datavs.Contains(m.Value))
                {
                    datas.Add(dd);
                    datavs.Add(m.Value);
                }
            }
            HashSet<int> xs = new HashSet<int>(datas.Select(x => x.GetCount()));
            if (xs.Count != 1) throw new Exception("数据总点数不同，无法进行映射计算");
            count = xs.ElementAt(0);
            return true;
        }



        /// <summary>
        /// 计算映射
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalculateMapping(object sender, RoutedEventArgs e)
        {
            try
            {
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
                        throw new Exception("计算表达式不能为空:");
                    }
                    expression = SimpleMappingBox.Text;
                }
                else
                {
                    if (PyhtonFunc.SelectedItem == null)
                    {
                        throw new Exception("未选择参与计算的函数:");
                    }
                    timeout = int.Parse(PythonTimeout.Text);
                    Func = PyhtonFunc.SelectedItem.Tag as FuncInstance;
                }
                #endregion

                #region 一维映射数据
                if (D1Result.KeepPressed)
                {
                    if (ParentDataSource.ChartDataSource1D.Where(x => x.GroupName == D1DataName.Text).Count() != 0)
                    {
                        throw new Exception("已存在同名数据，请修改名称");
                    }
                    string dataname = D1DataName.Text;
                    if (D1GroupName.SelectedItem == null || D1DataType.SelectedItem == null) throw new Exception("选项不能为空");
                    ChartDataType type = (ChartDataType)Enum.Parse(typeof(ChartDataType), D1DataType.SelectedItem.Text);
                    string groupname = (D1GroupName.SelectedItem.Tag as List<string>)[0];
                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                CalcBtn.IsEnabled = false;
                            });

                            NumricChartData1D data = null;
                            if (CalculateSimple)
                            {
                                if (IsD1CalCulate(expression, out int count, out List<ChartData1D> ds, out List<string> dvs) == false)
                                {
                                    throw new Exception("二维数据映射计算无法保存为一维结果");
                                }
                                data = CalculateSimple1DMapping(expression, groupname, dataname, count, type, ds, dvs);
                            }
                            else
                                data = CalculatePython1DMapping(groupname, dataname, type, timeout, Func);

                            ParentDataSource.ChartDataSource1D.Add(data);
                            Dispatcher.Invoke(() =>
                            {
                                if (DataGroupName.SelectedItem != null)
                                    UpdateDataDisplay(DataGroupName.SelectedItem.Text);
                                CalcBtn.IsEnabled = true;
                                UpdateVariables();
                            });
                            MessageWindow.ShowTipWindow("数据" + dataname + "已添加", this);
                        }
                        catch (Exception ex)
                        {
                            MessageWindow.ShowTipWindow("计算遇到问题:" + ex.Message, this);
                            Dispatcher.Invoke(() =>
                            {
                                CalcBtn.IsEnabled = true;
                                UpdateVariables();
                            });
                        }
                    });
                    t.Start();
                }
                #endregion

                #region 二维映射数据
                if (D2Result.KeepPressed)
                {
                    if (ParentDataSource.ChartDataSource1D.Where(x => x.GroupName == D1DataName.Text).Count() != 0)
                    {
                        throw new Exception("已存在同名数据，请修改名称");
                    }
                    string xname = D2XName.Text;
                    string yname = D2YName.Text;
                    string zname = D2ZName.Text;
                    if (xname == "" || yname == "" || zname == "") throw new Exception("轴名称不能为空");
                    if (D2GroupName.SelectedItem == null) throw new Exception("选项不能为空");
                    string groupname = (D2GroupName.SelectedItem.Tag as List<string>)[0];
                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                CalcBtn.IsEnabled = false;
                            });

                            ChartData2D data = null;

                            if (CalculateSimple)
                            {
                                if (IsD2CalCulate(expression, out int xcount, out int ycount, out double xlo, out double xhi, out double ylo, out double yhi, out List<ChartData2D> ds, out List<string> dvs) == false)
                                {
                                    throw new Exception("一维数据映射计算无法保存为二维结果");
                                }
                                data = CalculateSimple2DMapping(expression, groupname, xname, yname, zname, xcount, ycount, xlo, xhi, ylo, yhi, ds, dvs);
                            }

                            ParentDataSource.ChartDataSource2D.Add(data);
                            Dispatcher.Invoke(() =>
                            {
                                if (DataGroupName.SelectedItem != null)
                                    UpdateDataDisplay(DataGroupName.SelectedItem.Text);
                                CalcBtn.IsEnabled = true;
                                UpdateVariables();
                            });
                            MessageWindow.ShowTipWindow("数据" + xname + " " + yname + " " + zname + "已添加", this);
                        }
                        catch (Exception ex)
                        {
                            MessageWindow.ShowTipWindow("计算遇到问题:" + ex.Message, this);
                            Dispatcher.Invoke(() =>
                            {
                                CalcBtn.IsEnabled = true;
                                UpdateVariables();
                            });
                        }
                    });
                    t.Start();
                }
                #endregion
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("计算过程出现异常:\n" + ex.Message, this);
            }
        }

        #region 简单映射部分
        private void ShowSimpleMappingInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("1.映射计算的数组必须保证元素个数相同。\n2.映射计算的具体操作为对给定数组中的每个元素进行相同计算，得到的结果构成新的数组\n3.此功能仅支持简单函数混合映射运算，复杂函数的映射可能会导致计算速度变慢", this);
        }

        /// <summary>
        /// 计算一维简单映射
        /// </summary>
        /// <param name="targetData"></param>
        /// <exception cref="Exception"></exception>
        private NumricChartData1D CalculateSimple1DMapping(string expression, string resultGroupname, string resultName, int count, ChartDataType type, List<ChartData1D> sourceData, List<string> sourceDataVs)
        {
            NumricChartData1D targetData = new NumricChartData1D(resultName, resultGroupname, type);
            NormalRealFunction func = null;

            Dispatcher.Invoke(() =>
            {
                func = new NormalRealFunction(expression) { };
                foreach (var item in sourceDataVs)
                {
                    func.AddVariable(item, new RealDomain(0, 1, 10));
                }
            });

            //函数预处理
            func.Process();

            if (sourceData.Count == 0) throw new Exception("待计算数据为空");

            for (int i = 0; i < count; i++)
            {
                List<double> datatocal = new List<double>();
                foreach (var item in sourceData)
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
            return targetData;
        }


        /// <summary>
        /// 计算二维简单映射
        /// </summary>
        /// <param name="targetData"></param>
        /// <exception cref="Exception"></exception>
        private ChartData2D CalculateSimple2DMapping(string expression, string resultGroupname, string xName, string yName, string zName, int resultxCount, int resultyCount, double xlo, double xhi, double ylo, double yhi, List<ChartData2D> sourceData, List<string> sourceDataVs)
        {
            ChartData2D targetData = new ChartData2D(new FormattedDataSeries2D(resultxCount, xlo, xhi, resultyCount, ylo, yhi) { XName = xName, YName = yName, ZName = zName }) { GroupName = resultGroupname };
            NormalRealFunction func = null;

            Dispatcher.Invoke(() =>
            {
                func = new NormalRealFunction(expression) { };
                foreach (var item in sourceDataVs)
                {
                    func.AddVariable(item, new RealDomain(0, 1, 10));
                }
            });

            //函数预处理
            func.Process();

            if (sourceData.Count == 0) throw new Exception("待计算数据为空");

            for (int i = 0; i < resultxCount; i++)
            {
                for (int j = 0; j < resultyCount; j++)
                {
                    List<double> datatocal = new List<double>();
                    foreach (var item in sourceData)
                    {
                        datatocal.Add(item.Data.GetValue(i, j));
                    }
                    targetData.Data.SetValue(i, j, func.GetValueAt(datatocal).Content.Last());
                }
            }
            return targetData;
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
        private NumricChartData1D CalculatePython1DMapping(string resultGroupname, string resultName, ChartDataType type, int timeout, FuncInstance Func)
        {
            NumricChartData1D targetData = new NumricChartData1D(resultName, resultGroupname, type);
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
            return targetData;
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
    }
}
