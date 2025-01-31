using Controls.Windows;
using HardWares.温度控制器.SRS_PTC10;
using Microsoft.Win32;
using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.温度监测部分
{
    /// <summary>
    /// aveWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SaveWindow : Window
    {
        private TemperaturePage page = null;

        public SaveWindow(TemperaturePage page)
        {
            this.page = page;
            InitializeComponent();
            if (AutoChooser.IsSelected)
            {
                AutoOptions.IsEnabled = true;
            }
            else
            {
                AutoOptions.IsEnabled = false;
            }

            DateTime time = page.HistorySaveTime;
            LastSaveTime.Content = time.Year.ToString() + "年" + time.Month.ToString() + "月" + time.Day.ToString() + "日" + "   " + time.ToString("HH:mm:ss");
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).Y < 30)
            {
                DragMove();
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 浏览文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Folder(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dia = new FolderBrowserDialog();
            if (dia.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Path.Content = dia.SelectedPath;
                Path.ToolTip = dia.SelectedPath;
            }
        }

        private void AutoFolder(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dia = new FolderBrowserDialog();
            if (dia.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AutoPath.Content = dia.SelectedPath;
                AutoPath.ToolTip = dia.SelectedPath;
            }
        }

        private void Information(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("温度控制终端以文件夹形式保存数据，文件夹名称为：仪器（温度控制器）型号&起始记录时间（yy-mm-dd hh-mm-ss-fff）&终止记录时间（yy-mm-dd hh-mm-ss-fff）；文件夹内文件(.data)的名称为通道名，文件个数等于接收数据的通道个数。", this);
        }

        private void AutoInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("当自动保存功能开启时，系统每隔一段时间保存现有数据到文件夹中，同时从运行内存中清空所有数据。", this);
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                page.Save(Path.Content.ToString());
            }
            catch (Exception exc)
            {
                MessageWindow.ShowTipWindow(exc.Message, this);
            }
        }

        private void AutoChooser_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (AutoChooser.IsSelected)
            {
                AutoOptions.IsEnabled = true;
            }
            else
            {
                AutoOptions.IsEnabled = false;
            }
        }

        private void SetGap(object sender, RoutedEventArgs e)
        {
            TimeWindow window = new TimeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (double.TryParse(AutoSaveGap.Text, out double value))
            {
                window.ShowWindow("设置成功");
            }
            else
            {
                MessageWindow.ShowTipWindow("自动保存间隔值必须是数字", this);
            }
        }

    }
}
