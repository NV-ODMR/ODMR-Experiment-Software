using ODMR_Lab.IO操作;
using ODMR_Lab.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.磁场调节
{
    /// <summary>
    /// 磁场调节参数列表
    /// </summary>
    public class MagnetScanConfigParams : ConfigBase
    {
        #region 预设参数
        /// <summary>
        /// 磁铁半径
        /// </summary>
        public Param<double> MRadius { get; set; } = new Param<double>("磁铁半径(mm)", double.NaN, "MRadius");

        /// <summary>
        /// 磁铁长度
        /// </summary>
        public Param<double> MLength { get; set; } = new Param<double>("磁铁长度(mm)", double.NaN, "MLength");

        public Param<double> MIntensity { get; set; } = new Param<double>("磁铁磁化强度系数", double.NaN, "MIntensity");

        /// <summary>
        /// 偏心量X
        /// </summary>
        public Param<double> OffsetX { get; set; } = new Param<double>("偏心量X(mm)", double.NaN, "OffsetX");

        /// <summary>
        /// 偏心量Y
        /// </summary>
        public Param<double> OffsetY { get; set; } = new Param<double>("偏心量Y(mm)", double.NaN, "OffsetY");

        /// <summary>
        /// X对应位移台轴
        /// </summary>
        public Param<MoverTypes> XRelate { get; set; } = new Param<MoverTypes>("X对应位移台轴", MoverTypes.None, "XRelate");

        /// <summary>
        /// Y对应位移台轴
        /// </summary>
        public Param<MoverTypes> YRelate { get; set; } = new Param<MoverTypes>("Y对应位移台轴", MoverTypes.None, "YRelate");

        /// <summary>
        /// Z对应位移台轴
        /// </summary>
        public Param<MoverTypes> ZRelate { get; set; } = new Param<MoverTypes>("Z对应位移台轴", MoverTypes.None, "ZRelate");

        /// <summary>
        /// 角度对应位移台轴
        /// </summary>
        public Param<MoverTypes> ARelate { get; set; } = new Param<MoverTypes>("角度对应位移台轴", MoverTypes.None, "ARelate");

        /// <summary>
        /// X反转值 
        /// </summary>
        public Param<bool> ReverseX { get; set; } = new Param<bool>("反转X轴", false, "ReverseX");

        /// <summary>
        /// Y反转值 
        /// </summary>
        public Param<bool> ReverseY { get; set; } = new Param<bool>("反转Y轴", false, "ReverseY");

        /// <summary>
        /// Z反转值 
        /// </summary>
        public Param<bool> ReverseZ { get; set; } = new Param<bool>("反转Z轴", false, "ReverseZ");

        /// <summary>
        /// 角度反转值 
        /// </summary>
        public Param<bool> ReverseA { get; set; } = new Param<bool>("反转角度", false, "ReverseA");

        /// <summary>
        /// 进行X扫描时的旋转台角度
        /// </summary>
        public double AngleX { get; set; } = double.NaN;
        /// <summary>
        /// 进行Y扫描时的旋转台角度
        /// </summary>
        public double AngleY { get; set; } = double.NaN;

        /// <summary>
        ///基准角度
        /// </summary>
        public Param<double> AngleStart { get; set; } = new Param<double>("角度零点(度)", double.NaN, "AngleStart");

        /// <summary>
        /// X扫描范围
        /// </summary>
        public Param<double> XScanHi { get; set; } = new Param<double>("X扫描范围上限(mm)", double.NaN, "XScanHi");

        /// <summary>
        /// X扫描范围 
        /// </summary>
        public Param<double> XScanLo { get; set; } = new Param<double>("X扫描范围下限(mm)", double.NaN, "XScanLo");

        /// <summary>
        /// Y扫描范围
        /// </summary>
        public Param<double> YScanHi { get; set; } = new Param<double>("Y扫描范围上限(mm)", double.NaN, "YScanHi");

        /// <summary>
        /// Y扫描范围 
        /// </summary>
        public Param<double> YScanLo { get; set; } = new Param<double>("Y扫描范围下限(mm)", double.NaN, "YScanLo");

        /// <summary>
        /// Z扫描高度
        /// </summary>
        public Param<double> ZPlane { get; set; } = new Param<double>("Z扫描高度(mm)", double.NaN, "ZPlane");

        /// <summary>
        /// X位移台行程
        /// </summary>
        public Param<double> XRangeLo { get; set; } = new Param<double>("X位移台行程下限(mm)", double.NaN, "XRangeLo");

        /// <summary>
        /// X位移台行程
        /// </summary>
        public Param<double> XRangeHi { get; set; } = new Param<double>("X位移台行程上限(mm)", double.NaN, "XRangeHi");

        /// <summary>
        /// Y位移台行程
        /// </summary>
        public Param<double> YRangeLo { get; set; } = new Param<double>("Y位移台行程下限(mm)", double.NaN, "YRangeLo");

        /// <summary>
        /// Y位移台行程
        /// </summary>
        public Param<double> YRangeHi { get; set; } = new Param<double>("Y位移台行程上限(mm)", double.NaN, "YRangeHi");

        /// <summary>
        /// Z位移台行程
        /// </summary>
        public Param<double> ZRangeLo { get; set; } = new Param<double>("Z位移台行程下限(mm)", double.NaN, "ZRangeLo");

        /// <summary>
        /// Z位移台行程
        /// </summary>
        public Param<double> ZRangeHi { get; set; } = new Param<double>("Z位移台行程上限(mm)", double.NaN, "ZRangeHi");

        /// <summary>
        /// D值
        /// </summary>
        public Param<double> D { get; set; } = new Param<double>("D值(Mhz)", double.NaN, "D");

        #endregion

    }
}
