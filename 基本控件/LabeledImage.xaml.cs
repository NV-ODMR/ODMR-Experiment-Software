using CodeHelper;
using CodeHelper.ResizeElements;
using Controls;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.设备部分.相机_翻转镜_开关;
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
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

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
                helper.AfterTransformUpdated += (s, ev) =>
                {
                    DrawingPanel.Margin = DisplayArea.Margin;
                    foreach (var item in DisplayMarkers)
                    {
                        item.ValidateShape();
                    }
                };
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

        List<MarkerBase> DisplayMarkers = new List<MarkerBase>();

        public void CreateMarkerAndEdit(MarkerBase marker)
        {
            marker.RegisterToCanvas(DrawingPanel);
            marker.MarkerDoubleClick += ShowMarkerSetWindow;
            marker.DeleteEvent += (s, e) =>
            {
                DisplayMarkers.Remove(marker);
            };
            marker.BeginEdit();
            DisplayMarkers.Add(marker);
        }

        MarkerSetWindow window = new MarkerSetWindow();
        private void ShowMarkerSetWindow(object sender, RoutedEventArgs e)
        {
            BitmapSource source = null;
            try
            {
                source = DisplayArea.Source as WriteableBitmap;
            }
            catch (Exception)
            {
            }
            window.WindowState = WindowState.Normal;
            window.Activate();
            window.Show(sender as MarkerBase, source);
        }
        #endregion

        #region 标记文件保存
        public FileObject GenerateMarkersFile(FileObject obj)
        {
            var points = DisplayMarkers.Where((x) => x is PointMarker);
            var circles = DisplayMarkers.Where((x) => x is CircleMarker);
            var centercircles = DisplayMarkers.Where((x) => x is CenterCircleMarker);
            var rectangles = DisplayMarkers.Where((x) => x is RectangleMarker);
            var centerrectangles = DisplayMarkers.Where((x) => x is CenterRectangleMarker);
            if (points.Count() != 0)
                obj.WriteStringData("PointMarker", points.Select((x) => FileHelper.Combine("@", x.GetRatioPosition().ToString(), x.InitialPoint.ToString(), x.FinalPoint.ToString(), x.GetRatioSize().ToString(), x.MarkerColor.Color.ToString(), x.MarkerStrokeThickness.ToString())).ToList());
            if (circles.Count() != 0)
                obj.WriteStringData("CircleMarker", circles.Select((x) => FileHelper.Combine("@", x.GetRatioPosition().ToString(), x.InitialPoint.ToString(), x.FinalPoint.ToString(), x.GetRatioSize().ToString(), x.MarkerColor.Color.ToString(), x.MarkerStrokeThickness.ToString())).ToList());
            if (rectangles.Count() != 0)
                obj.WriteStringData("RectangleMarker", rectangles.Select((x) => FileHelper.Combine("@", x.GetRatioPosition().ToString(), x.InitialPoint.ToString(), x.FinalPoint.ToString(), x.GetRatioSize().ToString(), x.MarkerColor.Color.ToString(), x.MarkerStrokeThickness.ToString())).ToList());
            if (centercircles.Count() != 0)
                obj.WriteStringData("CenterCircleMarker", centercircles.Select((x) => FileHelper.Combine("@", x.GetRatioPosition().ToString(), x.InitialPoint.ToString(), x.FinalPoint.ToString(), x.GetRatioSize().ToString(), x.MarkerColor.Color.ToString(), x.MarkerStrokeThickness.ToString())).ToList());
            if (centerrectangles.Count() != 0)
                obj.WriteStringData("CenterRectangleMarker", centerrectangles.Select((x) => FileHelper.Combine("@", x.GetRatioPosition().ToString(), x.InitialPoint.ToString(), x.FinalPoint.ToString(), x.GetRatioSize().ToString(), x.MarkerColor.Color.ToString(), x.MarkerStrokeThickness.ToString())).ToList());
            return obj;
        }

        public void ReadFromMarkersFile(FileObject obj)
        {
            var names = obj.GetDataNames();
            if (names.Contains("PointMarker"))
            {
                var strs = obj.ExtractString("PointMarker");
                foreach (var item in strs)
                {
                    PointMarker marker = new PointMarker();
                    var ss = item.Split('@').ToList();
                    marker.RegisterToCanvas(DrawingPanel);
                    DisplayMarkers.Add(marker);
                    marker.Position = marker.GetTransformPoint(Point.Parse(ss[0]));
                    marker.InitialPoint = Point.Parse(ss[1]);
                    marker.FinalPoint = Point.Parse(ss[2]);
                    marker.MarkerSize = marker.GetTransformSize(Size.Parse(ss[3]));
                    marker.MarkerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ss[4]));
                    marker.MarkerStrokeThickness = double.Parse(ss[5]);
                }
            }
            if (names.Contains("CircleMarker"))
            {
                var strs = obj.ExtractString("CircleMarker");
                foreach (var item in strs)
                {
                    CircleMarker marker = new CircleMarker();
                    var ss = item.Split('@').ToList();
                    marker.RegisterToCanvas(DrawingPanel);
                    DisplayMarkers.Add(marker);
                    marker.Position = marker.GetTransformPoint(Point.Parse(ss[0]));
                    marker.InitialPoint = Point.Parse(ss[1]);
                    marker.FinalPoint = Point.Parse(ss[2]);
                    marker.MarkerSize = marker.GetTransformSize(Size.Parse(ss[3]));
                    marker.MarkerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ss[4]));
                    marker.MarkerStrokeThickness = double.Parse(ss[5]);
                }
            }
            if (names.Contains("RectangleMarker"))
            {
                var strs = obj.ExtractString("RectangleMarker");
                foreach (var item in strs)
                {
                    RectangleMarker marker = new RectangleMarker();
                    var ss = item.Split('@').ToList();
                    marker.RegisterToCanvas(DrawingPanel);
                    DisplayMarkers.Add(marker);
                    marker.Position = marker.GetTransformPoint(Point.Parse(ss[0]));
                    marker.InitialPoint = Point.Parse(ss[1]);
                    marker.FinalPoint = Point.Parse(ss[2]);
                    marker.MarkerSize = marker.GetTransformSize(Size.Parse(ss[3]));
                    marker.MarkerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ss[4]));
                    marker.MarkerStrokeThickness = double.Parse(ss[5]);
                }
            }
            if (names.Contains("CenterCircleMarker"))
            {
                var strs = obj.ExtractString("CenterCircleMarker");
                foreach (var item in strs)
                {
                    CenterCircleMarker marker = new CenterCircleMarker();
                    var ss = item.Split('@').ToList();
                    marker.RegisterToCanvas(DrawingPanel);
                    DisplayMarkers.Add(marker);
                    marker.Position = marker.GetTransformPoint(Point.Parse(ss[0]));
                    marker.InitialPoint = Point.Parse(ss[1]);
                    marker.FinalPoint = Point.Parse(ss[2]);
                    marker.MarkerSize = marker.GetTransformSize(Size.Parse(ss[3]));
                    marker.MarkerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ss[4]));
                    marker.MarkerStrokeThickness = double.Parse(ss[5]);
                }
            }
            if (names.Contains("CenterRectangleMarker"))
            {
                var strs = obj.ExtractString("CenterRectangleMarker");
                foreach (var item in strs)
                {
                    CenterRectangleMarker marker = new CenterRectangleMarker();
                    var ss = item.Split('@').ToList();
                    marker.RegisterToCanvas(DrawingPanel);
                    DisplayMarkers.Add(marker);
                    marker.Position = marker.GetTransformPoint(Point.Parse(ss[0]));
                    marker.InitialPoint = Point.Parse(ss[1]);
                    marker.FinalPoint = Point.Parse(ss[2]);
                    marker.MarkerSize = marker.GetTransformSize(Size.Parse(ss[3]));
                    marker.MarkerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ss[4]));
                    marker.MarkerStrokeThickness = double.Parse(ss[5]);
                }
            }
        }
        #endregion
    }
}
