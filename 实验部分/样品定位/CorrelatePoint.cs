using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.样品定位
{
    public class CorrelatePoint
    {
        public Point SourcePixelLoc { get; set; }

        public Point TargetPixelLoc { get; set; }

        public Point TargetMoverLoc { get; set; } = new Point(double.NaN, double.NaN);

        public CorrelatePoint(Point sourcepixel, Point targetpixel)
        {
            SourcePixelLoc = sourcepixel;
            TargetPixelLoc = targetpixel;
        }
    }

    public class CorrelatePointCollection
    {
        public List<CorrelatePoint> CorrelatePoints = new List<CorrelatePoint>();

        public double SPtoP { get; private set; } = 1;

        public double BetaPtoP { get; private set; } = 0;

        public double TxPtoP { get; private set; } = 0;

        public double TyPtoP { get; private set; } = 0;

        public double SPtoM { get; private set; } = 1;

        public double BetaPtoM { get; private set; } = 0;

        public double TxPtoM { get; private set; } = 0;

        public double TyPtoM { get; private set; } = 0;

        /// <summary>
        /// 是否反转X
        /// </summary>
        public bool ReverseX { get; set; } = false;

        /// <summary>
        /// 是否反转Y
        /// </summary>
        public bool ReverseY { get; set; } = false;

        /// <summary>
        /// 计算变换参数
        /// </summary>
        public void CalculateTransformParams()
        {
            if (CorrelatePoints.Count < 2) throw new Exception("用于计算的点数不能小于2 ");
            List<Point> sources = new List<Point>();
            List<Point> targets = new List<Point>();
            List<Point> targetps = new List<Point>();
            foreach (var item in CorrelatePoints)
            {
                if (double.IsNaN(item.TargetMoverLoc.X) || double.IsNaN(item.TargetMoverLoc.Y))
                {
                    throw new Exception("目标点为NAN，无法计算");
                }
                sources.Add(item.SourcePixelLoc);
                targets.Add(item.TargetMoverLoc);
                targetps.Add(item.TargetPixelLoc);
            }
            FitTransform(sources, targets, out double a, out double b, out double tx, out double ty);
            SPtoM = Math.Sqrt(a * a + b * b);
            BetaPtoM = Math.Atan2(a, b);
            TxPtoM = tx;
            TyPtoM = ty;


            bool rex = ReverseX;
            bool rey = ReverseY;

            ReverseX = false;
            ReverseY = false;
            FitTransform(sources, targetps, out a, out b, out tx, out ty);
            SPtoP = Math.Sqrt(a * a + b * b);
            BetaPtoP = Math.Atan2(a, b);
            TxPtoP = tx;
            TyPtoP = ty;

            ReverseX = rex;
            ReverseY = rey;
        }

        /// <summary>
        /// 使用对应点进行拟合
        /// </summary>
        /// <param name="SourcePoint"></param>
        /// <param name="TargetPoint"></param>
        /// <param name="a">a=cos(beta)*S,S为图片缩放率,beta为旋转角度</param>
        /// <param name="b">b=sin(beta)*S,S为图片缩放率,beta为旋转角度</param>
        /// <param name="tx">X方向平移</param>
        /// <param name="ty">Y方向平移</param>
        /// <returns></returns>
        private bool FitTransform(List<Point> SourcePoint, List<Point> TargetPoint, out double a, out double b, out double tx, out double ty)
        {
            if (SourcePoint.Count < 2 || SourcePoint.Count != TargetPoint.Count)
            {
                a = 0;
                b = 0;
                tx = 0;
                ty = 0;
                return false;
            }
            //获取系数矩阵
            DenseMatrix matq = new DenseMatrix(SourcePoint.Count * 2, 4);
            int index = 0;

            int reverseXNum = ReverseX ? -1 : 1;
            int reverseYNum = ReverseY ? -1 : 1;

            for (int i = 0; i < matq.RowCount; i += 2)
            {
                matq[i, 0] = reverseXNum * SourcePoint[index].X;
                matq[i, 1] = -reverseYNum * SourcePoint[index].Y;
                matq[i, 2] = 1;
                matq[i, 3] = 0;
                matq[i + 1, 0] = reverseYNum * SourcePoint[index].Y;
                matq[i + 1, 1] = reverseXNum * SourcePoint[index].X;
                matq[i + 1, 2] = 0;
                matq[i + 1, 3] = 1;
                ++index;
            }
            //获取常数项矩阵
            index = 0;
            DenseMatrix matb = new DenseMatrix(SourcePoint.Count * 2, 1);
            for (int i = 0; i < matb.RowCount; i += 2)
            {
                matb[i, 0] = TargetPoint[index].X;
                matb[i + 1, 0] = TargetPoint[index].Y;
                ++index;
            }
            var A = matq.Transpose() * matq;

            var B = matq.Transpose() * matb;

            var result = A.Inverse() * B;

            a = result[0, 0];
            b = result[1, 0];
            tx = result[2, 0];
            ty = result[3, 0];

            return true;
        }

        public Point TransformPointToMover(Point sourcePoint)
        {
            return TransformPoint(sourcePoint, SPtoM, BetaPtoM, TxPtoM, TyPtoM);
        }

        public Point TransformPointToPixel(Point sourcePoint)
        {
            bool rex = ReverseX;
            bool rey = ReverseY;

            ReverseX = false;
            ReverseY = false;

            Point p = TransformPoint(sourcePoint, SPtoP, BetaPtoP, TxPtoP, TyPtoP);

            ReverseX = rex;
            ReverseY = rey;

            return p;
        }

        private Point TransformPoint(Point p, double s, double beta, double tx, double ty)
        {
            int reverseXNum = ReverseX ? -1 : 1;
            int reverseYNum = ReverseY ? -1 : 1;
            Point np = new Point(s * Math.Sin(beta) * reverseXNum * p.X - s * Math.Cos(beta) * reverseYNum * p.Y + tx, Math.Cos(beta) * s * reverseXNum * p.X + s * Math.Sin(beta) * p.Y * reverseYNum + ty);
            return np;
        }
    }
}
