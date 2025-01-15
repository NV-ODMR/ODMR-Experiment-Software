using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ODMR_Lab.基本控件
{
    public class ImageLabel
    {
        public Ellipse Geometry { get; private set; } = new Ellipse()
        {
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            IsHitTestVisible = true,
            Width = 5,
            Height = 5,
            Cursor = Cursors.Hand,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Fill = Brushes.White,
        };

        public void SetLoc(double x, double y)
        {
            Canvas.SetLeft(Geometry, x - Geometry.ActualWidth / 2);
            Canvas.SetTop(Geometry, y - Geometry.ActualHeight / 2);
        }

        public ImageLabel()
        {
            Geometry.Tag = this;
        }

        public Point RatioPoint { get; set; }

        public Point PixelPoint { get; set; }

        private bool isSelected = false;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                if (value)
                {
                    Geometry.Fill = Brushes.Yellow;
                    Geometry.Stroke = Brushes.Red;
                }
                else
                {
                    Geometry.Fill = Brushes.Black;
                    Geometry.Stroke = Brushes.White;
                }
                isSelected = value;
            }
        }

        public void AddToControl(Panel c)
        {
            c.Children.Add(Geometry);
        }

        public void RemoveFromControl(Panel c)
        {
            c.Children.Remove(Geometry);
        }
    }
}
