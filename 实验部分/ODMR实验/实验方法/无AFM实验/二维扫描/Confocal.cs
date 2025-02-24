using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.二维扫描
{
    public class Confocal : Scan2DExpBase<NanoStageInfo, NanoStageInfo>
    {
        public override string ODMRExperimentName { get; set; } = "共聚焦扫描";
        public override string ODMRExperimentGroupName { get; set; } = "实空间面实验(无AFM)";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("X扫描下限(μm)",0,"XRangeLo"),
            new Param<double>("X扫描上限(μm)",0,"XRangeHi"),
            new Param<double>("Y扫描下限(μm)",0,"YRangeLo"),
            new Param<double>("Y扫描上限(μm)",0,"YRangeHi"),
            new Param<int>("X扫描点数",0,"XCount"),
            new Param<int>("Y扫描点数",0,"YCount"),
            new Param<bool>("X扫描反向",false,"XReverse"),
            new Param<bool>("Y扫描反向",false,"YReverse"),
            new Param<int>("采样率(Hz)",60,"SampleRate"),
            new Param<int>("位移台等待时间(ms)",0,"MoverWaitingTime")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {

        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头X","","LenX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头Y","","LenY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("激光触发源","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("APD Trace 触发源","","TraceSource")),
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();
        public override List<KeyValuePair<string, Action>> InterativeButtons { get; set; } = new List<KeyValuePair<string, Action>>();

        public override string CreateThreadState(NanoStageInfo dev1, NanoStageInfo dev2, double currentvalue1, double currentvalue2)
        {
            return "共聚焦扫描 X: " + Math.Round(currentvalue1, 5).ToString() + " Y: " + Math.Round(currentvalue2, 5).ToString();
        }

        public override List<object> FirstScanEvent(NanoStageInfo device1, NanoStageInfo device2, ScanRange range1, ScanRange range2, double loc1value, double loc2value, List<object> inputParams)
        {
            return ScanEvent(device1, device2, range1, range2, loc1value, loc2value, inputParams);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override List<object> ScanEvent(NanoStageInfo device1, NanoStageInfo device2, ScanRange range1, ScanRange range2, double loc1value, double loc2value, List<object> inputParams)
        {
            int waittime = GetInputParamValueByName("MoverWaitingTime");
            device1.Device.MoveToAndWait(loc1value, waittime);
            device2.Device.MoveToAndWait(loc2value, waittime);
            ConfocalAPDSample a = new ConfocalAPDSample();
            var res = a.CoreMethod(new List<object>() { GetInputParamValueByName("SampleRate") }, GetDeviceByName("APD"));
            int count = (int)(double)res[0];
            var chartdata = Get2DChartData("计数率(cps)", "共聚焦扫描结果");
            chartdata.Data.SetValue(range1.GetNearestIndex(loc1value), range2.GetNearestIndex(loc2value), count);
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override ScanRange GetScanRange1()
        {
            double xlo = GetInputParamValueByName("XRangeLo");
            double xhi = GetInputParamValueByName("XRangeHi");
            int count = GetInputParamValueByName("XCount");
            bool rev = GetInputParamValueByName("XReverse");
            return new ScanRange(xlo, xhi, count, rev);
        }

        public override ScanRange GetScanRange2()
        {
            double ylo = GetInputParamValueByName("YRangeLo");
            double yhi = GetInputParamValueByName("YRangeHi");
            int count = GetInputParamValueByName("YCount");
            bool rev = GetInputParamValueByName("YReverse");
            return new ScanRange(ylo, yhi, count, rev);
        }

        public override NanoStageInfo GetScanSource1()
        {
            return GetDeviceByName("LenX") as NanoStageInfo;
        }

        public override NanoStageInfo GetScanSource2()
        {
            return GetDeviceByName("LenY") as NanoStageInfo;
        }

        public override void PreExpEventWithoutAFM()
        {
            //打开激光
            LaserOn lon = new LaserOn();
            PulseBlasterInfo pb = GetDeviceByName("TraceSource") as PulseBlasterInfo;
            var chind = pb.FindChannelEnumOfDescription("激光触发源");
            lon.CoreMethod(new List<object>() { 1.0, chind }, pb);
            //打开APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.StartContinusSample();
            //创建数据集
            var r1 = GetScanRange1();
            var r2 = GetScanRange2();
            //新建数据集
            D2ChartDatas = new List<ChartData2D>()
            {
                new ChartData2D(new FormattedDataSeries2D(r1.Count,r1.Lo,r1.Hi,r2.Count,r2.Lo,r2.Hi){
                    XName="X轴位置(μm)",YName="Y轴位置(μm)",ZName="计数率(cps)"}){ GroupName="共聚焦扫描结果"}
            };
            UpdatePlotChart();
            //选中数据集
            Show2DChartData("共聚焦扫描结果", "X轴位置(μm)", "Y轴位置(μm)", "计数率(cps)");
        }

        public override void AfterExpEventWithoutAFM()
        {
            //关闭激光
            LaserOff loff = new LaserOff();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            var chind = pb.FindChannelEnumOfDescription("激光触发源");
            loff.CoreMethod(new List<object>() { 1, chind }, pb);
            //关闭APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.EndContinusSample();
            //关闭APD触发源
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.End();
        }
    }
}
