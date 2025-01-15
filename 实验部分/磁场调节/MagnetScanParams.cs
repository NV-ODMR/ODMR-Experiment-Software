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
    public class MagnetScanParams : ParamBase
    {
        #region 设备信息
        public Param<string> XDeviceName { get; set; } = new Param<string>("X方向设备", "");

        public Param<string> YDeviceName { get; set; } = new Param<string>("Y方向设备", "");

        public Param<string> ZDeviceName { get; set; } = new Param<string>("Z方向设备", "");

        public Param<string> RotName { get; set; } = new Param<string>("旋转台设备", "");
        #endregion

        #region 预设参数
        /// <summary>
        /// 磁铁半径
        /// </summary>
        public Param<double> MRadius { get; set; } = new Param<double>("磁铁半径(mm)", double.NaN);

        /// <summary>
        /// 磁铁长度
        /// </summary>
        public Param<double> MLength { get; set; } = new Param<double>("磁铁长度(mm)", double.NaN);

        /// <summary>
        /// 偏心量X
        /// </summary>
        public Param<double> OffsetX { get; set; } = new Param<double>("偏心量X(mm)", double.NaN);

        /// <summary>
        /// 偏心量Y
        /// </summary>
        public Param<double> OffsetY { get; set; } = new Param<double>("偏心量Y(mm)", double.NaN);


        public MoverTypes XRelate { get; set; } = MoverTypes.None;
        public MoverTypes YRelate { get; set; } = MoverTypes.None;
        public MoverTypes ZRelate { get; set; } = MoverTypes.None;

        /// <summary>
        /// X反转值 
        /// </summary>
        public Param<int> ReverseXNum { get; set; } = new Param<int>("X轴反转系数", 1);

        /// <summary>
        /// Y反转值 
        /// </summary>
        public Param<int> ReverseYNum { get; set; } = new Param<int>("Y轴反转系数", 1);

        /// <summary>
        /// Z反转值 
        /// </summary>
        public Param<int> ReverseZNum { get; set; } = new Param<int>("Z轴反转系数", 1);

        /// <summary>
        /// 角度反转值 
        /// </summary>
        public Param<int> ReverseANum { get; set; } = new Param<int>("角度反转系数", 1);

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
        public Param<double> StartAngle { get; set; } = new Param<double>("角度零点(度)", double.NaN);

        /// <summary>
        /// X扫描范围
        /// </summary>
        public Param<double> XScanHi { get; set; } = new Param<double>("X扫描范围上限(mm)", double.NaN);

        /// <summary>
        /// X扫描范围 
        /// </summary>
        public Param<double> XScanLo { get; set; } = new Param<double>("X扫描范围下限(mm)", double.NaN);

        /// <summary>
        /// Y扫描范围
        /// </summary>
        public Param<double> YScanHi { get; set; } = new Param<double>("Y扫描范围上限(mm)", double.NaN);

        /// <summary>
        /// Y扫描范围 
        /// </summary>
        public Param<double> YScanLo { get; set; } = new Param<double>("Y扫描范围下限(mm)", double.NaN);

        /// <summary>
        /// Z扫描高度
        /// </summary>
        public Param<double> ZPlane { get; set; } = new Param<double>("Z扫描高度(mm)", double.NaN);

        /// <summary>
        /// X位移台行程
        /// </summary>
        public Param<double> XRangeLo { get; set; } = new Param<double>("X位移台行程下限(mm)", double.NaN);

        /// <summary>
        /// X位移台行程
        /// </summary>
        public Param<double> XRangeHi { get; set; } = new Param<double>("X位移台行程上限(mm)", double.NaN);

        /// <summary>
        /// Y位移台行程
        /// </summary>
        public Param<double> YRangeLo { get; set; } = new Param<double>("Y位移台行程下限(mm)", double.NaN);

        /// <summary>
        /// Y位移台行程
        /// </summary>
        public Param<double> YRangeHi { get; set; } = new Param<double>("Y位移台行程上限(mm)", double.NaN);

        /// <summary>
        /// Z位移台行程
        /// </summary>
        public Param<double> ZRangeLo { get; set; } = new Param<double>("Z位移台行程下限(mm)", double.NaN);

        /// <summary>
        /// Z位移台行程
        /// </summary>
        public Param<double> ZRangeHi { get; set; } = new Param<double>("Z位移台行程上限(mm)", double.NaN);

        /// <summary>
        /// D值
        /// </summary>
        public Param<double> D { get; set; } = new Param<double>("D值(Mhz)", double.NaN);

        #endregion

        #region 运行时参数
        /// <summary>
        /// X扫描结果
        /// </summary>
        public Param<double> XLoc { get; set; } = new Param<double>("X方向磁场峰值位置(mm)", double.NaN);

        /// <summary>
        /// Y扫描结果
        /// </summary>
        public Param<double> YLoc { get; set; } = new Param<double>("Y方向磁场峰值位置(mm)", double.NaN);

        /// <summary>
        /// Z扫描结果
        /// </summary>
        public Param<double> ZLoc { get; set; } = new Param<double>("Z方向位置(mm)", double.NaN);

        /// <summary>
        /// Z扫描结果
        /// </summary>
        public Param<double> ZDistance { get; set; } = new Param<double>("Z方向位置和磁铁的距离(mm)", double.NaN);

        /// <summary>
        /// 角度扫描结果
        /// </summary>
        public Param<double> Theta1 { get; set; } = new Param<double>("方位角θ1(度)", double.NaN);

        /// <summary>
        /// 角度扫描结果
        /// </summary>
        public Param<double> Theta2 { get; set; } = new Param<double>("方位角θ2(度)", double.NaN);

        /// <summary>
        /// 角度扫描结果
        /// </summary>
        public Param<double> Phi1 { get; set; } = new Param<double>("方位角φ1(度)", double.NaN);

        /// <summary>
        /// 角度扫描结果
        /// </summary>
        public Param<double> Phi2 { get; set; } = new Param<double>("方位角φ2(度)", double.NaN);
        #endregion
    }
}
