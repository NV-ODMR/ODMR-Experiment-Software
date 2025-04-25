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
                if (fobj.ODMRExperimentName != "定位程序（确定NV朝向）" || fobj.ODMRExperimentGroupName != "磁场定位")
                {
                    throw new Exception();
                }
                //查找参数
                MagnetLocParams Ps = new MagnetLocParams();
                Ps.XLoc = double.Parse(fobj.GetOutputParamValueByName("XLoc"));
                Ps.YLoc = double.Parse(fobj.GetOutputParamValueByName("YLoc"));
                Ps.ZLoc = double.Parse(fobj.GetOutputParamValueByName("ZLoc"));
                Ps.ZDistance = double.Parse(fobj.GetOutputParamValueByName("ZDistance"));
                Ps.CheckedTheta = double.Parse(fobj.GetOutputParamValueByName("CheckedTheta"));
                Ps.D = double.Parse(fobj.GetInputParamValueByName("D"));
                Ps.CheckedPhi = double.Parse(fobj.GetOutputParamValueByName("CheckedPhi"));
                Ps.XReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseX"));
                Ps.YReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseY"));
                Ps.ZReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseZ"));
                Ps.AReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseA"));
                Ps.AngleStart = double.Parse(fobj.GetInputParamValueByName("AngleStart"));
                Ps.MRadius = double.Parse(fobj.GetInputParamValueByName("MRadius"));
                Ps.MLength = double.Parse(fobj.GetInputParamValueByName("MLength"));
                Ps.OffsetX = double.Parse(fobj.GetInputParamValueByName("OffsetX"));
                Ps.OffsetY = double.Parse(fobj.GetInputParamValueByName("OffsetY"));
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
                if (fobj.ODMRExperimentName != "定位程序（确定NV朝向）" || fobj.ODMRExperimentGroupName != "磁场定位")
                {
                    throw new Exception();
                }
                //查找参数
                MagnetLocParams Ps = new MagnetLocParams();
                Ps.XLoc = double.Parse(fobj.GetOutputParamValueByName("XLoc"));
                Ps.YLoc = double.Parse(fobj.GetOutputParamValueByName("YLoc"));
                Ps.ZLoc = double.Parse(fobj.GetOutputParamValueByName("ZLoc"));
                Ps.ZDistance = double.Parse(fobj.GetOutputParamValueByName("ZDistance"));
                Ps.CheckedTheta = double.Parse(fobj.GetOutputParamValueByName("CheckedTheta"));
                Ps.D = double.Parse(fobj.GetInputParamValueByName("D"));
                Ps.CheckedPhi = double.Parse(fobj.GetOutputParamValueByName("CheckedPhi"));
                Ps.XReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseX"));
                Ps.YReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseY"));
                Ps.ZReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseZ"));
                Ps.AReverse = bool.Parse(fobj.GetInputParamValueByName("ReverseA"));
                Ps.AngleStart = double.Parse(fobj.GetInputParamValueByName("AngleStart"));
                Ps.MRadius = double.Parse(fobj.GetInputParamValueByName("MRadius"));
                Ps.MLength = double.Parse(fobj.GetInputParamValueByName("MLength"));
                Ps.OffsetX = double.Parse(fobj.GetInputParamValueByName("OffsetX"));
                Ps.OffsetY = double.Parse(fobj.GetInputParamValueByName("OffsetY"));
                return Ps;
            }
            return null;
        }
    }
}
