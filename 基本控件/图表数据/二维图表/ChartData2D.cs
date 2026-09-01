using Controls.Charts;

namespace ODMR_Lab.基本控件
{
    public class ChartData2D : ChartDataBase
    {

        public ChartData2D(FormattedDataSeries2D data)
        {
            Data = data;
        }

        public FormattedDataSeries2D Data { get; set; } = null;

        public string GroupName { get; set; } = "";

        public bool IsSelected { get; set; } = false;

        public string GetDescription()
        {
            return "X: " + Data.XName + " Y: " + Data.YName + " Z: " + Data.ZName;
        }

        public int GetXCount()
        {
            return Data.XCounts;
        }

        public int GetYCount()
        {
            return Data.YCounts;
        }
    }
}
