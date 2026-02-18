using Controls.Windows;
using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ODMR_Lab
{
    public class ImageHelper
    {
        /// <summary>
        /// 保存为图片文件
        /// </summary>
        /// <param name="bitmapSource"></param>
        /// <param name="filePath"></param>
        public static void SaveAsPng(BitmapSource bitmapSource, string filePath)
        {
            // 创建PngBitmapEncoder对象，用于编码为PNG格式
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            // 将BitmapSource对象添加到编码器中
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            // 使用FileStream打开指定路径的文件，用于写入图像数据
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                // 将编码后的图像数据写入文件流
                encoder.Save(stream);
            }
        }

        /// <summary>
        /// 导入图片
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BitmapImage LoadImage(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                // 这里可以对 bitmapImage 进行操作

                stream.Close();
                return bitmapImage;
            }
        }

        /// <summary>
        /// 从资源中导入文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BitmapImage LoadImageFromResource(string path)
        {
            Uri uri = new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/{path}", UriKind.RelativeOrAbsolute);
            return new BitmapImage(uri);
        }
    }
}
