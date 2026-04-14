using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using ODMR_Lab.设备部分.相机_翻转镜_开关;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace ODMR_Lab.设备部分.相机_翻转镜
{
    /// <summary>
    /// CameraWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CameraWindow : Window
    {
        DevicePage CameraPage { get; set; } = null;

        CameraInfo Camera = null;

        public CameraWindow(DevicePage camera, CameraInfo info)
        {
            InitializeComponent();

            CameraPage = camera;
            Camera = info;


            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterCloseWindow(this, MinBtn, MaxBtn, CloseBtn, 5, 30);
            hel.BeforeClose += BeforeClose;

            try
            {
                CreateCameraThread();
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("相机正在使用", this);
                BeforeClose(null, new RoutedEventArgs());
                Close();
            }
        }

        private void BeforeClose(object sender, RoutedEventArgs e)
        {
            CancelThread();
            Camera.DisplayWindow = null;
        }

        Thread CameraThread = null;
        bool isThreadEnd = false;

        bool isMarkerLoaded = false;

        private void CreateCameraThread()
        {
            Camera?.BeginUse();
            CameraThread = new Thread(() =>
            {
                while (!isThreadEnd)
                {
                    try
                    {
                        long tik1 = DateTime.Now.Ticks;

                        BitmapSource image = Camera.Device.GrabFrame(2000);
                        Dispatcher.Invoke(() =>
                        {
                            image = ClipImage(image, ClipImageBound);
                            DisplayArea.SetSource(image);
                            DrawingPanel.UpdateLayout();
                        });
                        Thread.Sleep(40);
                        long tik2 = DateTime.Now.Ticks;
                        //计算帧率
                        Dispatcher.Invoke(() =>
                        {
                            Framerate.Content = "帧率:" + Math.Round(1000.0 / ((tik2 - tik1) / 10000.0), 3).ToString() + "\t" + "未捕获帧数:" + Camera.Device.BrokenFrameCount.ToString();
                            if (image != null)
                            {
                                //导入标签文件
                                if (!isMarkerLoaded)
                                {
                                    if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, "CameraMarkerFile")))
                                    {
                                        var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "CameraMarkerFile"));
                                        foreach (var item in files)
                                        {
                                            try
                                            {
                                                var descs = FileObject.ReadDescription(item);
                                                if (descs["DeviceName"] == Camera.Device.ProductName)
                                                {
                                                    DisplayArea.ReadFromMarkersFile(FileObject.ReadFromFile(item));
                                                    break;
                                                }
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                    }
                                    isMarkerLoaded = true;
                                }
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(40);
                    }
                }
            });
            CameraThread.Start();
        }

        public void CancelThread()
        {
            isThreadEnd = true;
            while (CameraThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(30);
            }
            //保存标签文件
            FileObject obj = new FileObject();
            obj.Descriptions.Add("DeviceName", Camera.Device.ProductName);
            obj = DisplayArea.GenerateMarkersFile(obj);
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "CameraMarkerFile")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "CameraMarkerFile"));
            }
            obj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "CameraMarkerFile", FileHelper.ProcessFileStr(Camera.Device.ProductName)));
            Camera?.EndUse();
        }

        /// <summary>
        /// 截图到剪切板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SnapToClipboard(object sender, RoutedEventArgs e)
        {
            BitmapSource image = SnapHelper.GetControlAreaSnap(DisplayArea.DisplayArea);
            System.Windows.Clipboard.SetImage(image);
            TimeWindow window = new TimeWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        /// <summary>
        /// 刷新连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshConnection(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CameraPage == null) return;
                double x = Camera.DisplayWindow.Left;
                double y = Camera.DisplayWindow.Top;
                double width = Camera.DisplayWindow.ActualWidth;
                double height = Camera.DisplayWindow.ActualHeight;
                WindowState state = Camera.DisplayWindow.WindowState;
                Camera?.EndUse();
                string path = Camera.CloseDeviceInfoAndSaveParams(out bool result);
                if (result == false) { return; }

                int ind = CameraPage.Cameras.IndexOf(Camera);

                var newcam = (CameraInfo)CameraInfo.OpenAndLoadParams(path, Camera.Device.GetType());
                CameraPage.Cameras[ind] = newcam;
                CameraPage.CameraList.SetTag(ind, newcam);
                newcam.DisplayWindow = new CameraWindow(CameraPage, newcam);
                newcam.DisplayWindow.Left = x;
                newcam.DisplayWindow.Top = y;
                newcam.DisplayWindow.Width = width;
                newcam.DisplayWindow.Height = height;
                newcam.DisplayWindow.WindowState = state;
                newcam.DisplayWindow.Show();
            }
            catch (Exception exc)
            {
                try
                {
                    Camera?.BeginUse();
                }
                catch (Exception)
                {

                }
            }
        }

        private void SetParams(object sender, RoutedEventArgs e)
        {
            ParameterWindow window = new ParameterWindow(Camera.Device, Window.GetWindow(this));
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }


        private void ImageProcess(object sender, RoutedEventArgs e)
        {
            ImageProcessWindow win = new ImageProcessWindow(Camera);
            win.ShowDialog();
        }

        MarkerBase marker = null;
        /// <summary>
        /// 绘制标记
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Paint(object sender, RoutedEventArgs e)
        {
            string name = (sender as DecoratedButton).Name;
            if (name == "PointBtn")
            {
                marker = new PointMarker();
                DisplayArea.CreateMarkerAndEdit(marker);
            }
            if (name == "CircleBtn")
            {
                marker = new CircleMarker();
                DisplayArea.CreateMarkerAndEdit(marker);
            }
            if (name == "CenterCircleBtn")
            {
                marker = new CenterCircleMarker();
                DisplayArea.CreateMarkerAndEdit(marker);
            }
            if (name == "RectangleBtn")
            {
                marker = new RectangleMarker();
                DisplayArea.CreateMarkerAndEdit(marker);
            }
            if (name == "CenterRectangleBtn")
            {
                marker = new CenterRectangleMarker();
                DisplayArea.CreateMarkerAndEdit(marker);
            }
            marker.MarkerConfirmedEvent += (s, ev) =>
            {
                MarkersPanel.IsEnabled = true;
                FunctionPanel.IsEnabled = true;
            };
            MarkersPanel.IsEnabled = false;
            FunctionPanel.IsEnabled = false;
        }

        /// <summary>
        /// 设置拍摄区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSnapArea(object sender, RoutedEventArgs e)
        {
            BeginResize();
        }

        #region 相机区域调整

        public void BeginResize()
        {
            IsResize = true;
            MarkersPanel.IsEnabled = false;
            FunctionPanel.IsEnabled = false;
        }

        public void EndResize()
        {
            IsResize = false;
            MarkersPanel.IsEnabled = true;
            FunctionPanel.IsEnabled = true;
            ResizeBound.Visibility = Visibility.Hidden;
        }

        Rect ClipImageBound = Rect.Empty;

        bool IsResize = false;

        Point beginPoint = new Point();

        /// <summary>
        /// 开始设定范围
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeginResize(object sender, MouseButtonEventArgs e)
        {
            if (IsResize == true)
            {
                beginPoint = e.GetPosition(DrawingPanel);
                ResizeBound.Visibility = Visibility.Visible;
                Canvas.SetLeft(ResizeBound, beginPoint.X);
                Canvas.SetTop(ResizeBound, beginPoint.Y);
                ResizeBound.Width = 0;
                ResizeBound.Height = 0;
                DisplayArea.CaptureMouse();
            }
        }

        private void DragResize(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsResize)
            {
                DisplayArea.Cursor = Cursors.Pen;
            }
            else
            {
                DisplayArea.Cursor = Cursors.Arrow;
            }
            if (e.LeftButton == MouseButtonState.Pressed && IsResize)
            {
                Point p = e.GetPosition(DrawingPanel);
                Rect re = new Rect(p, beginPoint);
                Canvas.SetLeft(ResizeBound, re.Left);
                Canvas.SetTop(ResizeBound, re.Top);
                ResizeBound.Width = re.Width;
                ResizeBound.Height = re.Height;
            }
        }

        private void EndResize(object sender, MouseButtonEventArgs e)
        {
            if (IsResize)
            {
                DisplayArea.ReleaseMouseCapture();
                //刷新边框
                Point p1 = DrawingPanel.TranslatePoint(beginPoint, DisplayArea);
                Point p2 = DrawingPanel.TranslatePoint(e.GetPosition(DrawingPanel), DisplayArea);
                GetRatioPoint(DisplayArea.DisplayArea, p1, out Point rp1);
                GetRatioPoint(DisplayArea.DisplayArea, p2, out Point rp2);
                ClipImageBound = new Rect(rp1, rp2);
                EndResize();
            }
        }

        private void SetRange(object sender, RoutedEventArgs e)
        {
            ClipImageBound = Rect.Empty;
            ResizeBound.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// 裁剪图片
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="clipRange"></param>
        /// <returns></returns>
        private BitmapSource ClipImage(BitmapSource origin, Rect clipRange)
        {
            BitmapSource res = null;
            Dispatcher.Invoke(() =>
            {
                if (!clipRange.IsEmpty)
                {
                    int x1 = (int)(clipRange.Left * origin.PixelWidth);
                    int x2 = (int)(clipRange.Right * origin.PixelWidth);
                    int y1 = (int)(clipRange.Top * origin.PixelHeight);
                    int y2 = (int)(clipRange.Bottom * origin.PixelHeight);
                    Int32Rect cutrect = new Int32Rect(x1, y1, x2 - x1, y2 - y1);
                    CroppedBitmap cb = new CroppedBitmap(origin, cutrect);
                    res = cb;
                    return;
                }
                res = origin;
            });
            return res;
        }

        private void GetRatioPoint(Image image, Point p, out Point ratioPoint)
        {
            ratioPoint = new Point(p.X / image.ActualWidth, p.Y / image.ActualHeight);
        }

        /// <summary>
        /// 取消设置大小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelResize(object sender, MouseButtonEventArgs e)
        {
            EndResize();
        }

        #endregion
    }
}
