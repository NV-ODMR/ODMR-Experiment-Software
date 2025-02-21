using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.线扫描
{
    public class CW : Scan1DExpBase<RFSourceInfo>
    {
        public override string ODMRExperimentName { get; set; } = "连续波谱(CW)";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("频率起始点(MHz)",2830,"RFFreqLo"),
            new Param<double>("频率中止点(MHz)",2890,"RFFreqHi"),
            new Param<double>("扫描步长(MHz)",1,"RFStep"),
            new Param<double>("微波功率(dBm)",-20,"RFPower"),
            new Param<bool>("反向扫描",false,"Reverse"),
            new Param<int>("循环次数",1000,"LoopCount"),
            new Param<int>("单点扫描时间上限(ms)",0,"TimeOut")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.射频源,new Param<string>("射频源","","RFSource")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("板卡","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override string CreateThreadState(RFSourceInfo dev, double currentvalue)
        {
            return "CW谱扫描 当前频率: " + Math.Round(currentvalue, 5).ToString();
        }

        public override List<object> FirstScanEvent(RFSourceInfo device, ScanRange range, double locvalue, List<object> inputParams)
        {
            var r1 = GetScanRange();
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("频率","CW对比度数据",ChartDataType.X),
                new NumricChartData1D("对比度","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("频率","CW荧光计数",ChartDataType.X),
                new NumricChartData1D("信号总计数","CW荧光计数",ChartDataType.Y),
                new NumricChartData1D("参考信号总计数","CW荧光计数",ChartDataType.Y),
            };
            UpdatePlotChart();
            return ScanEvent(device, range, locvalue, inputParams);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override List<object> ScanEvent(RFSourceInfo device, ScanRange range, double locvalue, List<object> inputParams)
        {
            CWCore cw = new CWCore();
            var result = cw.CoreMethod(new List<object>() { locvalue, GetInputParamValueByName("RFPower"), GetInputParamValueByName("LoopCount"), GetInputParamValueByName("TimeOut") },
                GetDeviceByName("PB"), GetDeviceByName("RFSource"), GetDeviceByName("APD"));

            double contrast = (double)result[0];
            double signalcount = (int)result[1];
            double refcount = (int)result[2];

            (Get1DChartData("频率", "CW对比度数据") as NumricChartData1D).Data.Add(locvalue);
            (Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D).Data.Add(contrast);
            (Get1DChartData("频率", "CW荧光计数") as NumricChartData1D).Data.Add(locvalue);
            (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data.Add(signalcount);
            (Get1DChartData("参考信号总计数", "CW荧光计数") as NumricChartData1D).Data.Add(refcount);
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override KeyValuePair<ScanRange, bool> GetScanRange()
        {
            double xlo = GetInputParamValueByName("RFFreqLo");
            double xhi = GetInputParamValueByName("RFFreqHi");
            double step = GetInputParamValueByName("RFStep");
            bool rev = GetInputParamValueByName("Reverse");
            return new KeyValuePair<ScanRange, bool>(new ScanRange(xlo, xhi, step), rev);
        }

        public override RFSourceInfo GetScanSource()
        {
            return GetDeviceByName("RFSource") as RFSourceInfo;
        }

        protected override void InnerRead(FileObject fobj)
        {
        }

        protected override void InnerWrite(FileObject obj)
        {
        }

        public override void PreScanEvent()
        {
        }

        public override void AfterScanEvent()
        {
        }
    }
}
