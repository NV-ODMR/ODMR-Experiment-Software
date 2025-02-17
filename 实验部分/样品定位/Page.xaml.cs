using CodeHelper;
using Controls;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Xml.Linq;
using Path = System.IO.Path;
using MathNet.Numerics.Distributions;
using System.Diagnostics.Contracts;
using MathNet.Numerics;
using ODMR_Lab.Python.LbviewHandler;
using System.Windows.Forms;
using ContextMenu = Controls.ContextMenu;
using ODMR_Lab.实验部分.样品定位;
using Label = System.Windows.Controls.Label;
using ODMR_Lab.基本控件;
using Clipboard = System.Windows.Clipboard;
using Controls.Windows;
using Window = System.Windows.Window;

namespace ODMR_Lab.样品定位
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public override string PageName { get; set; } = "样品定位";
        public CorrelatePointCollection CorrelatePointCollection { get; set; } = new CorrelatePointCollection();

        public DisplayPage()
        {
            InitializeComponent();

        }

        private void InitCursorSettings()
        {
        }

        private void AddCursor(object sender, RoutedEventArgs e)
        {
        }

        public override void InnerInit()
        {
        }


        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }

        private void ResizeSourcePlot(object sender, RoutedEventArgs e)
        {
            SourceImage.InitialViewing();
        }

        private void OpenSourceImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.Cancel) return;
            try
            {
                BitmapImage image = GetImage(ofd.FileName);
                SourceImage.SetSource(image);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("打开图片出现错误:" + ex.Message, Window.GetWindow(this));
            }
        }

        private void ResizeTargetPlot(object sender, RoutedEventArgs e)
        {
            TargetImage.InitialViewing();
        }

        private void OpenTargetImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.Cancel) return;
            try
            {
                BitmapImage image = GetImage(ofd.FileName);
                TargetImage.SetSource(image);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("打开图片出现错误:" + ex.Message, Window.GetWindow(this));
            }
        }

        /// <summary>
        /// 获取图片并取消源文件引用
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static BitmapImage GetImage(string path)
        {
            if (!System.IO.File.Exists(path)) { return null; }
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(path)))
            {
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }


        private bool IsSourceSelectedEnd = true;
        private Point SourceSelectedPoint = new Point(double.NaN, double.NaN);

        private bool IsTargetSelectedEnd = true;
        private Point TargetSelectedPoint = new Point(double.NaN, double.NaN);

        /// <summary>
        /// 添加关联点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddCorrelatePoint(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                IsSourceSelectedEnd = false;
                SourceSelectedPoint = new Point(double.NaN, double.NaN);

                Dispatcher.Invoke(() =>
                {
                    PredictBtn.IsEnabled = false;
                    btnTemplate.IsEnabled = false;
                    //源图片选择关联点
                    SourceName.Text = "源图片(选择关联点,左键点击添加，拖动移动，右键点击确定)";
                    TargetName.Text = "目标图片";
                });

                while (IsSourceSelectedEnd == false)
                {
                    Thread.Sleep(50);
                }

                IsSourceSelectedEnd = true;
                if (double.IsNaN(SourceSelectedPoint.X))
                {
                    Dispatcher.Invoke(() =>
                    {
                        SourceName.Text = "源图片";
                        TargetName.Text = "目标图片";
                        PredictBtn.IsEnabled = true;
                        btnTemplate.IsEnabled = true;
                    });
                    return;
                }

                IsTargetSelectedEnd = false;
                TargetSelectedPoint = new Point(double.NaN, double.NaN);
                Dispatcher.Invoke(() =>
                {
                    //目标图片选择关联点
                    SourceName.Text = "源图片";
                    TargetName.Text = "目标图片(选择关联点,左键点击添加，拖动移动，右键点击确定)";
                });

                while (IsTargetSelectedEnd == false)
                {
                    Thread.Sleep(50);
                }

                IsTargetSelectedEnd = true;
                if (double.IsNaN(TargetSelectedPoint.X))
                {
                    Dispatcher.Invoke(() =>
                    {
                        SourceName.Text = "源图片";
                        TargetName.Text = "目标图片";
                        PredictBtn.IsEnabled = true;
                        btnTemplate.IsEnabled = true;
                    });
                    return;
                }

                //添加关联点
                CorrelatePointCollection.CorrelatePoints.Add(new CorrelatePoint(SourceSelectedPoint, TargetSelectedPoint));
                UpdateCorrelatePoints();

                Dispatcher.Invoke(() =>
                {
                    SourceName.Text = "源图片";
                    TargetName.Text = "目标图片";
                    PredictBtn.IsEnabled = true;
                    btnTemplate.IsEnabled = true;
                });
                return;

            });
            t.Start();
        }


        bool IsPredict = false;
        ImageLabel SourcePre = null;
        ImageLabel TargetPre = null;

        /// <summary>
        /// 位置预测
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PredictLoc(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                IsPredict = true;

                Dispatcher.Invoke(() =>
                {
                    SourcePre = SourceImage.AddCursor(new Point(SourceImage.ActualWidth / 2, SourceImage.ActualHeight / 2));
                    SourceImage.SelectCursor(SourcePre);
                    TargetPre = TargetImage.AddCursor(new Point(0, 0));
                    TargetImage.SelectCursor(TargetPre);

                    PredictBtn.IsEnabled = false;
                    btnTemplate.IsEnabled = false;
                    //源图片选择关联点
                    SourceName.Text = "源图片(选择需要预测的点,左键点击添加，拖动移动，右键退出)";
                    TargetName.Text = "目标图片";
                });

                while (IsPredict == true)
                {
                    Thread.Sleep(50);
                }

                Dispatcher.Invoke(() =>
                {
                    SourceName.Text = "源图片";
                    TargetName.Text = "目标图片";
                    PredictBtn.IsEnabled = true;
                    btnTemplate.IsEnabled = true;
                    IsPredict = false;
                    SourceImage.RemoveCursour(SourcePre);
                    TargetImage.RemoveCursour(TargetPre);
                });

            });
            t.Start();
        }

        /// <summary>
        /// /更新预测值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshPredectPoint(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsPredict)
            {
                try
                {
                    Point res = CorrelatePointCollection.TransformPointToPixel(SourcePre.PixelPoint);
                    TargetPre.RatioPoint = new Point(res.X / TargetImage.DisplayArea.ActualWidth, res.Y / TargetImage.DisplayArea.ActualHeight);
                    TargetPre.PixelPoint = new Point(res.X, res.Y);
                    TargetImage.UpdatePointLocation(TargetPre);
                    //更新位移台值
                    res = CorrelatePointCollection.TransformPointToMover(SourcePre.PixelPoint);
                    UpdatePredictParams(res);
                }
                catch (Exception ex) { return; }
            }
        }

        private void UpdatePredictParams(Point p)
        {
            XPre.Text = "位移台X预测值:" + Math.Round(p.X, 6).ToString();
            YPre.Text = "位移台Y预测值:" + Math.Round(p.Y, 6).ToString();
        }


        private void SourcePointLocChanged(ImageLabel obj, int ind)
        {
            UpdateCorrelatePoints();

            try
            {
                CorrelatePointCollection.CorrelatePoints[ind].SourcePixelLoc = obj.PixelPoint;
            }
            catch (Exception ex) { return; }
        }

        private void TargetPointLocChanged(ImageLabel obj, int ind)
        {
            UpdateCorrelatePoints();
            try
            {
                CorrelatePointCollection.CorrelatePoints[ind].TargetPixelLoc = obj.PixelPoint;
            }
            catch (Exception ex) { return; }
        }

        private void UpdateCorrelatePoints()
        {
            Dispatcher.Invoke(() =>
            {
                PointPanel.Children.Clear();

                foreach (var item in CorrelatePointCollection.CorrelatePoints)
                {
                    MouseColorHelper helper = new MouseColorHelper(Brushes.Transparent, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")), new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838")));
                    Grid g = new Grid() { Height = 40 };
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    helper.RegistateTarget(g);
                    g.MouseLeftButtonUp += SelectPointBar;
                    g.MouseRightButtonUp += SelectPointBar;
                    ContextMenu menu = new ContextMenu();

                    DecoratedButton btn = new DecoratedButton();
                    BtnTemplate.CloneStyleTo(btn);
                    btn.Text = "删除";
                    btn.Tag = g;
                    btn.Click += DeleteCorrelatePoint;
                    menu.Items.Add(btn);

                    btn = new DecoratedButton();
                    BtnTemplate.CloneStyleTo(btn);
                    btn.Text = "获取当前位移台值";
                    btn.Tag = g;
                    btn.Click += DeleteCorrelatePoint;
                    menu.Items.Add(btn);

                    menu.ApplyToControl(g);
                    g.Tag = helper;

                    Label l1 = new Label() { Content = item.SourcePixelLoc.X.ToString(), Foreground = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };
                    g.Children.Add(l1);
                    Grid.SetColumn(l1, 0);

                    Label l2 = new Label() { Content = item.SourcePixelLoc.Y.ToString(), Foreground = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };
                    g.Children.Add(l2);
                    Grid.SetColumn(l2, 1);

                    Label l3 = new Label() { Content = item.TargetPixelLoc.X.ToString(), Foreground = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };
                    g.Children.Add(l3);
                    Grid.SetColumn(l3, 2);

                    Label l4 = new Label() { Content = item.TargetPixelLoc.Y.ToString(), Foreground = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };
                    g.Children.Add(l4);
                    Grid.SetColumn(l4, 3);


                    FontChangeText tmoverx = new FontChangeText();
                    template.CloneStyleTo(tmoverx);
                    tmoverx.InnerTextBox.Text = item.TargetMoverLoc.X.ToString();
                    g.Children.Add(tmoverx);
                    Grid.SetColumn(tmoverx, 4);

                    FontChangeText tmovery = new FontChangeText();
                    template.CloneStyleTo(tmovery);
                    tmovery.InnerTextBox.Text = item.TargetMoverLoc.Y.ToString();
                    g.Children.Add(tmovery);
                    Grid.SetColumn(tmovery, 5);

                    PointPanel.Children.Add(g);
                }
            });
        }

        /// <summary>
        /// 删除关联点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void DeleteCorrelatePoint(object sender, RoutedEventArgs e)
        {
            try
            {
                int ind = PointPanel.Children.IndexOf((sender as DecoratedButton).Tag as Grid);
                CorrelatePointCollection.CorrelatePoints.RemoveAt(ind);
                SourceImage.RemoveCursour(ind);
                TargetImage.RemoveCursour(ind);
                UpdateCorrelatePoints();
            }
            catch (Exception exc) { return; }
        }

        private void SelectPointBar(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in PointPanel.Children)
            {
                ((item as Grid).Tag as MouseColorHelper).CancelEvent(item as Grid);
                if (item == sender)
                {
                    (item as Grid).Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF383838"));
                    //选定对应的点
                    try
                    {
                        int ind = PointPanel.Children.IndexOf(item as Grid);
                        SourceImage.SelectCursor(ind);
                        TargetImage.SelectCursor(ind);
                    }
                    catch (Exception ex) { return; }
                }
                else
                {
                    ((item as Grid).Tag as MouseColorHelper).RegistateTarget(item as Grid);
                }
            }
        }

        private void SourceImageSelected(object sender, MouseButtonEventArgs e)
        {
            IsSourceSelectedEnd = true;
            IsPredict = false;
            try
            {
                SourceSelectedPoint = SourceImage.GetSelectedCursor().PixelPoint;
            }
            catch (Exception ex) { return; }
        }

        private void TargetImageSelected(object sender, MouseButtonEventArgs e)
        {
            IsTargetSelectedEnd = true;
            try
            {
                TargetSelectedPoint = TargetImage.GetSelectedCursor().PixelPoint;
            }
            catch (Exception ex) { return; }
        }

        private void AddSourceCursor(object sender, MouseButtonEventArgs e)
        {
            SourceImage.Focus();
            if (IsSourceSelectedEnd == false && double.IsNaN(SourceSelectedPoint.X))
            {
                SourceImage.SelectCursor(SourceImage.AddCursor(e.GetPosition(SourceImage)));
                SourceSelectedPoint = new Point(0, 0);
            }
            if (IsPredict)
            {
                SourceSelectedPoint = e.GetPosition(SourceImage);
            }
        }

        private void AddTargetCursor(object sender, MouseButtonEventArgs e)
        {
            TargetImage.Focus();
            if (IsTargetSelectedEnd == false && double.IsNaN(TargetSelectedPoint.X))
            {
                TargetImage.SelectCursor(TargetImage.AddCursor(e.GetPosition(TargetImage)));
                TargetSelectedPoint = new Point(0, 0);
            }
            if (IsPredict)
            {
                TargetSelectedPoint = e.GetPosition(TargetImage);
            }
        }

        /// <summary>
        /// 计算关联参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calculate(object sender, RoutedEventArgs e)
        {
            try
            {
                //读取位移台数据
                foreach (var item in PointPanel.Children)
                {
                    double x = double.Parse(((item as Grid).Children[4] as FontChangeText).InnerTextBox.Text);
                    double y = double.Parse(((item as Grid).Children[5] as FontChangeText).InnerTextBox.Text);
                    int ind = PointPanel.Children.IndexOf(item as Grid);
                    CorrelatePointCollection.CorrelatePoints[ind].TargetMoverLoc = new Point(x, y);
                }
                CorrelatePointCollection.ReverseX = ReverseXChooser.IsSelected;
                CorrelatePointCollection.ReverseY = ReverseYChooser.IsSelected;
                CorrelatePointCollection.CalculateTransformParams();
                SetCorrelateParams(CorrelatePointCollection.TxPtoP, CorrelatePointCollection.TyPtoP, CorrelatePointCollection.SPtoP, CorrelatePointCollection.BetaPtoP);
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow(ex.Message, Window.GetWindow(this));
            }
        }

        private void SetCorrelateParams(double tx, double ty, double s, double beta)
        {
            OffsetX.Text = "偏移量X:" + Math.Round(tx, 6).ToString();
            OffsetY.Text = "偏移量Y:" + Math.Round(ty, 6).ToString();
            Zoom.Text = "缩放比例:" + Math.Round(s, 6).ToString();
            RotateAngle.Text = "旋转角:" + Math.Round(beta, 6).ToString();
        }

        private void ShowInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("样品定位说明:\n" + "1.一般使用方法：原图片为显微镜下样品照片，目标图片为其他视野下的图片，设置每组关联点后自行输入位移台示数。\n" +
                "2.位移台坐标轴规定：目标图片的方向应与源图片的朝向保持一致，XY坐标系可以自行规定，但是必须是右手系。自行规定的坐标轴正向必须是位移台示数增加的方向。位移台的方向规定可以在“设备”中的位移台界面进行设置。",
                Window.GetWindow(this));
        }

        /// <summary>
        /// 显示翻转信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowReverseInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("当X或Y方向位移台示数的增加方向和原图像的像素增加方向不一致时要勾选对应的反转选项。", Window.GetWindow(this));
        }

        #region 按键事件
        private void SourceImage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                try
                {
                    BitmapSource sour = Clipboard.GetImage();
                    if (sour == null) return;
                    SourceImage.SetSource(sour);
                }
                catch (Exception ex) { return; }
            }
        }

        private void TargetImage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                try
                {
                    BitmapSource sour = Clipboard.GetImage();
                    if (sour == null) return;
                    TargetImage.SetSource(sour);
                }
                catch (Exception ex) { return; }
            }
        }
        #endregion
    }
}
