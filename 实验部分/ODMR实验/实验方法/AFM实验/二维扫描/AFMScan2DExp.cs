using Controls.Charts;
using Controls.Windows;
using HardWares.Lock_In;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM.二维扫描
{
    /// <summary>
    /// 二维扫描实验
    /// </summary>
    public class AFMScan2DExp : ODMRExperimentWithAFM
    {
        Scan2DSession<NanoStageInfo, NanoStageInfo> D2Session = new Scan2DSession<NanoStageInfo, NanoStageInfo>();

        public override string ODMRExperimentName { get; set; } = "";
        public override string ODMRExperimentGroupName { get; set; } = "AFM面扫描";
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
            new Param<bool>("显示子实验窗口",true,"ShowSubMenu"),
            new Param<bool>("存储单点实验数据",true,"SaveSingleExpData"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {

        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.AFM扫描台,new Param<string>("扫描台 X","","ScannerX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.AFM扫描台,new Param<string>("扫描台 Y","","ScannerY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.锁相放大器,new Param<string>("Lock In","","LockIn")),
        };
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override void ODMRExpWithAFM()
        {
            NanoStageInfo dev1 = GetDeviceByName("ScannerX") as NanoStageInfo;
            NanoStageInfo dev2 = GetDeviceByName("ScannerY") as NanoStageInfo;

            ScanRange range1 = new ScanRange(GetInputParamValueByName("XRangeLo"), GetInputParamValueByName("XRangeHi"), GetInputParamValueByName("XCount"), GetInputParamValueByName("XReverse"));
            ScanRange range2 = new ScanRange(GetInputParamValueByName("YRangeLo"), GetInputParamValueByName("YRangeHi"), GetInputParamValueByName("YCount"), GetInputParamValueByName("YReverse"));

            D2Session.FirstScanEvent = ScanEvent;
            D2Session.ScanEvent = ScanEvent;
            D2Session.ScanSource1 = dev1;
            D2Session.ScanSource2 = dev2;
            D2Session.ProgressBarMethod = new Action<NanoStageInfo, NanoStageInfo, double>((devi1, devi2, val) =>
            {
                SetProgress(val);
            });
            D2Session.SetStateMethod = new Action<NanoStageInfo, NanoStageInfo, double, double>((devi1, devi2, val1, val2) =>
            {
                SetExpState("当前扫描点: X: " + Math.Round(val1, 5).ToString() + " Y: " + Math.Round(val2, 5).ToString());
            });
            D2Session.StateJudgeEvent = JudgeThreadEndOrResume;
            D2Session.BeginScan(range1, range2, 0, 100);
        }

        /// <summary>
        /// 扫描操作，inputParams为空
        /// </summary>
        /// <param name="device"></param>
        /// <param name="locvalue"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public List<object> ScanEvent(NanoStageInfo scannerx, NanoStageInfo scannery, ScanRange range1, ScanRange range2, double loc1value, double loc2value, List<object> inputParams)
        {
            //移动位移台
            scannerx.Device.MoveToAndWait(loc1value, 60000);
            //移动位移台
            scannery.Device.MoveToAndWait(loc2value, 60000);
            //进行实验
            ODMRExpObject exp = RunSubExperimentBlock(0, GetInputParamValueByName("ShowSubMenu"));

            //获取输出参数
            double value = 0;
            int indx = range1.GetNearestIndex(loc1value);
            int indy = range2.GetNearestIndex(loc2value);
            foreach (var item in exp.OutputParams)
            {
                if (item.RawValue is bool)
                {
                    value = (bool)item.RawValue ? 1 : 0;
                }
                if (item.RawValue is int)
                {
                    value = (int)item.RawValue;
                }
                if (item.RawValue is double)
                {
                    value = (double)item.RawValue;
                }
                if (item.RawValue is float)
                {
                    value = (float)item.RawValue;
                }
                Get2DChartData(exp.ODMRExperimentName + ":" + item.Description, "二维扫描数据").Data.SetValue(indx, indy, value);
            }
            UpdatePlotChartFlow(true);

            //设置二维图表

            return new List<object>();
        }

        public override void AfterExpEventWithAFM()
        {

        }

        public override void PreExpEventWithAFM()
        {
            ScanRange range1 = new ScanRange(GetInputParamValueByName("XRangeLo"), GetInputParamValueByName("XRangeHi"), GetInputParamValueByName("XCount"), GetInputParamValueByName("XReverse"));
            ScanRange range2 = new ScanRange(GetInputParamValueByName("YRangeLo"), GetInputParamValueByName("YRangeHi"), GetInputParamValueByName("YCount"), GetInputParamValueByName("YReverse"));
            foreach (var item in SubExperiments)
            {
                foreach (var par in item.OutputParams)
                {
                    D2ChartDatas.Add(new ChartData2D(new FormattedDataSeries2D(range1.Count, range1.Lo, range1.Hi, range2.Count, range2.Lo, range2.Hi)
                    { XName = "轴1位置", YName = "轴2位置", ZName = item.ODMRExperimentName + ":" + par.Description })
                    { GroupName = "二维扫描数据" });
                }
            }
            UpdatePlotChart();
            if (D2ChartDatas.Count != 0)
            {
                var data = D2ChartDatas[0].Data;
                Show2DChartData("二维扫描数据", data.XName, data.YName, data.ZName);
            }
        }

        public override LockinInfo GetLockIn()
        {
            return GetDeviceByName("LockIn") as LockinInfo;
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据,并且将执行下针操作", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }
    }
}
