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

namespace ODMR_Lab.扩展部分.数据记录.界面及子窗口
{
    /// <summary>
    /// NoteControl.xaml 的交互逻辑
    /// </summary>
    public partial class ImageViewingControl : Grid
    {
        public ExtPage ParentPage = null;

        ViewingHelper ViewingControl = null;

        public NoteUnit ParentNoteUnit = null;

        List<KeyValuePair<BitmapImage, string>> ImageList = new List<KeyValuePair<BitmapImage, string>>();

        int DisplayedImagePairIndex = 0;

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

        public ImageViewingControl()
        {
            InitializeComponent();
            ViewingControl = new ViewingHelper(DisplayedImage, this);
        }

        public void LoadNoteUnit(NoteUnit unit)
        {
            ParentNoteUnit = unit;
            DisplayedImagePairIndex = 0;
            ReloadImages(null, new RoutedEventArgs());
        }

        private void ReloadImages(object sender, RoutedEventArgs e)
        {
            ImageList.Clear();
            if (Directory.Exists(ParentNoteUnit.GetNoteUnitFolderPath()))
            {
                var files = Directory.GetFiles(ParentNoteUnit.GetNoteUnitFolderPath());
                foreach (var item in files)
                {
                    string extention = Path.GetExtension(item);
                    if (extention == ".png" || extention == ".jpg")
                    {
                        BitmapImage img = NoteHelper.LoadImage(item);
                        img.Freeze();
                        ImageList.Add(new KeyValuePair<BitmapImage, string>(img, item));
                    }
                    else
                    {
                        if (extention == ".userdat")
                        {
                            if (ExperimentObject<ExpParamBase, ConfigBase>.GetExpType(item) != ExperimentFileTypes.None)
                            {
                            }
                            else
                            {
                                continue;
                            }
                        }
                        BitmapImage img = null;
                        ImageList.Add(new KeyValuePair<BitmapImage, string>(img, item));
                    }
                }
            }

            UpdateImagesDisplay();
        }

        private void UpdateImagesDisplay()
        {
            ImageIndex.Content = "第" + (DisplayedImagePairIndex + 1).ToString() + "/" + ImageList.Count.ToString() + "个";
            ApplyImage(DisplayedImagePairIndex);
        }

        private void Resize(object sender, RoutedEventArgs e)
        {
            ViewingControl.InitControlTransform();
        }

        public ExpDisplayWindow ParentExpDisplayWindow = null;

        private void ShowInNewWindow(object sender, RoutedEventArgs e)
        {
            if (!(DisplayedImagePairIndex < 0 || DisplayedImagePairIndex > ImageList.Count - 1))
            {
                if (Path.GetExtension(ImageList[DisplayedImagePairIndex].Value) == ".userdat")
                {
                    try
                    {
                        ParentExpDisplayWindow.Owner = null;
                        ParentExpDisplayWindow.WindowState = WindowState.Normal;
                        ParentExpDisplayWindow.Activate();
                        ParentExpDisplayWindow?.ShowWithExpFile(ImageList[DisplayedImagePairIndex].Value);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    Process.Start(ImageList[DisplayedImagePairIndex].Value);
                }
            }
        }

        private void Up(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyImage(DisplayedImagePairIndex - 1);
                if (ImageList.Count == 0)
                {
                    ImageIndex.Content = "第" + "0/0" + "个";
                    return;
                }
                ImageIndex.Content = "第" + (DisplayedImagePairIndex + 1).ToString() + "/" + ImageList.Count.ToString() + "个";
            }
            catch (Exception)
            {
            }
        }
        private void Down(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyImage(DisplayedImagePairIndex + 1);
                if (ImageList.Count == 0)
                {
                    ImageIndex.Content = "第" + "0/0" + "个";
                    return;
                }
                ImageIndex.Content = "第" + (DisplayedImagePairIndex + 1).ToString() + "/" + ImageList.Count.ToString() + "个";
            }
            catch (Exception)
            {
            }
        }

        private void ApplyImage(int index)
        {
            DisplayedImagePairIndex = index;
            if (DisplayedImagePairIndex > ImageList.Count - 1) DisplayedImagePairIndex = ImageList.Count - 1;
            if (DisplayedImagePairIndex < 0) DisplayedImagePairIndex = 0;
            DisplayedImage.Source = null;
            DisplayedImage.Visibility = Visibility.Hidden;
            FileImage.Visibility = Visibility.Hidden;
            try
            {
                if (ImageList[DisplayedImagePairIndex].Key != null)
                {
                    DisplayedImage.Visibility = Visibility.Visible;
                    FileImage.Visibility = Visibility.Hidden;
                    DisplayedImage.Source = ImageList[DisplayedImagePairIndex].Key;
                }
                else
                {
                    DisplayedImage.Visibility = Visibility.Hidden;
                    FileImage.Visibility = Visibility.Visible;
                    FileImage.SetDisplayFile(ImageList[DisplayedImagePairIndex].Value);
                    return;
                }
            }
            catch (Exception)
            {
            }
        }


        TakePhotoWindow window = null;
        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TakePhoto(object sender, RoutedEventArgs e)
        {
            if (ParentPage.PhotoCamera == null) return;
            if (!ParentPage.PhotoCamera.IsWriting)
            {
                window = new TakePhotoWindow(ParentPage.PhotoCamera);
                window.PhotoTakenCommand += new Action<BitmapSource>((image) =>
                {
                    try
                    {
                        string path = Path.Combine(ParentNoteUnit.GetNoteUnitFolderPath(), DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".png");
                        ///保存图片
                        NoteHelper.SaveAsPng(image, path);
                        BitmapImage img = NoteHelper.LoadImage(path);
                        img.Freeze();
                        ImageList.Add(new KeyValuePair<BitmapImage, string>(img, path));
                        UpdateImagesDisplay();
                    }
                    catch (Exception)
                    {
                    }
                });
                window.Show();
            }
            else
            {
                MessageWindow.ShowTipWindow("相机已被占用", Window.GetWindow(this));
            }
        }

        private void DeletePhoto(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DisplayedImagePairIndex < 0 || DisplayedImagePairIndex > ImageList.Count - 1)
                {
                    return;
                }
                if (MessageWindow.ShowMessageBox("删除", "确定要删除此图片吗？此操作不可恢复", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    File.Delete(ImageList[DisplayedImagePairIndex].Value);
                    ImageList.RemoveAt(DisplayedImagePairIndex);
                    UpdateImagesDisplay();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void LoadFiles(List<string> files)
        {
            int index = 0;
            foreach (var item in files)
            {
                try
                {
                    string path = Path.Combine(ParentNoteUnit.GetNoteUnitFolderPath(), DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + "-" + index.ToString());
                    string ext = Path.GetExtension(item);
                    if (ext == ".png" || ext == ".jpg")
                    {
                        try
                        {
                            File.Copy(item, path + ext);
                            BitmapImage img = NoteHelper.LoadImage(path + ext);
                            img.Freeze();
                            ImageList.Add(new KeyValuePair<BitmapImage, string>(img, path + ext));
                            DisplayedImagePairIndex = ImageList.Count - 1;
                            UpdateImagesDisplay();
                        }
                        catch (Exception)
                        {
                        }
                        continue;
                    }
                    else
                    {
                        try
                        {
                            path = Path.Combine(ParentNoteUnit.GetNoteUnitFolderPath(), Path.GetFileName(item));
                            File.Copy(item, path);
                            ImageList.Add(new KeyValuePair<BitmapImage, string>(null, path));
                            DisplayedImagePairIndex = ImageList.Count - 1;
                            UpdateImagesDisplay();
                        }
                        catch (Exception)
                        {
                        }
                        continue;
                    }
                }
                catch (Exception)
                {
                }
                ++index;
            }
        }

        /// <summary>
        /// 快捷键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {
                    //粘贴图片
                    var image = Clipboard.GetImage();
                    var file = Clipboard.GetFileDropList();
                    if (file.Count == 0 && image == null) return;
                    if (image != null)
                    {
                        string path = Path.Combine(ParentNoteUnit.GetNoteUnitFolderPath(), DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".png");
                        ///保存图片
                        NoteHelper.SaveAsPng(image, path);
                        BitmapImage img = NoteHelper.LoadImage(path);
                        img.Freeze();
                        ImageList.Add(new KeyValuePair<BitmapImage, string>(img, path));
                        DisplayedImagePairIndex = ImageList.Count - 1;
                        UpdateImagesDisplay();
                        return;
                    }
                    if (file.Count != 0)
                    {
                        List<string> filestrs = new List<string>();
                        foreach (var item in file)
                        {
                            filestrs.Add(item.ToString());
                        }
                        LoadFiles(filestrs);
                    }
                }
                catch (Exception)
                {

                }
            }
            //粘贴图片
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {
                    Clipboard.SetImage(ImageList[DisplayedImagePairIndex].Key);
                }
                catch (Exception)
                {
                }
            }
            if (e.Key == Key.Delete)
            {
                DeletePhoto(null, new RoutedEventArgs());
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
        }

        private void ImportFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    LoadFiles(openFileDialog.FileNames.ToList());
                }
                catch (Exception)
                {
                }
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameFile(object sender, RoutedEventArgs e)
        {
            ParamInputWindow window = new ParamInputWindow("重命名");
            window.Height = 150;
            window.Width = 500;
            var dic = new Dictionary<string, string>();
            dic.Add("文件名", Path.GetFileNameWithoutExtension(ImageList[DisplayedImagePairIndex].Value));
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = window.ShowDialog(dic);
            if (result.Count == 0) return;
            try
            {
                string path = Path.Combine(Path.GetDirectoryName(ImageList[DisplayedImagePairIndex].Value), result["文件名"] + Path.GetExtension(ImageList[DisplayedImagePairIndex].Value));
                if (File.Exists(path))
                {
                    throw new Exception("重命名未完成，存在同名文件");
                }
                File.Move(ImageList[DisplayedImagePairIndex].Value, path);
                ImageList[DisplayedImagePairIndex] = new KeyValuePair<BitmapImage, string>(ImageList[DisplayedImagePairIndex].Key, path);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow(ex.Message, Window.GetWindow(this));
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 文件拖动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                LoadFiles(files.ToList());
            }
        }
    }
}
