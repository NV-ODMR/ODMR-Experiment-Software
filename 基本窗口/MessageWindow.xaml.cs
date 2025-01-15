using System;
using System.Collections.Generic;
using System.IO.Ports;
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
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using Controls;

namespace ODMR_Lab.Windows
{
    /// <summary>
    /// ConnectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWindow : Window
    {

        private MessageBoxResult result = MessageBoxResult.None;

        private bool canclose = true;

        public MessageWindow(string caption, string content, MessageBoxButton option, bool CanClose = false, bool showButton = true, double MaxHeight = double.NaN, ImageSource source = null)
        {
            InitializeComponent();
            canclose = CanClose;
            if (CanClose == false) CloseBtn.IsEnabled = false;

            if (source != null)
            {
                Image.Visibility = Visibility.Visible;
                Image.Source = source;
            }

            canclose = CanClose;
            Caption.Text = caption;
            Content.Text = content;
            Content.Measure(new Size(246, double.PositiveInfinity));
            double height = Content.DesiredSize.Height;
            if (height < Content.MinHeight) height = Content.MinHeight;
            if (Image.Visibility == Visibility.Visible)
            {
                height += Image.Height;
            }
            if (!double.IsNaN(MaxHeight))
            {
                if (height > MaxHeight)
                    height = MaxHeight;
                Information.MaxHeight = height;
                this.MaxHeight = height + 160;
            }
            Information.Height = height;
            Height = height + 160;
            if (showButton)
            {
                if (option == MessageBoxButton.OK)
                {
                    OK.Visibility = Visibility.Visible;
                }
                if (option == MessageBoxButton.YesNo)
                {
                    Yes.Visibility = Visibility.Visible;
                    No.Visibility = Visibility.Visible;
                }
                if (option == MessageBoxButton.YesNoCancel)
                {
                    Yes.Visibility = Visibility.Visible;
                    No.Visibility = Visibility.Visible;
                    Cancel.Visibility = Visibility.Visible;
                }
                if (option == MessageBoxButton.OKCancel)
                {
                    OK.Visibility = Visibility.Visible;
                    Cancel.Visibility = Visibility.Visible;
                }
            }
        }

        public static MessageBoxResult ShowMessageBox(string caption, string content, MessageBoxButton option, bool CanClose = true, bool showBtns = true, Window owner = null, double MaxHeight = double.NaN, ImageSource source = null)
        {
            MessageWindow win = null;
            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                win = new MessageWindow(caption, content, option, CanClose, showBtns, source: source, MaxHeight: MaxHeight);
                if (owner != null)
                {
                    win.Owner = owner;
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                win.ShowDialog();
            });
            return win.result;
        }

        public static void ShowTipWindow(string content, Window Parent)
        {
            ShowMessageBox("提示", content, MessageBoxButton.OK, owner: Parent, MaxHeight: 800);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            if (!canclose) return;
            result = MessageBoxResult.None;
            Close();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).Y < 30)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Close();
            }
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.OK;
            Close();
        }
        private void YesClick(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.Yes;
            Close();
        }
        private void NoClick(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.No;
            Close();
        }
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.Cancel;
            Close();
        }
    }
}
