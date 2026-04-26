using CodeHelper;
using Controls;
using ODMR_Lab.基本控件;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.数据处理
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessedDataSaveWindow : Window
    {
        public ChartDataBase Data = null;

        public bool IsChanged = false;

        public ProcessedDataSaveWindow()
        {
            InitializeComponent();
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterHideWindow(this, MinBtn, MaxBtn, CloseBtn, PinBtn, 5, 40);
            hel.BeforeHide += new RoutedEventHandler((s, e) =>
            {
                AfterHided?.Invoke(this);
            });
            D1AxisTypeBox.Items.Clear();
            foreach (var item in Enum.GetNames(typeof(ChartDataType)))
            {
                DecoratedButton btn = new DecoratedButton() { Text = item };
                UIUpdater.ButtonTemplate.CloneStyleTo(btn);
                D1AxisTypeBox.Items.Add(btn);
            }
        }

        public Action<ProcessedDataSaveWindow> AfterHided = null;

        public new void ShowDialog(ChartDataBase data)
        {
            Data = data;
            IsChanged = false;
            if (data is ChartData1D)
            {
                D1Panel.Visibility = Visibility.Visible;
                D2Panel.Visibility = Visibility.Hidden;
            }
            if (data is ChartData2D)
            {
                D2Panel.Visibility = Visibility.Visible;
                D1Panel.Visibility = Visibility.Hidden;
            }
            Show();
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            try
            {
                //设置数据
                if (Data != null)
                {
                    if (D1Panel.Visibility == Visibility.Visible)
                    {
                        if (D1AxisTypeBox.SelectedItem == null || D1DataName.Text == "") return;
                        (Data as ChartData1D).Name = D1DataName.Text;
                        (Data as ChartData1D).GroupName = "计算数据";
                        (Data as ChartData1D).DataAxisType = (ChartDataType)Enum.Parse(typeof(ChartDataType), D1AxisTypeBox.SelectedItem.Text);
                        IsChanged = true;
                    }
                    if (D2Panel.Visibility == Visibility.Visible)
                    {
                        if (D2XName.Text == "" || D2YName.Text == "" || D2ZName.Text == "") return;
                        (Data as ChartData2D).Data.XName = D2XName.Text;
                        (Data as ChartData2D).Data.YName = D2YName.Text;
                        (Data as ChartData2D).Data.ZName = D2ZName.Text;
                        (Data as ChartData2D).GroupName = "计算数据";
                        IsChanged = true;
                    }
                }
            }
            catch (Exception)
            {
            }
            Close();
        }
    }
}
