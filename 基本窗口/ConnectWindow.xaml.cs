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
using HardWares.端口基类;
using ODMR_Lab.Windows;
using HardWares;
using System.Threading;
using ODMR_Lab;
using Controls.Windows;

namespace 温度监控程序.Windows
{
    /// <summary>
    /// ConnectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectWindow : Window
    {

        bool IsConnected = false;

        PortObject obj = null;

        /// <summary>
        /// USB连接名称
        /// </summary>
        public DeviceConnectInfo ConnectInfo { get; private set; } = null;

        public ConnectWindow(Type targetType, Window parentwindow)
        {
            InitializeComponent();
            if (parentwindow != null)
            {
                Owner = parentwindow;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            Dictionary<string, Type> types = PortObject.GetDeviceNamesWithSameBase(targetType);

            USBBtn.Visibility = Visibility.Collapsed;
            COMBtn.Visibility = Visibility.Collapsed;
            TCPBtn.Visibility = Visibility.Collapsed;

            foreach (var item in types)
            {
                //Win设备
                if (typeof(WinUSBOuterInterface).IsAssignableFrom(item.Value))
                {
                    USBDeviceTypeList.Items.Add(new DecoratedButton() { Text = item.Key, Tag = item.Value });
                }
                //COM设备
                if (typeof(COMOuterInterface).IsAssignableFrom(item.Value))
                {
                    COMDeviceTypeList.Items.Add(new DecoratedButton() { Text = item.Key, Tag = item.Value });
                }
            }

            if (USBDeviceTypeList.Items.Count != 0)
            {
                USBBtn.Visibility = Visibility.Visible;
            }
            if (COMDeviceTypeList.Items.Count != 0)
            {
                COMBtn.Visibility = Visibility.Visible;
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public new bool ShowDialog(out PortObject deviceInstance)
        {
            base.ShowDialog();
            deviceInstance = obj;
            return IsConnected;
        }


        /// <summary>
        /// 连接COM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectCOM(object sender, RoutedEventArgs e)
        {
            if (COMDeviceTypeList.SelectedItem == null || COMDeviceList.SelectedItem == null) return;
            COMConnectSucceed.Visibility = Visibility.Hidden;
            COMConnectFalse.Visibility = Visibility.Hidden;
            int value = 9600;
            int.TryParse(BaudRate.Text, out value);

            obj = Activator.CreateInstance(COMDeviceTypeList.SelectedItem.Tag as Type) as PortObject;
            IsConnected = (obj as COMOuterInterface).ConnectCOM(COMDeviceList.Text, value, out Exception ex);
            if (IsConnected)
            {
                COMConnectSucceed.Visibility = Visibility.Visible;

                ConnectInfo = new DeviceConnectInfo(PortType.COM, COMDeviceList.Text, value.ToString());

                Thread t = new Thread(() =>
                {
                    Thread.Sleep(2000);
                    Dispatcher.Invoke(() =>
                    {
                        if (IsActive)
                            Close();
                    });
                });
                t.Start();
            }
            else
            {
                MessageWindow.ShowTipWindow(ex.Message, this);
                COMConnectFalse.Visibility = Visibility.Hidden;
                IsConnected = false;
                obj = null;
            }

        }

        /// <summary>
        /// 连接USB
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectUSB(object sender, RoutedEventArgs e)
        {
            if (USBDeviceList.SelectedItem == null || USBDeviceTypeList == null) return;
            USBConnectSucceed.Visibility = Visibility.Hidden;
            USBConnectFalse.Visibility = Visibility.Hidden;
            obj = Activator.CreateInstance(USBDeviceTypeList.SelectedItem.Tag as Type) as PortObject;

            IsConnected = (obj as WinUSBOuterInterface).ConnectUSB(USBDeviceList.SelectedItem.Text, out Exception ex);

            if (IsConnected)
            {
                USBConnectSucceed.Visibility = Visibility.Visible;

                ConnectInfo = new DeviceConnectInfo(PortType.USB, USBDeviceList.SelectedItem.Text);

                Thread t = new Thread(() =>
                {
                    Thread.Sleep(2000);
                    Dispatcher.Invoke(() =>
                    {
                        if (IsActive)
                            Close();
                    });
                });
                t.Start();
            }
            else
            {
                MessageWindow.ShowTipWindow(ex.Message, this);
                IsConnected = false;
                USBConnectFalse.Visibility = Visibility.Hidden;
                obj = null;
            }

        }



        /// <summary>
        /// 刷新串口列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshCOMPort(object sender, RoutedEventArgs e)
        {
            COMDeviceList.Items.Clear();
            string[] names = SerialPort.GetPortNames();
            foreach (var item in names)
            {
                COMDeviceList.Items.Add(new DecoratedButton() { Text = item });
            }
        }

        /// <summary>
        /// 刷新串口列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshUSBPort(object sender, RoutedEventArgs e)
        {
            USBDeviceList.Items.Clear();
            if (USBDeviceTypeList.Text == "") return;
            object result = Activator.CreateInstance(USBDeviceTypeList.SelectedItem.Tag as Type);
            List<string> names = (result as WinUSBOuterInterface).GetUsbDeviceNames();
            foreach (var item in names)
            {
                USBDeviceList.Items.Add(new DecoratedButton() { Text = item });
            }
        }


        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).Y < 30)
            {
                DragMove();
            }
        }

        private void COMView(object sender, RoutedEventArgs e)
        {
            COMCanvas.Visibility = Visibility.Visible;
            USBCanvas.Visibility = Visibility.Hidden;
            TCPCanvas.Visibility = Visibility.Hidden;
            COMBtn.KeepPressed = true;
            USBBtn.KeepPressed = false;
            TCPBtn.KeepPressed = false;
        }

        private void USBView(object sender, RoutedEventArgs e)
        {
            COMCanvas.Visibility = Visibility.Hidden;
            USBCanvas.Visibility = Visibility.Visible;
            TCPCanvas.Visibility = Visibility.Hidden;
            COMBtn.KeepPressed = false;
            USBBtn.KeepPressed = true;
            TCPBtn.KeepPressed = false;
        }

        private void TCPView(object sender, RoutedEventArgs e)
        {
            COMCanvas.Visibility = Visibility.Hidden;
            USBCanvas.Visibility = Visibility.Hidden;
            TCPCanvas.Visibility = Visibility.Visible;
            COMBtn.KeepPressed = false;
            USBBtn.KeepPressed = false;
            TCPBtn.KeepPressed = true;
        }
    }
}
