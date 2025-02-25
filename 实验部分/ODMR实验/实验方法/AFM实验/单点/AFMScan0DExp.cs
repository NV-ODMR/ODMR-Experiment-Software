using Controls.Windows;
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
    public class AFMScan0DExp : ODMRExperimentWithAFM
    {
        public override string ODMRExperimentName { get; set; } = "";
        public override string ODMRExperimentGroupName { get; set; } = "AFM点扫描";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
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
        public override List<KeyValuePair<string, Action>> InterativeButtons { get; set; } = new List<KeyValuePair<string, Action>>();
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override void AfterExpEventWithAFM()
        {
        }

        public override LockinInfo GetLockIn()
        {
            return GetDeviceByName("LockIn") as LockinInfo;
        }

        public override void ODMRExpWithAFM()
        {
            ODMRExpObject exp = RunSubExperimentBlock(0, GetInputParamValueByName("ShowSubMenu"));
            D1ChartDatas = exp.D1ChartDatas;
            D2ChartDatas = exp.D2ChartDatas;
            D1FitDatas = exp.D1FitDatas;
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "是否要继续?此操作将清除原先的实验数据,并且将执行下针操作", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override void PreExpEventWithAFM()
        {
        }
    }
}
