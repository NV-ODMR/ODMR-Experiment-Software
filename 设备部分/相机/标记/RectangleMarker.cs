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
    public class RectangleMarker : MarkerBase
    {
        public override Shape MarkerShape { get; set; } = new Rectangle() { Stroke = Brushes.Red, StrokeThickness = 4 };

        public override void ValidateShape()
        {
            if (IsNanPoint(InitialPoint) || IsNanPoint(FinalPoint)) return;
            Point fp = GetTransformPoint(FinalPoint);
            Point ip = GetTransformPoint(InitialPoint);
            Position = new Point(Math.Min(fp.X, ip.X), Math.Min(fp.Y, ip.Y));
            MarkerSize = new Size(Math.Abs(fp.X - ip.X), Math.Abs(fp.Y - ip.Y));
        }
    }
}
