using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.实验部分.ODMR实验.参数;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他.磁场调节.通用方法
{
    public class MagnetLocParams
    {
        public double XLoc { get; set; } = double.NaN;

        public double YLoc { get; set; } = double.NaN;

        public double ZLoc { get; set; } = double.NaN;

        public double ZDistance { get; set; } = double.NaN;
        public double CheckedTheta { get; set; } = double.NaN;
        public double CheckedPhi { get; set; } = double.NaN;

        public bool XReverse { get; set; } = false;
        public bool YReverse { get; set; } = false;
        public bool ZReverse { get; set; } = false;
        public bool AReverse { get; set; } = false;

        public double AngleStart { get; set; } = 0;

        public double MRadius { get; set; } = double.NaN;
        public double MLength { get; set; } = double.NaN;

        public double OffsetX { get; set; } = double.NaN;
        public double OffsetY { get; set; } = double.NaN;
        public double D { get; set; } = double.NaN;

        /// <summary>
        /// 从文件中读取,读不到报错
        /// </summary>
        /// <returns></returns>
        public static MagnetLocParams ReadFromExplorer(out string filepath)
        {
            SequenceFileExpObject fobj = new SequenceFileExpObject();
            if (fobj.ReadFromExplorer(out filepath))
            {
                if (fobj.ODMRExperimentName != "定位程序(确定NV朝向)" || fobj.ODMRExperimentGroupName != "磁场定位")
                {
                    throw new Exception();
                }
                //查找参数
                MagnetLocParams Ps = new MagnetLocParams();
                Ps.XLoc = fobj.GetOutputParamValueByName("XLoc");
                Ps.YLoc = fobj.GetOutputParamValueByName("YLoc");
                Ps.ZLoc = fobj.GetOutputParamValueByName("ZLoc");
                Ps.ZDistance = fobj.GetOutputParamValueByName("ZDistance");
                Ps.CheckedTheta = fobj.GetOutputParamValueByName("CheckedTheta");
                Ps.D = fobj.GetInputParamValueByName("D");
                Ps.CheckedPhi = fobj.GetOutputParamValueByName("CheckedPhi");
                Ps.XReverse = fobj.GetInputParamValueByName("XReverse");
                Ps.YReverse = fobj.GetInputParamValueByName("YReverse");
                Ps.ZReverse = fobj.GetInputParamValueByName("ZReverse");
                Ps.AReverse = fobj.GetInputParamValueByName("AReverse");
                Ps.AngleStart = fobj.GetInputParamValueByName("AngleStart");
                Ps.MRadius = fobj.GetInputParamValueByName("MRadius");
                Ps.MLength = fobj.GetInputParamValueByName("MLength");
                Ps.OffsetX = fobj.GetInputParamValueByName("OffsetX");
                Ps.OffsetY = fobj.GetInputParamValueByName("OffsetY");
                return Ps;
            }
            return null;
        }

        /// <summary>
        /// 从文件中读取,读不到报错
        /// </summary>
        /// <returns></returns>
        public static MagnetLocParams ReadFromFile(string filepath)
        {
            SequenceFileExpObject fobj = new SequenceFileExpObject();
            if (fobj.ReadFromFile(filepath))
            {
                if (fobj.ODMRExperimentName != "定位程序(确定NV朝向)" || fobj.ODMRExperimentGroupName != "磁场定位")
                {
                    throw new Exception();
                }
                //查找参数
                MagnetLocParams Ps = new MagnetLocParams();
                Ps.XLoc = fobj.GetOutputParamValueByName("XLoc");
                Ps.YLoc = fobj.GetOutputParamValueByName("YLoc");
                Ps.ZLoc = fobj.GetOutputParamValueByName("ZLoc");
                Ps.ZDistance = fobj.GetOutputParamValueByName("ZDistance");
                Ps.CheckedTheta = fobj.GetOutputParamValueByName("CheckedTheta");
                Ps.D = fobj.GetInputParamValueByName("D");
                Ps.CheckedPhi = fobj.GetOutputParamValueByName("CheckedPhi");
                Ps.XReverse = fobj.GetInputParamValueByName("XReverse");
                Ps.YReverse = fobj.GetInputParamValueByName("YReverse");
                Ps.ZReverse = fobj.GetInputParamValueByName("ZReverse");
                Ps.AReverse = fobj.GetInputParamValueByName("AReverse");
                Ps.AngleStart = fobj.GetInputParamValueByName("AngleStart");
                Ps.MRadius = fobj.GetInputParamValueByName("MRadius");
                Ps.MLength = fobj.GetInputParamValueByName("MLength");
                Ps.OffsetX = fobj.GetInputParamValueByName("OffsetX");
                Ps.OffsetY = fobj.GetInputParamValueByName("OffsetY");
                return Ps;
            }
            return null;
        }
    }
}
