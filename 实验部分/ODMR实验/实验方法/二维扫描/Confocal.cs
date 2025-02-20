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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.序列实验.实验方法.二维扫描
{
    public class Confocal : Scan2DExpBase<NanoStageInfo, NanoStageInfo>
    {
        public override string ODMRExperimentName { get; set; } = "共聚焦扫描";
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
            new Param<int>("采样率(Hz)",60,"SampleRate")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {

        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头X","","LenX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头Y","","LenY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD"))
        };
        protected override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        protected override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override string CreateThreadState(NanoStageInfo dev1, NanoStageInfo dev2, double currentvalue1, double currentvalue2)
        {
            return "共聚焦扫描 X: " + Math.Round(currentvalue1, 5).ToString() + " Y: " + Math.Round(currentvalue2, 5).ToString();
        }

        public override List<object> FirstScanEvent(NanoStageInfo device1, NanoStageInfo device2, double loc1value, double loc2value, List<object> inputParams)
        {
            var r1 = GetScanRange1();
            var r2 = GetScanRange2();
            //新建数据集
            D2ChartDatas = new List<ChartData2D>()
            {
                new ChartData2D(new FormattedDataSeries2D(r1.Key.Count,r1.Key.Lo,r1.Key.Hi,r2.Key.Count,r2.Key.Lo,r2.Key.Hi){
                    XName="X轴位置(μm)",YName="Y轴位置(μm)",ZName="计数率(cps)"})
            };
            device1.Device.MoveToAndWait(loc1value, 1000, false);
            device2.Device.MoveToAndWait(loc2value, 1000, false);
            ConfocalAPDSample a = new ConfocalAPDSample();
            var res = a.CoreMethod(new List<object>());
            int count = (int)res[0];
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

        public override List<object> ScanEvent(NanoStageInfo device1, NanoStageInfo device2, double loc1value, double loc2value, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc1value, 1000, false);
            device2.Device.MoveToAndWait(loc2value, 1000, false);
            ConfocalAPDSample a = new ConfocalAPDSample();
            var res = a.CoreMethod(new List<object>());
            int count = (int)res[0];
            return new List<object>();
        }

        public override void SetDataToDataVisualSource(DataVisualSource source)
        {
            return;
        }

        public override KeyValuePair<ScanRange, bool> GetScanRange1()
        {
            double xlo = (GetInputParamByName("XRangeLo") as Param<double>).Value;
            double xhi = (GetInputParamByName("XRangeHi") as Param<double>).Value;
            int count = (GetInputParamByName("XCount") as Param<int>).Value;
            bool rev = (GetInputParamByName("XReverse") as Param<bool>).Value;
            return new KeyValuePair<ScanRange, bool>(new ScanRange(xlo, xhi, count), rev);
        }

        public override KeyValuePair<ScanRange, bool> GetScanRange2()
        {
            double ylo = (GetInputParamByName("YRangeLo") as Param<double>).Value;
            double yhi = (GetInputParamByName("YRangeHi") as Param<double>).Value;
            int count = (GetInputParamByName("YCount") as Param<int>).Value;
            bool rev = (GetInputParamByName("YReverse") as Param<bool>).Value;
            return new KeyValuePair<ScanRange, bool>(new ScanRange(ylo, yhi, count), rev);
        }

        public override NanoStageInfo GetScanSource1()
        {
            return GetDeviceByDescription("镜头X") as NanoStageInfo;
        }

        public override NanoStageInfo GetScanSource2()
        {
            return GetDeviceByDescription("镜头Y") as NanoStageInfo;
        }

        protected override void InnerRead(FileObject fobj)
        {
        }

        protected override void InnerWrite(FileObject obj)
        {
        }
    }
}
