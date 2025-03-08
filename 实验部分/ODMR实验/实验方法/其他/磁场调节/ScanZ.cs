using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.磁场调节;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {

        /// <summary>
        /// Z方向扫描
        /// </summary>
        private void ScanZ(double progresslo, double progresshi)
        {
            //扫第一个点
            (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(GetAngleX(), 60000);
            Scan1DSession<NanoStageInfo> session = new Scan1DSession<NanoStageInfo>();
            session.ProgressBarMethod = new Action<NanoStageInfo, double>((dev, v) =>
            {
                SetProgress(v);
            });
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            session.ScanSource = GetDeviceByName("MagnetZ") as NanoStageInfo;

            //扫第一个点
            double height = GetInputParamValueByName("ZPlane");
            session.BeginScan(new D1NumricLinearScanRange(height, height, 1),
                progresslo,
                progresslo + 1 / 4 * (progresshi - progresslo), this, 0.0, 0.0);
            //扫第二个点
            height = GetInputParamValueByName("ZPlane") + GetReverseNum(GetInputParamValueByName("ReverseZ"));
            session.BeginScan(new D1NumricLinearScanRange(height, height, 1),
                progresslo + 1 / 4 * (progresshi - progresslo),
                progresslo + 1 / 2 * (progresshi - progresslo), this, 0.0, 0.0);

            var bps = Get1DChartDataSource("沿轴磁场(G)", "Z方向磁场信息");
            var bs = Get1DChartDataSource("总磁场(G)", "Z方向磁场信息");
            var bvs = Get1DChartDataSource("垂直轴磁场(G)", "Z方向磁场信息");
            var locs = Get1DChartDataSource("位置", "Z方向磁场信息");

            #region 如果NV磁场分量太小则转90度再测
            if (bvs[0] != 0 && bvs[1] != 0)
            {
                if (bps[0] / bvs[0] < 0.1 || bps[1] / bvs[1] < 0.1)
                {
                    (GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(GetAngleY(), 60000);
                    ClearMagnetDataFromChart("Z方向磁场信息");
                    //扫第一个点
                    height = GetInputParamValueByName("ZPlane");
                    session.BeginScan(new D1NumricLinearScanRange(height, height, 1),
                       progresslo + 1 / 2 * (progresshi - progresslo),
                       progresslo + 3 / 4 * (progresshi - progresslo), this, 0.0, 0.0);
                    //扫第二个点
                    height = GetInputParamValueByName("ZPlane") + GetReverseNum(GetInputParamValueByName("ReverseZ"));
                    session.BeginScan(new D1NumricLinearScanRange(height, height, 1),
                        progresslo + 3 / 4 * (progresshi - progresslo),
                        progresslo + (progresshi - progresslo), this, 0.0, 0.0);
                }
                bps = Get1DChartDataSource("沿轴磁场(G)", "Z方向磁场信息");
                bs = Get1DChartDataSource("总磁场(G)", "Z方向磁场信息");
                locs = Get1DChartDataSource("位置", "Z方向磁场信息");
            }
            #endregion


            #region 计算位置
            double ratio = bps[0] / bps[1];
            double zloc = 0;
            if (ratio < 1)
            {
                ratio = bps[1] / bps[0];
                zloc = locs[1];
            }
            else
            {
                zloc = locs[0];
            }
            if (double.IsInfinity(ratio))
            {
                ratio = bs[0] / bs[1];
                zloc = locs[0];
                if (ratio < 1)
                {
                    ratio = bs[1] / bs[0];
                    zloc = locs[1];
                }
            }
            OutputParams.Add(new Param<double>("Z方向参考位置", zloc, "ZLoc"));
            OutputParams.Add(new Param<double>("参考位置与NV的距离", new Magnet(GetInputParamValueByName("MRadius"), GetInputParamValueByName("MLength"), 1).FindZRoot(locs[0], locs[1], ratio), "ZDistance"));

            #endregion
        }

    }
}
