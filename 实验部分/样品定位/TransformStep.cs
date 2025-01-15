using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CodeHelper;
using OpenCvSharp.Extensions;
using OpenCvSharp;

namespace ODMR_Lab.实验部分.样品定位
{
    public enum TransformStep
    {
        /// <summary>
        /// 不变换
        /// </summary>
        None = -1,
        /// <summary>
        /// 水平镜像
        /// </summary>
        HorizontalMirror = 0,
        /// <summary>
        /// 垂直镜像
        /// </summary>
        VerticalMirror = 1,
        /// <summary>
        /// 顺时针旋转
        /// </summary>
        ClockWiseRotate = 2,
        /// <summary>
        /// 逆时针旋转
        /// </summary>
        AntiClockwiseRotate = 3,
    }

    /// <summary>
    /// 变换组
    /// </summary>
    public class TransformGroup
    {
        public List<TransformStep> TramsformSteps = new List<TransformStep>();

        /// <summary>
        /// 变换
        /// </summary>
        /// <returns></returns>
        public BitmapImage ApplyTransform(BitmapImage image)
        {
            Mat mat = BitmapConverter.ToMat(ImageConverter.BitmapImageToBitmap(image));
            ///开始变换
            foreach (var item in TramsformSteps)
            {
                if (item == TransformStep.VerticalMirror)
                {
                    mat = VMirror(mat);
                }
                if (item == TransformStep.HorizontalMirror)
                {
                    mat = HMirror(mat);
                }
                if (item == TransformStep.ClockWiseRotate)
                {
                    mat = CRotate(mat);
                }
                if (item == TransformStep.AntiClockwiseRotate)
                {
                    mat = ACRotate(mat);
                }
            }
            return ImageConverter.BitmapToBitmapImage(BitmapConverter.ToBitmap(mat));
        }

        private Mat HMirror(Mat bmap)
        {
            return bmap.Flip(FlipMode.Y);
        }
        private Mat VMirror(Mat bmap)
        {
            return bmap.Flip(FlipMode.X);
        }
        private Mat CRotate(Mat bmap)
        {
            Cv2.Rotate(bmap, bmap, RotateFlags.Rotate90Clockwise);
            return bmap;
        }
        private Mat ACRotate(Mat bmap)
        {
            Cv2.Rotate(bmap, bmap, RotateFlags.Rotate90Counterclockwise);
            return bmap;
        }
    }
}
