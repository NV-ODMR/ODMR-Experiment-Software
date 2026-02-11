using CodeHelper;
using Controls;
using Controls.Windows;
using MathLib.NormalMath.Decimal;
using MathLib.NormalMath.Decimal.Function;
using ODMR_Lab.Properties;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using OpenCvSharp.Aruco;
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
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.基本窗口.数据拟合
{
    /// <summary>
    /// CurveFitWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CurveFitWindow : Window
    {
        private FittedData1D Func = null;

        private double[] xs = null;

        private double[] ys = null;

        public CurveFitWindow()
        {
            InitializeComponent();
            FitFuncGroup.TemplateButton = FitFuncGroup;
            FuncName.TemplateButton = FuncName;
            Algorithm.TemplateButton = Algorithm;
            FitFuncGroupEdit.TemplateButton = FitFuncGroupEdit;
            FitFuncGroupNew.TemplateButton = FitFuncGroupNew;
            FuncNameEdit.TemplateButton = FuncNameEdit;

            XDataSourceBox.TemplateButton = XDataSourceBox;
            YDataSourceBox.TemplateButton = YDataSourceBox;


            foreach (var item in Enum.GetNames(typeof(AlgorithmType)))
            {
                Algorithm.Items.Add(new DecoratedButton() { Text = item });
            }

            VariableEdit.AddItem(null, "x", 0, "");
            ExpressionEdit.Variables.Add("x");

            UpdateFuncs();

            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterCloseWindow(this, MinBtn, MaxBtn, CloseBtn, 0, 30);
        }

        public void UpdateFuncs()
        {
            FitFuncGroup.Items.Clear();
            FuncName.Items.Clear();
            FitFuncGroupNew.Items.Clear();
            FitFuncGroupEdit.Items.Clear();
            FuncNameEdit.Items.Clear();

            HashSet<string> groupname = FitFunc.FitFuncs.Select(x => x.GroupName).ToHashSet();
            foreach (var item in groupname)
            {
                FitFuncGroup.Items.Add(new DecoratedButton() { Text = item });
                FitFuncGroupNew.Items.Add(new DecoratedButton() { Text = item });
                FitFuncGroupEdit.Items.Add(new DecoratedButton() { Text = item });
            }
        }

        /// <summary>
        /// 选择面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectPanel(object sender, RoutedEventArgs e)
        {
            NewAndEditBtn.KeepPressed = false;
            SelectBtn.KeepPressed = false;
            (sender as DecoratedButton).KeepPressed = true;
            NewAndEditPanel.Visibility = Visibility.Collapsed;
            SelPanel.Visibility = Visibility.Collapsed;
            if (sender == NewAndEditBtn)
            {
                NewAndEditPanel.Visibility = Visibility.Visible;
            }
            if (sender == SelectBtn)
            {
                SelPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 测试表达式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestEvent(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FuncName.SelectedItem == null) throw new Exception("未选择函数");
                FitFunc func = FuncName.SelectedItem.Tag as FitFunc;
                double result = func.Calculate(double.Parse(Variable.GetCellValue(0, 1) as string));
                Variable.SetCelValue(0, 2, result);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("测试未完成:\n" + ex.Message, this);
            }
        }

        /// <summary>
        /// 修改Group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FuncGroupChanged(object sender, RoutedEventArgs e)
        {
            ComboBox box = sender as ComboBox;

            if (box.SelectedItem == null) return;

            List<FitFunc> funcs = FitFunc.FitFuncs.Where(x => box.SelectedItem.Text == x.GroupName).ToList();

            ComboBox funcNameBox = null;
            if (box == FitFuncGroup)
            {
                funcNameBox = FuncName;
            }
            else
            {
                funcNameBox = FuncNameEdit;
            }
            funcNameBox.Items.Clear();
            foreach (var item in funcs)
            {
                funcNameBox.Items.Add(new DecoratedButton() { Text = item.Name, Tag = item });
            }
        }

        /// <summary>
        /// 显示函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FuncSelectionChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem == null) return;
            FitFunc func = (sender as ComboBox).SelectedItem.Tag as FitFunc;

            UpdatFunctionPanel(func, false);
        }

        private void UpdatFunctionPanel(FitFunc func, bool IsEdit)
        {
            if (IsEdit)
            {
                DescriptionEdit.Text = func.Description;

                ExpressionEdit.Text = func.Function.Expression;

                ExpressionEdit.Variables.Clear();
                ExpressionEdit.Parameters.Clear();

                ExpressionEdit.Variables.Add(func.Var);
                ExpressionEdit.Parameters.AddRange(func.TypicalParams.Select(x => x.Name).ToList());
                ExpressionEdit.UpdateTextDisplay();

                ParamsEdit.ClearItems();

                for (int i = 0; i < func.TypicalParams.Count; i++)
                {
                    ParamsEdit.AddItem(func, func.TypicalParams[i].Name, func.TypicalParams[i].InitValue, func.TypicalParams[i].Range);
                }

                VariableEdit.ClearItems();
                VariableEdit.AddItem(null, "x", 0, "");
            }
            else
            {
                Description.Text = func.Description;

                FunctionExpreDisplay.Text = func.Function.Expression;
                FunctionExpreDisplay.Variables.Clear();
                FunctionExpreDisplay.Parameters.Clear();

                FunctionExpreDisplay.Variables.Add(func.Var);
                FunctionExpreDisplay.Parameters.AddRange(func.TypicalParams.Select(x => x.Name).ToList());
                FunctionExpreDisplay.UpdateTextDisplay();

                ParamsCongig.ClearItems();

                for (int i = 0; i < func.TypicalParams.Count; i++)
                {
                    ParamsCongig.AddItem(func, func.TypicalParams[i].Name, func.TypicalParams[i].InitValue, func.TypicalParams[i].Range);
                }

                Variable.ClearItems();
                Variable.AddItem(null, "x", 0, "");

            }
        }

        private void EditFuncSelectionChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem == null) return;
            FitFunc func = (sender as ComboBox).SelectedItem.Tag as FitFunc;

            UpdatFunctionPanel(func, true);
        }

        private void Fit(object sender, RoutedEventArgs e)
        {
            if (FuncName.SelectedItem == null) return;
            try
            {
                List<double> initvs = new List<double>();
                List<double> ranges = new List<double>();

                //读取参数
                int rowcount = ParamsCongig.GetRowCount();
                for (int i = 0; i < rowcount; i++)
                {
                    initvs.Add(double.Parse(ParamsCongig.GetCellValue(i, 1) as string));
                    ranges.Add(double.Parse(ParamsCongig.GetCellValue(i, 2) as string));
                }

                FitFunc func = FuncName.SelectedItem.Tag as FitFunc;

                if (XDataSourceBox.SelectedItem == null || YDataSourceBox.SelectedItem == null)
                {
                    throw new Exception("未选择数据源");
                }
                if (NumricDataSource.Visibility == Visibility.Visible)
                {
                    xs = (XDataSourceBox.SelectedItem.Tag as ChartData1D).GetDataCopyAsDouble();
                    ys = (YDataSourceBox.SelectedItem.Tag as ChartData1D).GetDataCopyAsDouble();
                }
                if (TimeDataSource.Visibility == Visibility.Visible)
                {
                    var dat = (KeyValuePair<TimeChartData1D, NumricChartData1D>)TimeDataSourceBox.SelectedItem.Tag;
                    xs = dat.Key.GetDataCopyAsDouble();
                    ys = dat.Value.GetDataCopyAsDouble();
                }
                if (xs.Length != ys.Length)
                {
                    throw new Exception("XY数据源的数据个数不相同，无法拟合");
                }

                var dic = func.FitData(xs.ToList(), ys.ToList(), initvs, ranges, (AlgorithmType)Enum.Parse(typeof(AlgorithmType), Algorithm.SelectedItem.Text));

                NormalRealFunction realfunc = new NormalRealFunction(func.Function.Expression);
                foreach (var item in dic)
                {
                    realfunc.AddParam(new KeyValuePair<string, double>(item.Key, item.Value));
                }
                realfunc.AddVariable("x", new RealDomain(xs.Min(), xs.Max(), 200));
                realfunc.Process();
                var xsource = XDataSourceBox.SelectedItem.Tag as ChartData1D;
                Func = new FittedData1D(xsource.Name, xsource.GroupName, realfunc);
                Func.UpdatePoint(xs.Min(), xs.Max(), 200);

                Close();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("拟合未完成。\n" + ex.Message, this);
            }
        }

        public FittedData1D ShowDialog(List<ChartData1D> sources)
        {
            TimeDataSource.Visibility = Visibility.Hidden;
            NumricDataSource.Visibility = Visibility.Visible;
            XDataSourceBox.Items.Clear();
            YDataSourceBox.Items.Clear();
            foreach (var item in sources)
            {
                if (item.IsSelectedAsX)
                {
                    XDataSourceBox.Items.Add(new DecoratedButton() { Text = item.GroupName + "→" + item.Name, Tag = item });
                }
                if (item.IsSelectedAsY)
                {
                    YDataSourceBox.Items.Add(new DecoratedButton() { Text = item.GroupName + "→" + item.Name, Tag = item });
                }
            }
            ShowDialog();
            return Func;
        }

        public FittedData1D ShowDialog(List<KeyValuePair<TimeChartData1D, NumricChartData1D>> timesources)
        {
            TimeDataSource.Visibility = Visibility.Visible;
            NumricDataSource.Visibility = Visibility.Hidden;
            TimeDataSourceBox.Items.Clear();
            foreach (var item in timesources)
            {
                if (item.Value.IsSelectedAsX)
                {
                    TimeDataSourceBox.Items.Add(new DecoratedButton() { Text = item.Value.GroupName + "→" + item.Value.Name, Tag = item });
                }
            }
            ShowDialog();
            return Func;
        }

        #region 新建和编辑函数
        private void FitGroupEditSelection(object sender, RoutedEventArgs e)
        {
            if ((sender as Chooser).IsSelected)
            {
                FitFuncGroupNewText.Visibility = Visibility.Visible;
                FitFuncGroupNew.Visibility = Visibility.Collapsed;
            }
            else
            {
                FitFuncGroupNewText.Visibility = Visibility.Collapsed;
                FitFuncGroupNew.Visibility = Visibility.Visible;
            }
        }

        private void ChangeAlgebraParam(int arg1, int arg2, object arg3)
        {
            if (arg2 != 0) return;
            ExpressionEdit.Parameters[arg1] = arg3 as string;
            ExpressionEdit.UpdateTextDisplay();
        }

        private FitFunc LoadFunc(bool IsNew)
        {
            #region 表达式
            string expression = ExpressionEdit.Text;
            int column = ParamsEdit.GetRowCount();
            #endregion

            #region 参数
            List<FitParam> ps = new List<FitParam>();
            for (int i = 0; i < column; i++)
            {
                string tname = (string)ParamsEdit.GetCellValue(i, 0);
                double init = double.Parse(ParamsEdit.GetCellValue(i, 1) as string);
                double range = double.Parse(ParamsEdit.GetCellValue(i, 2) as string);
                if (double.IsNaN(init) || double.IsNaN(range))
                {
                    throw new Exception("参数中存在非法值");
                }
                if (tname == "")
                {
                    throw new Exception("参数名为空");
                }

                ps.Add(new FitParam() { Name = tname, InitValue = init, Range = range });
            }

            IEnumerable<string> names = ps.Select(x => x.Name);
            if (names.ToHashSet().Count != names.Count())
            {
                throw new Exception("存在重名参数");
            }
            #endregion

            #region 变量
            string varname = (string)VariableEdit.GetCellValue(0, 0);
            if (varname == "")
            {
                throw new Exception("变量名为空");
            }
            #endregion

            #region 描述
            string descript = DescriptionEdit.Text;
            if (descript == "")
            {
                throw new Exception("描述内容不能为空");
            }
            #endregion

            #region 分组名
            string gname = "";
            string name = "";
            if (IsNew)
            {
                if (!IsGroupCustom.IsSelected)
                {
                    if (FitFuncGroupEdit.SelectedItem != null)
                    {
                        gname = FitFuncGroupEdit.SelectedItem.Text;
                    }
                    if (FuncNameEdit.SelectedItem != null)
                    {
                        name = FitFuncGroupEdit.SelectedItem.Text;
                    }
                }
                else
                {
                    gname = FitFuncGroupNewText.Text;
                    name = FuncNameNew.Text;
                }
            }
            else
            {
                if (FitFuncGroupEdit.SelectedItem == null || FuncNameEdit == null) throw new Exception("未选择函数");
                gname = FitFuncGroup.SelectedItem.Text;
                name = FuncNameEdit.SelectedItem.Text;
            }
            if (gname == "" || name == "") throw new Exception("分组名及函数名不能为空");

            return new FitFunc(expression, name, ps, varname, descript, gname);
        }

        #region 新建函数
        private void AddFunc(object sender, RoutedEventArgs e)
        {
            try
            {
                FitFunc func = LoadFunc(true);

                if (FitFunc.FitFuncs.Where(x => x.Name == func.Name).Count() != 0)
                {
                    throw new Exception("已存在同名拟合函数");
                }

                #endregion

                func.WriteToFile();
                FitFunc.FitFuncs.Add(func);
                TimeWindow win = new TimeWindow();
                win.Owner = this;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                win.ShowWindow("函数已保存");
                //刷新函数
                UpdateFuncs();

            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("参数格式不正确，拟合函数未添加。\n" + ex.Message, this);
            }
        }
        #endregion

        #region 保存函数
        private void SaveFunc(object sender, RoutedEventArgs e)
        {
            if (MessageWindow.ShowMessageBox("提示", "此操作将保存参数的初始值和范围，是否要继续?", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
            {
                try
                {
                    FitFunc originfunc = FuncNameEdit.Tag as FitFunc;
                    FitFunc func = LoadFunc(false);

                    originfunc.DeleteFromFile();
                    func.WriteToFile();
                    FitFunc.FitFuncs.Remove(originfunc);
                    FitFunc.FitFuncs.Add(func);

                    TimeWindow win = new TimeWindow();
                    win.Owner = this;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("函数已保存");
                    //刷新函数
                    UpdateFuncs();

                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("参数格式不正确，拟合函数未保存。\n" + ex.Message, this);
                }
            }
        }

        private void DeleteFit(object sender, RoutedEventArgs e)
        {
            if (FuncName.SelectedItem == null) return;

            if (MessageWindow.ShowMessageBox("提示", "此操作不可恢复，是否要继续?", MessageBoxButton.YesNo, owner: this) == MessageBoxResult.Yes)
            {
                try
                {
                    FitFunc func = FuncName.SelectedItem.Tag as FitFunc;
                    func.DeleteFromFile();
                    FitFunc.FitFuncs.Remove(func);
                    UpdateFuncs();

                    TimeWindow win = new TimeWindow();
                    win.Owner = this;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("函数已删除");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("拟合函数未删除。\n" + ex.Message, this);
                }
            }
        }
        #endregion

        private void SelectEditAndNewPanel(object sender, RoutedEventArgs e)
        {
            NewBtn.KeepPressed = false;
            EditBtn.KeepPressed = false;
            (sender as DecoratedButton).KeepPressed = true;
            NewPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            if (sender == NewBtn)
            {
                NewPanel.Visibility = Visibility.Visible;
            }
            if (sender == EditBtn)
            {
                EditPanel.Visibility = Visibility.Visible;
            }
        }

        private void TestEditEvent(object sender, RoutedEventArgs e)
        {
            try
            {
                FitFunc func = LoadFunc(true);
                double result = func.Calculate(double.Parse(VariableEdit.GetCellValue(0, 1) as string));
                VariableEdit.SetCelValue(0, 2, result);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("测试未完成:\n" + ex.Message, this);
            }
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddParam(object sender, RoutedEventArgs e)
        {
            ParamsEdit.AddItem(null, "p", 0, 0);
            ExpressionEdit.Parameters.Add("p");
            ExpressionEdit.UpdateTextDisplay();
        }

        private void DeleteParam(int arg1, int arg2, object arg3)
        {
            ParamsEdit.RemoveItemAt(arg2);
            ExpressionEdit.Parameters.RemoveAt(arg1);
            ExpressionEdit.UpdateTextDisplay();
        }

        #endregion
    }
}
