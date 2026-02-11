using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.Windows;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.相机_翻转镜;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// EmptyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TakePhotoWindow : Window
    {
        public CameraInfo CameraInfo = null;
        Thread PhotoThread = null;
        bool IsThreadEnd = false;

        public event Action<BitmapSource> PhotoTakenCommand = null;

        public TakePhotoWindow(CameraInfo info)
        {
            InitializeComponent();
            CameraInfo = info;
            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterCloseWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 40);
            CloseBtn.Click += EndThread;
            Title = "拍摄";
            title.Content = "     " + "拍摄";
        }

        public new void Show()
        {
            IsThreadEnd = false;
            if (CameraInfo != null)
            {
                CameraInfo.BeginUse();
                base.Show();
                PhotoThread = new Thread(() =>
                {
                    while (!IsThreadEnd)
                    {
                        try
                        {
                            long tik1 = DateTime.Now.Ticks;

                            BitmapSource image = CameraInfo.Device.GrabFrame(2000);
                            Dispatcher.Invoke(() =>
                            {
                                ImageSource.Source = image;
                            });
                            Thread.Sleep(40);
                            long tik2 = DateTime.Now.Ticks;
                        }
                        catch (Exception e)
                        {
                            Thread.Sleep(40);
                        }
                    }
                });
                PhotoThread.Start();
            }
        }

        private void EndThread(object sender, RoutedEventArgs e)
        {
            CameraInfo.EndUse();
            IsThreadEnd = true;
            while (PhotoThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(50);
            }
        }

        private void TakePhoto(object sender, RoutedEventArgs e)
        {
            BitmapSource source = SnapHelper.GetControlSnap(ImageSource);
            PhotoTakenCommand?.Invoke(source);
            TimeWindow window = new TimeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("照片已保存");
        }

        private void StartVideo(object sender, RoutedEventArgs e)
        {
            StartBtn.KeepPressed = false;
            EndBtn.KeepPressed = true;
        }

        private void EndVideo(object sender, RoutedEventArgs e)
        {
            StartBtn.KeepPressed = true;
            EndBtn.KeepPressed = false;
        }
    }
}
