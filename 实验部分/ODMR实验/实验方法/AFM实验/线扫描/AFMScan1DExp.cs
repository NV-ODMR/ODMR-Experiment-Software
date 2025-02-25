using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.Python.LbviewHandler;
using System.Windows.Threading;
using ODMR_Lab.实验部分.磁场调节;
using System.Threading;
using System.Windows;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分;
using ODMR_Lab.ODMR实验;
using HardWares.端口基类;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using Controls.Windows;
using Controls.Charts;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM.线扫描
{
    public class AFMScan1DExp : ODMRExperimentWithAFM
    {

        Scan1DSession<NanoStageInfo> D1Session = new Scan1DSession<NanoStageInfo>();

        public override string ODMRExperimentName { get; set; } = "";
        public override string ODMRExperimentGroupName { get; set; } = "AFM线扫描";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("静止轴位置",0,"StaticAxisLoc"),
            new Param<double>("扫描轴起始点",0,"MoveAxisLo"),
            new Param<double>("扫描轴终止点",0,"MoveAxisHi"),
            new Param<double>("扫描点数",0,"MoveAxisCount"),
            new Param<bool>("反转扫描方向",false,"Reverse"),
            new Param<bool>("轴X为移动轴",true,"IsXMove"),
            new Param<bool>("显示子实验窗口",true,"ShowSubMenu"),
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.AFM扫描台,new Param<string>("扫描台 X","","ScannerX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.AFM扫描台,new Param<string>("扫描台 Y","","ScannerY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.锁相放大器,new Param<string>("Lock In","","LockIn")),
        };

        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();
        public override List<KeyValuePair<string, Action>> InterativeButtons { get; set; }
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override void ODMRExpWithAFM()
        {
            NanoStageInfo dev = GetDeviceByName(GetInputParamValueByName("IsXMove") ? "ScannerX" : "ScannerY") as NanoStageInfo;
            var range = new ScanRange(GetInputParamValueByName("MoveAxisLo"),
                GetInputParamValueByName("MoveAxisHi"), GetInputParamValueByName("MoveAxisCount"),
                GetInputParamValueByName("Reverse"));
            //移动静止轴到指定位置
            NanoStageInfo staticdev = GetDeviceByName(GetInputParamValueByName("IsXMove") ? "ScannerY" : "ScannerX") as NanoStageInfo;
            staticdev.Device.MoveToAndWait(GetInputParamValueByName("StaticAxisLoc"), 60000);
            D1Session.FirstScanEvent = ScanEvent;
            D1Session.ScanEvent = ScanEvent;
            D1Session.ScanSource = dev;
            D1Session.ProgressBarMethod = new Action<NanoStageInfo, double>((devi, val) =>
            {
                SetProgress(val);
            });
            D1Session.SetStateMethod = new Action<NanoStageInfo, double>((devi, val1) =>
            {
                SetExpState("静止轴位置:" + Math.Round(GetInputParamValueByName("StaticAxisLoc"), 5).ToString() + ",当前位置: " + Math.Round(val1, 5).ToString());
            });
            D1Session.StateJudgeEvent = JudgeThreadEndOrResume;
            D1Session.BeginScan(range, 0, 100);
        }


        public List<object> ScanEvent(NanoStageInfo scanner, ScanRange range, double locvalue, List<object> inputParams)
        {
            //移动位移台
            scanner.Device.MoveToAndWait(locvalue, 60000);

            //进行实验
            ODMRExpObject exp = RunSubExperimentBlock(0, GetInputParamValueByName("ShowSubMenu"));

            //获取输出参数
            double value = 0;
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
                (Get1DChartData(exp.ODMRExperimentName + ":" + item.Description, "一维扫描数据") as NumricChartData1D).Data.Add(value);
            }
            UpdatePlotChartFlow(true);
            return inputParams;
        }
        public override void AfterExpEventWithAFM()
        {
        }

        public override void PreExpEventWithAFM()
        {
            ScanRange range1 = new ScanRange(GetInputParamValueByName("XRangeLo"), GetInputParamValueByName("XRangeHi"), GetInputParamValueByName("XCount"), GetInputParamValueByName("XReverse"));
            ScanRange range2 = new ScanRange(GetInputParamValueByName("YRangeLo"), GetInputParamValueByName("YRangeHi"), GetInputParamValueByName("YCount"), GetInputParamValueByName("YReverse"));
            D1ChartDatas.Clear();
            D1ChartDatas.Clear();
            D1FitDatas.Clear();
            D1ChartDatas.Add(new NumricChartData1D("扫描位置", "一维扫描数据", ChartDataType.X));
            List<string> yanmes = new List<string>();
            foreach (var item in SubExperiments)
            {
                foreach (var par in item.OutputParams)
                {
                    D1ChartDatas.Add(new NumricChartData1D(item.ODMRExperimentName + ":" + par.Description, "一维扫描数据", ChartDataType.Y));
                    yanmes.Add(item.ODMRExperimentName + ":" + par.Description);
                }
            }
            Show1DChartData("一维扫描数据", "扫描位置", yanmes);
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
