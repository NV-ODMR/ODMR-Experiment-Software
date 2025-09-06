using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.板卡;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;
using Window = System.Windows.Window;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.二维扫描
{
    public class Confocal : Scan2DExpBase<NanoStageInfo, NanoStageInfo>
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = true;

        public override string ODMRExperimentName { get; set; } = "共聚焦扫描";
        public override string ODMRExperimentGroupName { get; set; } = "实空间二维实验";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
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

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
            };
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override string CreateThreadState(NanoStageInfo dev1, NanoStageInfo dev2, Point loc)
        {
            return "共聚焦扫描 X: " + Math.Round(loc.X, 5).ToString() + " Y: " + Math.Round(loc.Y, 5).ToString();
        }

        protected override List<object> FirstScanEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc.X, 3000);
            device2.Device.MoveToAndWait(loc.Y, 3000);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        protected override List<object> EndScanNewLineEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc.X, 3000);
            device2.Device.MoveToAndWait(loc.Y, 3000);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        protected override List<object> StartScanNewLineEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc.X, 3000);
            device2.Device.MoveToAndWait(loc.Y, 3000);
            return ScanCore(device1, device2, range, loc, inputParams);
        }


        protected override List<object> ScanEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            int waittime = GetInputParamValueByName("MoverWaitingTime");
            device1.Device.MoveToAndWait(loc.X, waittime);
            device2.Device.MoveToAndWait(loc.Y, waittime);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        private List<object> ScanCore(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            ConfocalAPDSample a = new ConfocalAPDSample();
            var res = a.CoreMethod(new List<object>() { GetInputParamValueByName("SampleRate") }, GetDeviceByName("APD"));
            int count = (int)(double)res[0];
            //画图
            var chartdata = Get2DChartData("计数率(cps)", "共聚焦扫描结果");
            chartdata.Data.SetValue(range.GetNearestXIndex(loc.X), range.GetNearestYIndex(loc.Y), count);
            UpdatePlotChartFlow(true);
            return new List<object>();
        }


        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override NanoStageInfo GetScanSource1()
        {
            return GetDeviceByName("LenX") as NanoStageInfo;
        }

        public override NanoStageInfo GetScanSource2()
        {
            return GetDeviceByName("LenY") as NanoStageInfo;
        }

        protected override void Preview2DScanEventWithoutAFM()
        {
            //打开激光
            LaserOn lon = new LaserOn();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            lon.CoreMethod(new List<object>() { 1.0 }, pb);
            //设置触发源
            PulseBlasterInfo trace = GetDeviceByName("TraceSource") as PulseBlasterInfo;
            trace.Device.PulseFrequency = GetInputParamValueByName("SampleRate");
            trace.Device.Start();
            //打开APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.StartContinusSample();
            //创建数据集
            D2ScanRangeBase source = GetScanRange();
            //新建数据集
            D2ChartDatas = new List<ChartData2D>()
            {
                new ChartData2D(new FormattedDataSeries2D(source.XCount,source.XLo,source.XHi,source.YCount,source.YLo,source.YHi){
                    XName="X轴位置(μm)",YName="Y轴位置(μm)",ZName="计数率(cps)"}){ GroupName="共聚焦扫描结果"}
            };
            UpdatePlotChart();
            //选中数据集
            Show2DChartData("共聚焦扫描结果", "X轴位置(μm)", "Y轴位置(μm)", "计数率(cps)");
        }

        protected override void After2DScanEventWithoutAFM()
        {
            //关闭激光
            LaserOff loff = new LaserOff();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            loff.CoreMethod(new List<object>() { }, pb);
            //关闭APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.EndContinusSample();
            //关闭APD触发源
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.End();
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }
    }
}
