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
    public class CenterCircleMarker : MarkerBase
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
            Position = new Point(ip.X - distance, ip.Y - distance);
            MarkerSize = new Size(2 * distance, 2 * distance);
        }
    }
}
