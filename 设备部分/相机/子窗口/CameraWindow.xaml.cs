using CodeHelper;
using Controls;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本窗口;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using Point = System.Windows.Point;

namespace ODMR_Lab.相机
{
    /// <summary>
    /// CameraWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CameraWindow : Window
    {
        CameraInfo Camera { get; set; } = null;

        public CameraWindow(CameraInfo camera)
        {
            InitializeComponent();

            Camera = camera;

            WindowResizeHelper hel = new WindowResizeHelper();
            hel.RegisterWindow(this, 5, 30);

            InitCursorSettings();

            CreateCameraThread();

            CameraPixelHeight = camera.Device.CameraPixelHeightCount;
            CameraPixelHeight = camera.Device.CameraPixelWidthCount;
        }

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Maximize(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                return;
            }
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                return;
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            CancelThread();
            Camera.DisplayWindow = null;
            Close();
        }

        #region 光标设置

        private void InitCursorSettings()
        {
            ContextMenu menu = new ContextMenu();
            menu.ItemHeight = 30;
            menu.PanelMinWidth = 70;
            DecoratedButton btn = new DecoratedButton() { Text = "添加标记点" };
            SnapBtn.CloneStyleTo(btn);
            btn.Click += AddCursor;
            menu.Items.Add(btn);
            btn = new DecoratedButton() { Text = "设置拍摄区域" };
            SnapBtn.CloneStyleTo(btn);
            btn.Click += EnableResize;
            menu.Items.Add(btn);
            menu.ApplyToControl(DisplayArea);
        }

        private void AddCursor(object sender, RoutedEventArgs e)
        {
            ImageLabel ell = DisplayArea.AddCursor(pos);

            ContextMenu menu = new ContextMenu();
            menu.PanelMinWidth = 50;
            DecoratedButton btn = new DecoratedButton() { Text = "删除" };
            btn.Height = 30;
            SnapBtn.CloneStyleTo(btn);
            btn.Tag = ell;
            btn.Click += RemoveCursour;
            menu.Items.Add(btn);
            menu.ApplyToControl(ell.Geometry);
        }

        private void CursorDragEnd(object sender, MouseButtonEventArgs e)
        {
            (sender as Ellipse).ReleaseMouseCapture();
        }


        private void RemoveCursour(object sender, RoutedEventArgs e)
        {
            ImageLabel po = (sender as DecoratedButton).Tag as ImageLabel;
            DisplayArea.RemoveCursour(po);
        }

        private Point pos = new Point();
        /// <summary>
        /// 记录右键点击的位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordClickPos(object sender, MouseButtonEventArgs e)
        {
            pos = e.GetPosition(DisplayArea);
        }

        #endregion

        Thread CameraThread = null;
        bool isThreadEnd = false;

        private void CreateCameraThread()
        {
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
                        });
                        Thread.Sleep(40);
                        long tik2 = DateTime.Now.Ticks;
                        //计算帧率
                        Dispatcher.Invoke(() =>
                        {
                            Framerate.Content = "帧率:" + Math.Round(1000.0 / ((tik2 - tik1) / 10000.0), 3).ToString();
                        });
                    }
                    catch (Exception e) { }
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
                double x = Camera.DisplayWindow.Left;
                double y = Camera.DisplayWindow.Top;
                double width = Camera.DisplayWindow.ActualWidth;
                double height = Camera.DisplayWindow.ActualHeight;
                WindowState state = Camera.DisplayWindow.WindowState;
                string path = Camera.CloseDeviceInfoAndSaveParams(out bool result);
                if (result == false) { return; }
                Camera = (CameraInfo)CameraInfo.OpenAndLoadParams(path, Camera.Device.GetType());
                Camera.DisplayWindow = new CameraWindow(Camera);
                Camera.DisplayWindow.Left = x;
                Camera.DisplayWindow.Top = y;
                Camera.DisplayWindow.Width = width;
                Camera.DisplayWindow.Height = height;
                Camera.DisplayWindow.WindowState = state;
                Camera.DisplayWindow.Show();
            }
            catch (Exception exc) { }
        }

        private void SetParams(object sender, RoutedEventArgs e)
        {
            ParameterWindow window = new ParameterWindow(Camera.Device.AvailableParameterNames());
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }


        #region 相机区域调整


        int CameraPixelHeight;

        int CameraPixelWidth;

        Rect ClipImageBound = Rect.Empty;

        bool IsResize = false;

        bool IsResizeBegin = false;

        Point beginPoint = new Point();

        private void EnableResize(object sender, RoutedEventArgs e)
        {
            IsResize = true;
            e.Handled = true;
        }

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
                IsResizeBegin = true;
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
                IsResizeBegin = false;
                DisplayArea.ReleaseMouseCapture();
                //刷新边框
                Point p1 = DrawingPanel.TranslatePoint(beginPoint, DisplayArea);
                Point p2 = DrawingPanel.TranslatePoint(e.GetPosition(DrawingPanel), DisplayArea);
                GetRatioPoint(DisplayArea.DisplayArea, p1, out Point rp1);
                GetRatioPoint(DisplayArea.DisplayArea, p2, out Point rp2);
                ClipImageBound = new Rect(rp1, rp2);
                IsResize = false;
                ResizeBound.Visibility = Visibility.Hidden;
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
            origin.Freeze();
            if (!clipRange.IsEmpty)
            {
                int x1 = (int)(clipRange.Left * origin.PixelWidth);
                int x2 = (int)(clipRange.Right * origin.PixelWidth);
                int y1 = (int)(clipRange.Top * origin.PixelHeight);
                int y2 = (int)(clipRange.Bottom * origin.PixelHeight);
                Int32Rect cutrect = new Int32Rect(x1, y1, x2 - x1, y2 - y1);
                CroppedBitmap cb = new CroppedBitmap(origin, cutrect);
                cb.Freeze();
                return cb;
            }
            return origin;
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
            IsResize = false;
            ResizeBound.Visibility = Visibility.Hidden;
        }

        #endregion
    }
}
