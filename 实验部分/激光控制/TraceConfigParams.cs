using ODMR_Lab.IO操作;

namespace ODMR_Lab.激光控制
{
    public class TraceConfigParams : ConfigBase
    {
        public Param<double> SampleFreq { get; set; } = new Param<double>("采样频率", 100, "SampleFreq");

        public Param<int> MaxDisplayPoint { get; set; } = new Param<int>("最大显示点数", 1000, "MaxDisplayPoint");

        public Param<int> MaxSavePoint { get; set; } = new Param<int>("最大保存点数", 100000, "MaxSavePoint");
    }
}