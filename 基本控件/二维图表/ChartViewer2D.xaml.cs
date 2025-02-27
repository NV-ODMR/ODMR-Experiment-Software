using Controls.Windows;
using Controls;
using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using System.Data;
using System.Threading;
using Controls.Charts;

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// ChartViewer2D.xaml 的交互逻辑
    /// </summary>
    public partial class ChartViewer2D : Grid
    {

        public Window ParentWindow { get; set; } = null;

        /// <summary>
        /// 图表刷新事件,参数(修改后的数据名X,修改后的数据名Y,修改后的数据名Z,分组名)
        /// </summary>
        public Action<string, string, string, string> DataSelected = null;

        private bool reverseX = false;
        public bool ReverseX
        {
            get
            {
                return reverseX;
            }
            set
            {
                if (reverseX != value)
                {
                    ChartObject.RefreshPlot();
                }
                reverseX = value;
            }
        }

        private bool reverseY = false;
        public bool ReverseY
        {
            get
            {
                return reverseY;
            }
            set
            {
                if (reverseY != value)
                {
                    ChartObject.RefreshPlot();
                }
                reverseY = value;
            }
        }

        public ChartViewer2D()
        {
            InitializeComponent();
            DataSource.ItemChanged += UpdateDataPanel;
            MapColorStyle.TemplateButton = MapColorStyle;
            ChartObject.CursorMenuTemplate = ChartGroups;
            foreach (var item in Enum.GetNames(typeof(ColorMaps)))
            {
                MapColorStyle.Items.Add(new DecoratedButton() { Text = item });
            }
        }

        public Point GetSelectedCursor()
        {
            return ChartObject.GetSelectedCursorPoint();
        }

        /// <summary>
        /// 数据
        /// </summary>
        private ItemsList<ChartData2D> dataSource = new ItemsList<ChartData2D>();
        public ItemsList<ChartData2D> DataSource
        {
            get { return dataSource; }
            set
            {
                dataSource = value;
                UpdateDataPanel(this, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// 修改数据显示条元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDataPanel(object sender, RoutedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                UpdateGroups();
                DataSet.ClearItems();
                if (ChartGroups.SelectedItem == null && ChartGroups.Items.Count != 0)
                {
                    ChartGroups.Select(0);
                    return;
                }
                if (ChartGroups.SelectedItem != null)
                {
                    string GroupName = ChartGroups.SelectedItem.Tag as string;
                    UpdateDataPanel(GroupName);
                }
            });
        }

        /// <summary>
        /// 刷新图表数据显示(如果不存在groupName则不做任何操作)
        /// </summary>
        public void UpdateDataPanel(string groupname)
        {
            if (DataSource.Where(x => x.GroupName == groupname).Count() == 0) return;

            DataSet.ClearItems();

            foreach (var item in DataSource)
            {
                if (item.GroupName != groupname)
                {
                    item.IsSelected = false;
                    continue;
                }
                DataSet.AddItem(item, item.GetDescription(), item.GetXCount().ToString(), item.GetYCount().ToString());
            }
        }

        /// <summary>
        /// 刷新数据显示组
        /// </summary>
        private void UpdateGroups()
        {
            #region 查找所有GroupNames
            HashSet<string> groups = new HashSet<string>();
            foreach (ChartData2D data in DataSource)
            {
                groups.Add(data.GroupName);
            }
            ChartGroups.Items.Clear();
            ChartGroups.TemplateButton = ChartGroups;
            foreach (var item in groups)
            {
                DecoratedButton btn = new DecoratedButton() { Text = item };
                btn.Tag = item;
                ChartGroups.Items.Add(btn);
            }
            #endregion
        }

        private void DataSet_ItemSelected(int arg1, object arg2)
        {
            if ((arg2 as ChartData2D).IsSelected) return;
            foreach (var item in DataSource)
            {
                item.IsSelected = false;
            }
            var data = arg2 as ChartData2D;
            data.IsSelected = true;
            DataSelected?.Invoke(data.Data.XName, data.Data.YName, data.Data.ZName, data.GroupName);
            UpdateChartAndDataFlow();
        }

        /// <summary>
        /// 选中面板中的数据
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="xname"></param>
        /// <param name="yname"></param>
        /// <param name="zname"></param>
        public void SelectData(string groupname, string xname, string yname, string zname)
        {
            var data = DataSource.Where(x => x.GroupName == groupname && x.Data.XName == xname && x.Data.YName == yname && x.Data.ZName == zname);
            if (data.Count() != 0)
            {
                UpdateDataPanel(groupname);
                for (int i = 0; i < DataSet.GetRowCount(); i++)
                {
                    ChartData2D d2 = DataSet.GetTag(i) as ChartData2D;
                    if (d2.Data.XName == xname && d2.Data.YName == yname && d2.Data.ZName == zname)
                    {
                        DataSet.Select(i);
                    }
                }
            }
        }


        /// <summary>
        /// 从DataSource中同步数据并刷新绘图
        /// </summary>
        public void UpdateChartAndDataFlow()
        {
            //刷新数据点数
            UpdateDataPoint();

            ChartData2D SelectedXdata = null;
            foreach (var item in DataSource)
            {
                if (item.IsSelected) SelectedXdata = item;
            }
            if (SelectedXdata == null)
            {
                Dispatcher.Invoke(() =>
                {
                    ChartObject.Data = null;
                    ChartObject.RefreshPlot();
                });
                return;
            }

            ChartObject.XAxisName = SelectedXdata.Data.XName;
            ChartObject.YAxisName = SelectedXdata.Data.YName;
            ChartObject.Data = SelectedXdata.Data;
            ChartObject.RefreshPlot();
            ApplyChartStyle(StyleParam);
        }

        /// <summary>
        /// 刷新点数显示
        /// </summary>
        private void UpdateDataPoint()
        {
            for (int i = 0; i < DataSet.GetRowCount(); i++)
            {
                DataSet.SetCelValue(i, 1, (DataSet.GetTag(i) as ChartData2D).GetXCount());
                DataSet.SetCelValue(i, 2, (DataSet.GetTag(i) as ChartData2D).GetYCount());
            }
        }

        private void ChangeGroup(object sender, RoutedEventArgs e)
        {
            string s = ChartGroups.SelectedItem.Text;
            UpdateDataPanel(s);
            UpdateChartAndDataFlow();
        }

        #region 参数设置
        Chart2DStyleParams StyleParam = (Chart2DStyleParams)Chart2DStyleParams.GlobalChartPlotParam.Copy();
        private void ApplyLineStyle(object sender, RoutedEventArgs e)
        {
            StyleParam.ReadFromPage(new FrameworkElement[] { this });
            Chart2DStyleParams.GlobalChartPlotParam = (Chart2DStyleParams)StyleParam.Copy();
            ApplyChartStyle(StyleParam);
        }

        private void ApplyLineStyleKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StyleParam.ReadFromPage(new FrameworkElement[] { this });
                Chart2DStyleParams.GlobalChartPlotParam = (Chart2DStyleParams)StyleParam.Copy();
                ApplyChartStyle(StyleParam);
            }
        }

        private void ApplyChartStyle(Chart2DStyleParams para)
        {
            Dispatcher.Invoke(() =>
            {
                ChartObject.IsSmooth = para.Smooth.Value;
                try
                {
                    ChartObject.RangeLo = para.ValueLo.Value;
                    ChartObject.RangeHi = para.ValueHi.Value;
                }
                catch (Exception) { }

                ChartObject.ColorMap = para.MapColorStyle.Value;

                ChartObject.IsAutoScale = para.AutoScale.Value;

                if (para.LockCursor.Value)
                {
                    ChartObject.LockCursors();
                }
                else
                    ChartObject.UnLockCursors();

                ChartObject.RefreshPlot();
            });
        }

        public void LockPlotCursor()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LockCursor.IsSelected = true;
            });
        }
        public void UnLockPlotCursor()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LockCursor.IsSelected = false;
            });
        }

        public List<Point> GetCursors()
        {
            return ChartObject.GetCursors();
        }
        #endregion

        #region 按钮操作
        private void ResizePlot(object sender, RoutedEventArgs e)
        {
            bool vlue = ChartObject.IsAutoScale;
            ChartObject.IsAutoScale = true;
            ChartObject.RefreshPlot();
            ChartObject.IsAutoScale = vlue;
        }

        private void Snap(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(CodeHelper.SnapHelper.GetControlSnap(ChartObject));
            TimeWindow window = new TimeWindow();
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowWindow("截图已复制到剪切板");
        }

        private void SaveAsExternal(object sender, RoutedEventArgs e)

        {
            string name = (sender as DecoratedButton).Name;
            string save = "";

            string namestr = "";

            List<double> xdat = ChartObject.Data.GetFormattedXData();

            List<double> ydat = ChartObject.Data.GetFormattedYData();

            namestr += ChartObject.Data.XName + "\t" + ChartObject.Data.YName + "\t" + ChartObject.Data.ZName + "\n";

            save += namestr;

            int xcount = xdat.Count;
            int ycount = ydat.Count;


            string datastr = "";
            for (int i = 0; i < xcount; i++)
            {
                string temp = xdat[i].ToString() + "\t";
                if (i < ycount)
                    temp += ydat[i].ToString() + "\t";
                else
                {
                    temp += "\t";
                }
                for (int j = 0; j < ycount; j++)
                {
                    temp += ChartObject.Data.GetValue(i, j) + "\t";
                }
                datastr += temp + "\n";
            }

            if (datastr != "")
            {
                datastr = datastr.Remove(datastr.Length - 1, 1);
            }

            save += datastr;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "文本文件 (*.txt)|*.txt";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter wr = new StreamWriter(new FileStream(dlg.FileName, FileMode.Create)))
                    {
                        wr.Write(save);
                    }
                    TimeWindow win = new TimeWindow();
                    win.Owner = Window.GetWindow(this);
                    win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    win.ShowWindow("文件已保存");
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("文件未成功保存，原因：" + ex.Message, ParentWindow);
                }
            }

        }
        #endregion

        #region 光标
        private void AddCursor(object sender, RoutedEventArgs e)
        {
            ChartObject.AddCursorCenter();
            CursorCount.Content = ChartObject.GetCursorCount();
        }

        private void BringCursorToCenter(object sender, RoutedEventArgs e)
        {
            ChartObject.BringAllCursorToCenter();
        }
        #endregion
    }
}
