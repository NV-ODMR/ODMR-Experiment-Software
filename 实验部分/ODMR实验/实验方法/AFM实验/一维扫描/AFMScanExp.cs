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

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM
{
    /// <summary>
    /// 一维扫描实验
    /// </summary>
    public class AFMScan1DExp : ODMRExperimentWithAFM
    {
        Scan1DSession<NanoStageInfo> PointsScanSession = new Scan1DSession<NanoStageInfo>();

        D1ScanRangeBase ScanType { get; set; } = null;

        public override string ODMRExperimentName { get; set; } = "";
        public override string ODMRExperimentGroupName { get; set; } = "AFM线扫描";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<bool>("显示子实验窗口",true,"ShowSubMenu"),
            new Param<double>("定轴位置",0,"FixedAxisLoc"),
            new Param<bool>("存储单点实验数据",true,"SaveSingleExpData"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {

        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.AFM扫描台,new Param<string>("扫描台","","Scanner")),
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
            NanoStageInfo dev = GetDeviceByName("Scanner") as NanoStageInfo;

            PointsScanSession.FirstScanEvent = ScanEvent;
            PointsScanSession.ScanEvent = ScanEvent;
            PointsScanSession.ScanSource = dev;
            PointsScanSession.ProgressBarMethod = new Action<NanoStageInfo, double>((devi, val) =>
            {
                SetProgress(val);
            });
            PointsScanSession.SetStateMethod = new Action<NanoStageInfo, double>((devi, v) =>
            {
                SetExpState("当前扫描轴位置:" + Math.Round(v, 5).ToString() + "定轴位置:" + Math.Round(GetInputParamValueByName("FixedAxisLoc"), 5).ToString());
            });
            PointsScanSession.StateJudgeEvent = JudgeThreadEndOrResume;
            PointsScanSession.BeginScan(ScanType, 0, 100);
        }

        /// <summary>
        /// 扫描操作，inputParams为空
        /// </summary>
        /// <param name="device"></param>
        /// <param name="locvalue"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public List<object> ScanEvent(NanoStageInfo scanner, D1ScanRangeBase scanPoints, double currentloc, List<object> inputParams)
        {
            //移动位移台
            scanner.Device.MoveToAndWait(currentloc, 120000);
            //进行实验
            ODMRExpObject exp = RunSubExperimentBlock(0, GetInputParamValueByName("ShowSubMenu"));

            //获取输出参数
            double value = 0;
            int ind = ScanType.GetNearestIndex(currentloc);
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
            return new List<object>();
        }

        public override void AfterExpEventWithAFM()
        {

        }

        public override void PreExpEventWithAFM()
        {
            //获取扫描范围类型
            if (ScanType == null)
            {
                throw new Exception("扫描范围未选择");
            }
            D1ChartDatas.Clear();
            D1ChartDatas.Add(new NumricChartData1D("位置", "一维扫描数据", ChartDataType.X));
            foreach (var item in SubExperiments)
            {
                foreach (var par in item.OutputParams)
                {
                    D1ChartDatas.Add(new NumricChartData1D(ODMRExperimentGroupName + ":" + ODMRExperimentName + ":" + par.Description, "一维扫描数据"));
                }
            }
            UpdatePlotChart();
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
