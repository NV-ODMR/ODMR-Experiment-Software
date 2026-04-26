using Controls;
using Controls.Charts;
using Controls.Windows;
using MathLib.NormalMath.Decimal.Function;
using ODMR_Lab.基本控件;
using PythonHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace ODMR_Lab.数据处理.数据处理窗口.数据处理方法界面.Python脚本映射计算
{
    /// <summary>
    /// Page.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessPage : ProcesspageBase
    {
        public override string ProcessName { get; set; } = "Python脚本映射计算";

        public ProcessPage()
        {
            InitializeComponent();
        }

        public override ChartDataBase CalculateMethod()
        {
            FuncInstance inst = null;
            int time = 0;
            Exception exp = null;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (PyhtonFunc.SelectedItem == null)
                {
                    exp = new Exception("所选函数不能为空");
                    return;
                }
                try
                {
                    time = int.Parse(PythonTimeout.Text);
                }
                catch (Exception)
                {
                    exp = new Exception("超时时间格式错误");
                    return;
                }
                inst = PyhtonFunc.SelectedItem.Tag as FuncInstance;
            });
            if (exp != null) throw exp;

            return CalculatePythonD1Mapping(time, inst);
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
                PythonFileDir.Text = dlg.FileName;
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
                List<FuncInstance> funcs = Python_NetInterpretor.GetFuncs(PythonFileDir.Text.ToString());
                PyhtonFunc.Items.Clear();
                foreach (var item in funcs)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item.FuncName };
                    UIUpdater.SetDefaultTemplate(btn);
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
                InputParam.AddItem(null, item, "");
            }
        }

        public override void UpdateMethod()
        {
        }

        /// <summary>
        /// 获取输入数据，找不到报错
        /// </summary>
        /// <param name="isD1Data"></param>
        /// <returns></returns>
        private List<KeyValuePair<string, List<double>>> GetInputData()
        {
            List<KeyValuePair<string, List<double>>> result = new List<KeyValuePair<string, List<double>>>();

            int rows = InputParam.GetRowCount();
            for (int i = 0; i < rows; i++)
            {
                string varname = InputParam.GetCellValue(i, 1) as string;
                if (varname.Contains("O"))
                {
                    var varind = int.Parse(varname.Replace("O", ""));
                    result.Add(new KeyValuePair<string, List<double>>((InputParam.GetCellValue(i, 0) as string), ParentDataSource.ChartDataSource1D[varind].GetDataCopyAsDouble().ToList()));
                }
                else
                {
                    throw new Exception("输入数据格式错误，注意：仅支持一维数据的运算");
                }
            }
            return result;
        }

        /// <summary>
        /// 计算Python脚本
        /// </summary>
        private NumricChartData1D CalculatePythonD1Mapping(int timeout, FuncInstance Func)
        {
            NumricChartData1D targetData = new NumricChartData1D("", "");

            Exception exc = null;
            List<KeyValuePair<string, List<double>>> inputs = new List<KeyValuePair<string, List<double>>>();
            Dispatcher.Invoke(() =>
            {
                try
                {
                    //根据输入的变量名获取数据
                    inputs = GetInputData();
                }
                catch (Exception ex)
                {
                    exc = ex;
                }
            });
            if (exc != null) throw exc;

            targetData.Data.AddRange((Func.Excute(timeout, inputs.Select(x => (object)x.Value).ToList()) as List<object>).Select(x => (double)x).ToList());
            return targetData;
        }
    }
}
