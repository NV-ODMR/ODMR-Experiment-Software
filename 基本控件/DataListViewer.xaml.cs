using Controls;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.基本控件
{
    /// <summary>
    /// DataListViewer.xaml 的交互逻辑
    /// </summary>
    public partial class DataListViewer : Grid
    {

        public ItemsList<ViewerTemplate> HeaderTemplate { get; set; } = new ItemsList<ViewerTemplate>();

        public ContextMenu ContextTemplate { get; set; } = null;

        public double MinItemWidth
        {
            get { return (double)GetValue(MinItemWidthProperty); }
            set { SetValue(MinItemWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinItemWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinItemWidthProperty =
            DependencyProperty.Register("MinItemWidth", typeof(double), typeof(DataListViewer), new PropertyMetadata(200.0));



        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(DataListViewer), new PropertyMetadata(30.0));



        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register("HeaderHeight", typeof(double), typeof(DataListViewer), new PropertyMetadata(30.0));



        public bool IsMultiSelected
        {
            get { return (bool)GetValue(IsMultiSelectedProperty); }
            set { SetValue(IsMultiSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMultiSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMultiSelectedProperty =
            DependencyProperty.Register("IsMultiSelected", typeof(bool), typeof(DataListViewer), new PropertyMetadata(false));





        public DataListViewer()
        {
            InitializeComponent();
            datacontentscroll.HeaderHeight = HeaderHeight;
            HeaderTemplate.ItemChanged += UpdateHeader;
            datacontentscroll.ItemSelected += DataItemSelected;
            datacontentscroll.ItemValueChanged += DataItemValueChanged;
            datacontentscroll.ItemContextMenuSelected += DataItemContextMenuSelected;
            datacontentscroll.MultiItemSelected += DataMultiSelected;
            datacontentscroll.MultiItemUnSelected += DataMultiUnSelected;
            datacontentscroll.UpdateHeader();
            datacontentscroll.ItemContextMenu = ContextTemplate;
        }

        private void UpdateHeader(object sender, RoutedEventArgs e)
        {
            datacontentscroll.DataTemplate = HeaderTemplate.ToList();
            datacontentscroll.UpdateHeader();
        }


        public event Action<int, object> ItemSelected = null;

        public event Action<int, int, object> ItemValueChanged = null;

        public event Action<int, int, object> ItemContextMenuSelected = null;

        public event Action<int, object> MultiItemSelected = null;

        public event Action<int, object> MultiItemUnSelected = null;

        private void DataItemValueChanged(int arg1, int arg2, object arg3)
        {
            values[CurrentPageIndex * DisplayCount + arg1][arg2] = arg3;
            ItemValueChanged?.Invoke(CurrentPageIndex * DisplayCount + arg1, arg2, arg3);
        }

        private void DataItemSelected(int arg1, object arg2)
        {
            ItemSelected?.Invoke(CurrentPageIndex * DisplayCount + arg1, arg2);
        }
        private void DataItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            ItemContextMenuSelected?.Invoke(arg1, CurrentPageIndex * DisplayCount + arg2, arg3);
        }

        private void DataMultiUnSelected(int arg1, object arg2)
        {
            MultiItemUnSelected?.Invoke(CurrentPageIndex * DisplayCount + arg1, arg2);
        }

        private void DataMultiSelected(int arg1, object arg2)
        {
            MultiItemSelected?.Invoke(CurrentPageIndex * DisplayCount + arg1, arg2);
        }

        /// <summary>
        /// 显示的点数量
        /// </summary>
        int DisplayCount = 10;

        public int CurrentPageIndex { private set; get; } = 0;

        public void Select(int ind)
        {
            UpdatePointList(ind / DisplayCount);
            datacontentscroll.Select(ind - CurrentPageIndex * DisplayCount);
        }

        public void UpdatePointList(int pageindex, string name = "")
        {
            int startIndex = DisplayCount * pageindex;
            datacontentscroll.UpdateHeader();
            if (startIndex > tags.Count() - 1)
            {
                startIndex = DisplayCount * (pageindex - 1);
            }
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            CurrentPageIndex = startIndex / DisplayCount;

            datacontentscroll.ClearItems();
            for (int i = startIndex; i < startIndex + DisplayCount; i++)
            {
                if (i > tags.Count - 1) continue;
                datacontentscroll.AddItem(tags[i], values[i].ToList());
            }
        }

        List<object> tags = new List<object>();
        List<object[]> values = new List<object[]>();

        public void AddItem(object tag, params object[] value)
        {
            UpdateHeader(null, new RoutedEventArgs());
            tags.Add(tag);
            values.Add(value);
            //如果在显示范围内则加载
            if (tags.Count - 1 < (CurrentPageIndex + 1) * DisplayCount && tags.Count - 1 >= CurrentPageIndex * DisplayCount)
            {
                datacontentscroll.AddItem(tag, value.ToList());
            }
        }

        public void SetCellValue(int rowind, int columnind, object value)
        {
            values[rowind][columnind] = value;

            //如果在显示范围内则加载
            if (rowind < (CurrentPageIndex + 1) * DisplayCount && rowind >= CurrentPageIndex * DisplayCount)
            {
                datacontentscroll.SetCelValue(rowind - CurrentPageIndex * DisplayCount, columnind, value);
            }
        }

        public object GetCellValue(int rowind, int columnind)
        {
            //如果在显示范围内则加载
            if (tags.Count - 1 < (CurrentPageIndex + 1) * DisplayCount && tags.Count - 1 >= CurrentPageIndex * DisplayCount)
            {
                return datacontentscroll.GetCellValue(rowind - CurrentPageIndex * DisplayCount, columnind);
            }
            return values[rowind][columnind];
        }

        public object GetTag(int rowind)
        {
            return tags[rowind];
        }

        public int GetRowCount()
        {
            return tags.Count;
        }

        public void ClearItems()
        {
            tags.Clear();
            values.Clear();
            datacontentscroll.ClearItems();
        }

        public void RemoveItem(int ind)
        {
            tags.RemoveAt(ind);
            values.RemoveAt(ind);
            datacontentscroll.DeleteItems(ind);
        }

        private void FormerDataList(object sender, RoutedEventArgs e)
        {
            CurrentPageIndex -= 1;
            UpdatePointList(CurrentPageIndex);
            DisplayIndex.Text = CurrentPageIndex.ToString();
        }

        private void LaterDataList(object sender, RoutedEventArgs e)
        {
            CurrentPageIndex += 1;
            UpdatePointList(CurrentPageIndex);
            DisplayIndex.Text = CurrentPageIndex.ToString();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    TextBox b = sender as TextBox;
                    uint ind = uint.Parse(b.Text);
                    UpdatePointList((int)ind);
                    DisplayIndex.Text = CurrentPageIndex.ToString();
                }
                catch (Exception ex) { }
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

            datacontentscroll.DataTemplate = HeaderTemplate;
            datacontentscroll.ItemHeight = ItemHeight;
            datacontentscroll.MinItemWidth = MinItemWidth;
            datacontentscroll.IsMultiSelected = IsMultiSelected;
            datacontentscroll.ItemContextMenu = ContextTemplate;
            datacontentscroll.UpdateHeader();
        }
    }
}
