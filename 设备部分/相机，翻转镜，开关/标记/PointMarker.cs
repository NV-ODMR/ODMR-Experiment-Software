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
    public class PointMarker : MarkerBase
    {
        public override double MarkerStrokeThickness
        {
            get
            {
                return ((MarkerShape as Ellipse).Width + (MarkerShape as Ellipse).Height) / 2;
            }
            set
            {
                (MarkerShape as Ellipse).Width = value;
                (MarkerShape as Ellipse).Height = value;
            }
        }

        public override SolidColorBrush MarkerColor
        {
            get { return MarkerShape.Fill as SolidColorBrush; }
            set { MarkerShape.Fill = value; }
        }

        public override Point Position
        {
            get
            {
                return new Point(Canvas.GetLeft(MarkerShape), Canvas.GetTop(MarkerShape));
            }
            set
            {
                Canvas.SetLeft(MarkerShape, value.X - MarkerShape.Width / 2);
                Canvas.SetTop(MarkerShape, value.Y - MarkerShape.Height / 2);
            }
        }


        public override Shape MarkerShape { get; set; } = new Ellipse() { StrokeThickness = 0, Width = 2, Height = 2, Fill = Brushes.Red };

        public override void CalculateNewShape(Point newposition)
        {

            InitialPoint = newposition;
            Position = GetTransformPoint(InitialPoint);
        }

        public override void ValidateShape()
        {
            Position = GetTransformPoint(InitialPoint);
        }
    }
}
