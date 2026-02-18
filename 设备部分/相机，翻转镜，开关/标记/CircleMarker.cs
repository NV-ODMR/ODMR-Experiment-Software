using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ODMR_Lab.设备部分.相机_翻转镜_开关
{
    public class CircleMarker : MarkerBase
    {
        public override Shape MarkerShape { get; set; } = new Ellipse() { Stroke = Brushes.Red, StrokeThickness = 4 };

        public override void ValidateShape()
        {
            if (IsNanPoint(InitialPoint) || IsNanPoint(FinalPoint)) return;
            Point fp = GetTransformPoint(FinalPoint);
            Point ip = GetTransformPoint(InitialPoint);
            var det = (fp - ip);
            double distance = det.Length;
            double xratio = det.X / distance;
            double yratio = det.Y / distance;
            Point center = new Point((ip.X + fp.X) / 2, (ip.Y + fp.Y) / 2);
            Position = new Point(center.X - distance / 2, center.Y - distance / 2);
            MarkerSize = new Size(distance, distance);
        }
    }
}
