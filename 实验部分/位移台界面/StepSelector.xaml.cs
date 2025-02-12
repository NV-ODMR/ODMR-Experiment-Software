using Controls.Windows;
using HardWares;
using ODMR_Lab.位移台部分;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ODMR_Lab.实验部分.位移台界面
{
    /// <summary>
    /// StepSelector.xaml 的交互逻辑
    /// </summary>
    public partial class StepSelector : Grid
    {
        public StepSelector()
        {
            InitializeComponent();
        }

        public double CurrentValue { get; private set; } = 0.001;

        private void Select(object sender, RoutedEventArgs e)
        {
            Btn1.KeepPressed = false;
            Btn2.KeepPressed = false;
            Btn3.KeepPressed = false;
            Btn4.KeepPressed = false;
            CustomBtn.KeepPressed = false;

            if (sender == Btn1)
            {
                CurrentValue = double.Parse(Btn1.Text.ToString());
                Btn1.KeepPressed = true;
            }
            if (sender == Btn2)
            {
                CurrentValue = double.Parse(Btn2.Text.ToString());
                Btn2.KeepPressed = true;
            }
            if (sender == Btn3)
            {
                CurrentValue = double.Parse(Btn3.Text.ToString());
                Btn3.KeepPressed = true;
            }
            if (sender == Btn4)
            {
                CurrentValue = double.Parse(Btn4.Text.ToString());
                Btn4.KeepPressed = true;
            }
            if (sender == CustomBtn)
            {
                try
                {
                    CurrentValue = double.Parse(CustomValue.Text);
                    CustomBtn.KeepPressed = true;
                }
                catch (Exception)
                {
                    MessageWindow.ShowTipWindow("自定义步长必须为数字", Window.GetWindow(this));
                    Btn1.KeepPressed = true;
                    CurrentValue = double.Parse(Btn1.Text.ToString());
                }
            }
        }
    }
}
