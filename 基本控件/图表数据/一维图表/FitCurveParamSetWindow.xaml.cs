using CodeHelper;
using Controls.Windows;
using System;
using System.Windows;

namespace ODMR_Lab.基本控件.一维图表
{
    /// <summary>
    /// FitCurveParamSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FitCurveParamSetWindow : Window
    {
        public double Lo { get; private set; } = 0;

        public double Hi { get; private set; } = 0;

        public int Count { get; private set; } = 0;


        public FitCurveParamSetWindow()
        {
            InitializeComponent();

            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterCloseWindow(this, MinBtn, null, CloseBtn, null, 4, 40);
        }

        public void ShowDialog(double lo, double hi, int count)
        {
            Lo = lo;
            Hi = hi;
            Count = count;

            Lower.Text = lo.ToString();
            Upper.Text = hi.ToString();
            PointCount.Text = count.ToString();
            ShowDialog();
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            try
            {
                Lo = double.Parse(Lower.Text);
                Hi = double.Parse(Upper.Text);
                Count = int.Parse(PointCount.Text);
                Close();

            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("参数格式错误", this);
            }
        }
    }
}
