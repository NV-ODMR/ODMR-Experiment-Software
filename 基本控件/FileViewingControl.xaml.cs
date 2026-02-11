using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.Windows;
using ODMR_Lab.数据记录;
using ODMR_Lab.设备部分.电源;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using CodeHelper;
using System.IO;
using Path = System.IO.Path;
using System.Diagnostics;
using ODMR_Lab.设备部分.相机_翻转镜;
using System.Net.Cache;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.IO操作;
using System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.数据处理;
using ODMR_Lab.基本窗口;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class FileViewingControl : Grid
    {
        private static BitmapImage pdfImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "pdf.png"));
        private static BitmapImage PPTImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "ppt.png"));
        private static BitmapImage ExcelImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "excel.png"));
        private static BitmapImage TxtImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "txt.png"));
        private static BitmapImage WordImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "word.png"));
        private static BitmapImage NanImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "nanFile.png"));
        private static BitmapImage MathematicaImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "mathematica.png"));
        private static BitmapImage PythonImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "pythonfile.png"));
        private static BitmapImage ZipImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "zip.png"));
        private static BitmapImage expImage = NoteHelper.LoadImageFromResource(Path.Combine("图片资源", "experimentfile.png"));

        public FileViewingControl()
        {
            InitializeComponent();
        }

        public void SetDisplayFile(string filepath)
        {
            try
            {
                string ext = Path.GetExtension(filepath);
                if (ext == ".pdf")
                {
                    FileIcon.Source = pdfImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".ppt" || ext == ".pptx" || ext == ".ppsx" || ext == ".potx")
                {
                    FileIcon.Source = PPTImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".xls" || ext == ".xlsx" || ext == ".xlsm" || ext == ".xltx")
                {
                    FileIcon.Source = ExcelImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".doc" || ext == ".docx" || ext == ".dotx")
                {
                    FileIcon.Source = WordImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".txt")
                {
                    FileIcon.Source = TxtImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".m" || ext == ".wl" || ext == ".nb")
                {
                    FileIcon.Source = MathematicaImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".py")
                {
                    FileIcon.Source = PythonImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".zip" || ext == ".7z" || ext == ".rar")
                {
                    FileIcon.Source = ZipImage;
                    PDFName.Text = Path.GetFileName(filepath);
                    return;
                }
                if (ext == ".userdat")
                {
                    FileIcon.Source = expImage;
                    string filename = "";
                    var exptype = ExperimentObject<ExpParamBase, ConfigBase>.GetExpType(filepath);
                    filename += "(" + exptype.ToString() + ") ";
                    if (exptype == ExperimentFileTypes.ODMR实验)
                    {
                        SequenceFileExpObject.ReadODMRExpImformationFromFile(filepath, out string groupname, out string expname);
                        filename += "(" + groupname.ToString() + ") (" + expname + ") ";
                    }
                    PDFName.Text = filename + Path.GetFileName(filepath);
                    return;
                }

                FileIcon.Source = NanImage;
                PDFName.Text = Path.GetFileName(filepath);
                return;
            }
            catch (Exception)
            {
            }
        }
    }
}
