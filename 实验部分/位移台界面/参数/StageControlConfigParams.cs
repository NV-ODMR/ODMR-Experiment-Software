using ODMR_Lab.IO操作;

namespace ODMR_Lab.实验部分.位移台界面.参数
{
    internal class StageControlConfigParams : ConfigBase
    {
        public Param<bool> ReverseX { get; set; } = new Param<bool>("X反转", false, "ReverseX");

        public Param<bool> ReverseY { get; set; } = new Param<bool>("Y反转", false, "ReverseY");


        public Param<bool> ReverseZ { get; set; } = new Param<bool>("Z反转", false, "ReverseZ");

        public Param<bool> ReverseAngleX { get; set; } = new Param<bool>("角度X反转", false, "ReverseAngleX");

        public Param<bool> ReverseAngleY { get; set; } = new Param<bool>("角度Y反转", false, "ReverseAngleY");

        public Param<bool> ReverseAngleZ { get; set; } = new Param<bool>("角度Z反转", false, "ReverseAngleZ");

    }
}
