using CodeHelper;
using CodeHelper.ResizeElements;
using Controls;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// LabeledImage.xaml 的交互逻辑
    /// </summary>
    public partial class LabeledImage : Grid
    {
        /// <summary>
        /// 是否允许平移和缩放变换
        /// </summary>
        public bool Resizeable { get; set; } = false;

        /// <summary>
        /// 是否只允许编辑选中的点(包括移动等操作)
        /// </summary>
        public bool OnlySelectionMove { get; set; } = false;

        public event Action<ImageLabel, int> PointLocChanged = null;

        ViewingHelper helper = null;

        public LabeledImage()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resizeable == true)
            {
                helper = new ViewingHelper(DisplayArea, this);
                helper.AfterTransformUpdated += UpdateCursorLocations;
            }
        }

        public void InitialViewing()
        {
            helper?.InitControlTransform();
        }

        public void SetSource(ImageSource source)
        {
            DisplayArea.Source = source;
        }

        #region 光标设置

        List<ImageLabel> DisplayCursors = new List<ImageLabel>();

        public ImageLabel AddCursor(Point mousepos)
        {
            ImageLabel l = new ImageLabel();

            Point newpo = TranslatePoint(mousepos, DisplayArea);
            DisplayCursors.Add(l);
            l.Geometry.Cursor = Cursors.Hand;
            l.PixelPoint = new Point(newpo.X, newpo.Y);
            l.RatioPoint = new Point(newpo.X / DisplayArea.ActualWidth, newpo.Y / DisplayArea.ActualHeight);
            l.Geometry.MouseLeftButtonDown += CursorDragBegin;
            l.Geometry.MouseMove += CursorDragMove;
            l.Geometry.MouseLeftButtonUp += CursorDragEnd;
            l.AddToControl(DrawingPanel);
            UpdatePointLocation(l);
            return l;
        }

        public void SelectCursor(int ind)
        {
            foreach (var item in DisplayCursors)
            {
                item.IsSelected = false;
            }
            DisplayCursors[ind].IsSelected = true;
        }

        public void SelectCursor(ImageLabel ind)
        {
            foreach (var item in DisplayCursors)
            {
                item.IsSelected = false;
            }
            ind.IsSelected = true;
        }

        public ImageLabel GetSelectedCursor()
        {
            foreach (var item in DisplayCursors)
            {
                if (item.IsSelected)
                {
                    return item;
                }
            }
            return null;
        }

        private void CursorDragEnd(object sender, MouseButtonEventArgs e)
        {
            ImageLabel imlabel = (sender as Ellipse).Tag as ImageLabel;
            if (imlabel.IsSelected == false && OnlySelectionMove) return;
            imlabel.Geometry.ReleaseMouseCapture();
        }

        private void CursorDragMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ImageLabel imlabel = (sender as Ellipse).Tag as ImageLabel;
            if (imlabel.IsSelected == false && OnlySelectionMove) return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(DisplayArea);
                if (p.X < 0 || p.X > DisplayArea.ActualWidth) return;
                if (p.Y < 0 || p.Y > DisplayArea.ActualHeight) return;
                ImageLabel ellipse = (sender as Ellipse).Tag as ImageLabel;
                ellipse.PixelPoint = new Point(p.X, p.Y);
                ellipse.RatioPoint = new Point(p.X / DisplayArea.ActualWidth, p.Y / DisplayArea.ActualHeight);
                UpdatePointLocation(ellipse);
            }
        }

        private void CursorDragBegin(object sender, MouseButtonEventArgs e)
        {
            ImageLabel imlabel = (sender as Ellipse).Tag as ImageLabel;
            if (imlabel.IsSelected == false && OnlySelectionMove) return;
            imlabel.Geometry.CaptureMouse();
        }

        private void UpdateCursorLocations(object sender, SizeChangedEventArgs e)
        {
            foreach (var item in DisplayCursors)
            {
                UpdatePointLocation(item);
            }
        }

        private void UpdateCursorLocations(object sender, RoutedEventArgs e)
        {
            foreach (var item in DisplayCursors)
            {
                UpdatePointLocation(item);
            }
        }

        public void UpdatePointLocation(ImageLabel point)
        {
            Point p = point.RatioPoint;
            double x = p.X * DisplayArea.ActualWidth;
            double y = p.Y * DisplayArea.ActualHeight;
            Point tpoint = new Point();
            if (helper == null)
            {
                tpoint = new Point(x, y);
            }
            else
            {
                tpoint = helper.GetTramsform().Transform(new Point(x, y));
            }
            point.SetLoc(tpoint.X, tpoint.Y);
            PointLocChanged?.Invoke(point, DisplayCursors.IndexOf(point));
        }

        public void RemoveCursour(int ind)
        {
            ImageLabel po = DisplayCursors[ind];
            po.RemoveFromControl(DrawingPanel);
            DisplayCursors.Remove(po);
        }

        public void RemoveCursour(ImageLabel po)
        {
            po.RemoveFromControl(DrawingPanel);
            DisplayCursors.Remove(po);
        }

        #endregion
    }
}
