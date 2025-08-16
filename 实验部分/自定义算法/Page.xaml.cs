using CodeHelper;
using Controls;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Xml.Linq;
using Path = System.IO.Path;
using MathNet.Numerics.Distributions;
using System.Diagnostics.Contracts;
using MathNet.Numerics;
using System.Windows.Forms;
using ContextMenu = Controls.ContextMenu;
using ODMR_Lab.实验部分.样品定位;
using Label = System.Windows.Controls.Label;
using ODMR_Lab.基本控件;
using Clipboard = System.Windows.Clipboard;
using Controls.Windows;
using Window = System.Windows.Window;
using TextBox = System.Windows.Controls.TextBox;
using ODMR_Lab.实验部分.自定义算法;
using ODMR_Lab.实验部分.ODMR实验;

namespace ODMR_Lab.自定义算法
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public override string PageName { get; set; } = "自定义算法";
        public CorrelatePointCollection CorrelatePointCollection { get; set; } = new CorrelatePointCollection();

        public DisplayPage()
        {
            InitializeComponent();
            //加载算法
            var algorithms = ClassHelper.GetSubClassTypes(typeof(AlgorithmBase));
            foreach (var item in algorithms)
            {
                var alg = Activator.CreateInstance(item) as AlgorithmBase;
                Algorithms.Add(alg);
                AlgorithmNames.AddItem(alg, alg.AlgorithmName);
            }
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }

        List<AlgorithmBase> Algorithms = new List<AlgorithmBase>();
        AlgorithmBase currentAngorithm = null;
        AlgorithmBase CurrentAngorithm
        {
            get
            {
                return currentAngorithm;
            }
            set
            {
                currentAngorithm = value;
                if (currentAngorithm == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        CalcBtn.Visibility = Visibility.Hidden;
                        GraphBtn.Visibility = Visibility.Hidden;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        CalcBtn.Visibility = Visibility.Visible;
                        GraphBtn.Visibility = Visibility.Visible;
                    });
                }
            }
        }

        public void UpdateInputParams()
        {
            HashSet<string> gnames = CurrentAngorithm.InputParams.Select(x => x.GroupName).ToHashSet();
            foreach (var item in gnames)
            {
                TextBlock l = new TextBlock() { Text = item };
                l.Height = 30;
                UIUpdater.CloneStyle(TextBlockTemplate, l);
                InputParams.Children.Add(l);
                var ps = CurrentAngorithm.InputParams.Where(x => x.GroupName == item);
                foreach (var p in ps)
                {
                    Grid g = ExpParamWindow.GenerateControlBar(p, this, true);
                    g.Margin = new Thickness(10);
                    InputParams.Children.Add(g);
                    p.LoadToPage(new FrameworkElement[] { this }, false);
                }
            }
        }

        public void UpdateOutputParams()
        {
            HashSet<string> gnames = CurrentAngorithm.OutputParams.Select(x => x.GroupName).ToHashSet();
            foreach (var item in gnames)
            {
                TextBlock l = new TextBlock() { Text = item };
                l.Height = 30;
                UIUpdater.CloneStyle(TextBlockTemplate, l);
                OutputParams.Children.Add(l);
                var ps = CurrentAngorithm.OutputParams.Where(x => x.GroupName == item);
                foreach (var p in ps)
                {
                    Grid g = ExpParamWindow.GenerateControlBar(p, this, false);
                    OutputParams.Children.Add(g);
                    p.LoadToPage(new FrameworkElement[] { this }, false);
                }
            }
        }

        public void SelectAlgorithm(int index)
        {
            //存储上一个实验的参数
            if (CurrentAngorithm != null)
            {
                foreach (var item in CurrentAngorithm.InputParams)
                {
                    try
                    {
                        item.ReadFromPage(new FrameworkElement[] { this }, false);
                        UnregisterName(ExpParamWindow.GetValidName(item.PropertyName));
                    }
                    catch (Exception ex) { }
                }
                foreach (var item in CurrentAngorithm.OutputParams)
                {
                    try
                    {
                        item.ReadFromPage(new FrameworkElement[] { this }, false);
                        UnregisterName(ExpParamWindow.GetValidName(item.PropertyName));
                    }
                    catch (Exception) { }
                }
            }

            CurrentAngorithm = Algorithms[index];
            //更新新的参数
            InputParams.Children.Clear();
            OutputParams.Children.Clear();

            CurrentAngorithm = Algorithms[index];
            UpdateInputParams();
            UpdateOutputParams();
        }

        private void Calculate(object sender, RoutedEventArgs e)
        {
            try
            {
                //读取输入参数
                foreach (var item in CurrentAngorithm.InputParams)
                {
                    item.ReadFromPage(new FrameworkElement[] { this }, true);
                }
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("未能成功读取参数,请检查参数格式.", Window.GetWindow(this));
                return;
            }
            //清除原有输出参数
            foreach (var item in CurrentAngorithm.OutputParams)
            {
                try
                {
                    item.ReadFromPage(new FrameworkElement[] { this }, false);
                    UnregisterName(ExpParamWindow.GetValidName(item.PropertyName));
                }
                catch (Exception) { }
            }
            OutputParams.Children.Clear();
            CurrentAngorithm.OutputParams.Clear();
            try
            {
                //计算
                CurrentAngorithm.CalculateFunc();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("计算未完成," + ex.Message, Window.GetWindow(this));
            }
            //更新输出
            UpdateOutputParams();
        }

        private void AlgorithmNames_ItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            MessageWindow.ShowTipWindow((arg3 as AlgorithmBase).AlgorithmDescription, Window.GetWindow(this));
        }

        private void ShowGraph(object sender, RoutedEventArgs e)
        {

        }

        private void AlgorithmNames_ItemSelected(int arg1, object arg2)
        {
            try
            {
                SelectAlgorithm(arg1);
            }
            catch (Exception)
            {
            }
        }
    }
}
