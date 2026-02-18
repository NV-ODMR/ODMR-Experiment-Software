using CodeHelper;
using Controls;
using ODMR_Lab.基本控件;
using ODMR_Lab.设备部分.相机_翻转镜;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.设备部分.相机_翻转镜_开关
{
    public abstract class MarkerBase
    {
        public static DecoratedButton TemplateButton = new DecoratedButton()
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")),
            MoveInColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF424242")),
            PressedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF39393A")),
            Foreground = Brushes.White,
            MoveInForeground = Brushes.White,
            PressedForeground = Brushes.White,
        };

        public event RoutedEventHandler DeleteEvent = null;

        public event RoutedEventHandler MarkerDoubleClick = null;

        public event RoutedEventHandler MarkerConfirmedEvent = null;

        /// <summary>
        /// 标记粗细
        /// </summary>
        public virtual double MarkerStrokeThickness
        {
            get
            {
                return MarkerShape.StrokeThickness;
            }
            set
            {
                MarkerShape.StrokeThickness = value;
            }
        }

        /// <summary>
        /// 标记颜色
        /// </summary>
        public virtual SolidColorBrush MarkerColor
        {
            get
            {
                return MarkerShape.Stroke as SolidColorBrush;
            }
            set
            {
                MarkerShape.Stroke = value;
            }
        }

        public virtual Point Position
        {
            get
            {
                return new Point(Canvas.GetLeft(MarkerShape), Canvas.GetTop(MarkerShape));
            }
            set
            {
                Canvas.SetLeft(MarkerShape, value.X);
                Canvas.SetTop(MarkerShape, value.Y);
            }
        }

        /// <summary>
        /// 标记尺寸（绝对值）
        /// </summary>
        public virtual Size MarkerSize
        {
            get
            {
                return new Size(MarkerShape.Width, MarkerShape.Height);
            }
            set
            {
                MarkerShape.Width = value.Width;
                MarkerShape.Height = value.Height;
            }
        }

        /// <summary>
        /// 所属画布
        /// </summary>
        public Canvas ParentCanvas { get; set; } = null;

        public abstract Shape MarkerShape { get; set; }

        /// <summary>
        /// 起始选择点（相对坐标（0-1））
        /// </summary>
        public Point InitialPoint { get; set; } = new Point(double.NaN, double.NaN);

        /// <summary>
        /// 第二选择点（相对坐标（0-1））
        /// </summary>
        public Point FinalPoint { get; set; } = new Point(double.NaN, double.NaN);

        /// <summary>
        /// 计算新形状（输入参数为另一个点的坐标(0-1)）
        /// </summary>
        /// <param name="newposition"></param>
        public virtual void CalculateNewShape(Point newposition)
        {
            FinalPoint = newposition;
            ValidateShape();
        }

        /// <summary>
        /// 刷新形状
        /// </summary>
        public abstract void ValidateShape();

        public virtual void RegisterToCanvas(Canvas canvas)
        {
            ParentCanvas = canvas;
            if (!canvas.Children.Contains(MarkerShape))
                canvas.Children.Add(MarkerShape);
            canvas.SizeChanged -= UpdateShape;
            canvas.SizeChanged += UpdateShape;
            MarkerShape.MouseEnter += SelectMarkerDisplay;
            MarkerShape.MouseLeftButtonDown += MarkerBeginDrag;
            MarkerShape.MouseLeftButtonUp += MarkerEndDrag;
            MarkerShape.MouseMove += MarkerDrag;
            ControlEventHelper h = new ControlEventHelper(MarkerShape);
            h.MouseDoubleClick += MarkDoubleClickEvent;
            Controls.ContextMenu menu = new Controls.ContextMenu();
            DecoratedButton btn = new DecoratedButton();
            btn.Text = "删除";
            TemplateButton.CloneStyleTo(btn);
            menu.Items.Add(btn);
            btn.Click += (s, e) =>
            {
                if (!IsEdit)
                {
                    RemoveFromCanvas(ParentCanvas);
                    DeleteEvent?.Invoke(this, new RoutedEventArgs());
                }
            };
            menu.ApplyToControl(MarkerShape);
        }

        private void MarkDoubleClickEvent(object sender, MouseButtonEventArgs e)
        {
            if (!IsEdit)
                MarkerDoubleClick?.Invoke(this, e);
        }

        private Point InitialMovePoint;
        private void MarkerEndDrag(object sender, MouseButtonEventArgs e)
        {
            if (!IsEdit)
            {
                MarkerShape.ReleaseMouseCapture();
            }
        }

        private void MarkerDrag(object sender, MouseEventArgs e)
        {
            if (!IsEdit && e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(ParentCanvas);
                var det = GetRatioPoint(p) - GetRatioPoint(InitialMovePoint);
                InitialPoint += det;
                FinalPoint += det;
                ValidateShape();
                InitialMovePoint = p;
            }
        }

        private void MarkerBeginDrag(object sender, MouseButtonEventArgs e)
        {
            if (!IsEdit)
            {
                MarkerShape.CaptureMouse();
                InitialMovePoint = e.GetPosition(ParentCanvas);
            }
        }

        private void SelectMarkerDisplay(object sender, MouseEventArgs e)
        {
            if (!IsEdit) MarkerShape.Cursor = Cursors.Hand;
        }

        private void UpdateShape(object sender, SizeChangedEventArgs e)
        {
            ValidateShape();
        }

        public virtual void RemoveFromCanvas(Canvas canvas)
        {
            if (canvas.Children.Contains(MarkerShape))
                canvas.Children.Remove(MarkerShape);
            canvas.SizeChanged -= UpdateShape;
        }

        bool IsEdit = false;
        /// <summary>
        /// 开始编辑
        /// </summary>
        public void BeginEdit()
        {
            IsEdit = true;
            ParentCanvas.MouseLeftButtonDown += GetInitialPoint;
            ParentCanvas.MouseLeftButtonDown += CursorDragBegin;
            ParentCanvas.MouseMove += CursorDragMove;
            ParentCanvas.MouseLeftButtonUp += CursorDragEnd;
        }

        private void CursorDragBegin(object sender, MouseButtonEventArgs e)
        {
            MarkerShape.CaptureMouse();
        }

        private void CursorDragEnd(object sender, MouseButtonEventArgs e)
        {
            MarkerShape.ReleaseMouseCapture();
            ValidateShape();
            EndEdit();
            MarkerConfirmedEvent?.Invoke(this, e);
        }

        private void CursorDragMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsEdit) ParentCanvas.Cursor = Cursors.Pen;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(ParentCanvas);
                if (p.X < 0 || p.X > ParentCanvas.ActualWidth) return;
                if (p.Y < 0 || p.Y > ParentCanvas.ActualHeight) return;
                CalculateNewShape(GetRatioPoint(p));
            }
        }

        private void GetInitialPoint(object sender, MouseButtonEventArgs e)
        {
            InitialPoint = GetRatioPoint(e.GetPosition(ParentCanvas));
        }

        public void EndEdit()
        {
            IsEdit = false;
            ParentCanvas.MouseLeftButtonDown -= GetInitialPoint;
            ParentCanvas.MouseLeftButtonDown -= CursorDragBegin;
            ParentCanvas.MouseMove -= CursorDragMove;
            ParentCanvas.MouseLeftButtonUp -= CursorDragEnd;
            ParentCanvas.Cursor = Cursors.Arrow;
        }

        public Point GetTransformPoint(Point ratiopoint)
        {
            double x = ratiopoint.X * ParentCanvas.ActualWidth;
            double y = ratiopoint.Y * ParentCanvas.ActualHeight;
            return new Point(x, y);
        }

        public Size GetTransformSize(Size ratiosize)
        {
            return new Size(GetTransformWidth(ratiosize.Width), GetTransformHeight(ratiosize.Height));
        }

        protected Point GetRatioPoint(Point absolutepoint)
        {
            double x = absolutepoint.X / ParentCanvas.ActualWidth;
            double y = absolutepoint.Y / ParentCanvas.ActualHeight;
            return new Point(x, y);
        }

        protected double GetTransformWidth(double ratioWidth)
        {
            return ParentCanvas.ActualWidth * ratioWidth;
        }

        protected double GetTransformHeight(double ratioHeight)
        {
            return ParentCanvas.ActualHeight * ratioHeight;
        }

        protected double GetRatioWidth(double width)
        {
            return width / ParentCanvas.ActualWidth;
        }

        protected double GetRatioHeight(double height)
        {
            return height / ParentCanvas.ActualHeight;
        }

        protected bool IsNanPoint(Point p)
        {
            if (double.IsNaN(p.X) || double.IsNaN(p.Y))
                return true;
            return false;
        }

        public Point GetRatioPosition()
        {
            return GetRatioPoint(Position);
        }

        public Size GetRatioSize()
        {
            return new Size(GetRatioWidth(MarkerSize.Width), GetRatioHeight(MarkerSize.Height));
        }

        public Point GetPixelLocation(BitmapSource sourceimage)
        {
            if (sourceimage == null) return new Point(double.NaN, double.NaN);
            Point p = GetRatioPoint(Position);
            return new Point(p.X * sourceimage.PixelWidth, p.Y * sourceimage.PixelHeight);
        }

        public double GetPixelWidth(BitmapSource sourceimage)
        {
            if (sourceimage == null) return double.NaN;
            return GetRatioWidth(MarkerSize.Width) * sourceimage.PixelWidth;
        }

        public double GetPixelHeight(BitmapSource sourceimage)
        {
            if (sourceimage == null) return double.NaN;
            return GetRatioHeight(MarkerSize.Height) * sourceimage.PixelHeight;
        }
    }
}
